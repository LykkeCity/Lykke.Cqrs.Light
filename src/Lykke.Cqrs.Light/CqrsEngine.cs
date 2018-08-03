using Autofac;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using Lykke.Messaging.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

namespace Lykke.Cqrs.Light
{
    internal sealed class CqrsEngine : ICqrsEngine, IDisposable
    {
        private readonly Dictionary<Type, List<PublishRoute>> _typeRoutePointsDict = new Dictionary<Type, List<PublishRoute>>();
        private readonly Dictionary<string, Context> _contextsDict = new Dictionary<string, Context>();
        private readonly CompositeDisposable _subscription = new CompositeDisposable();
        private readonly IMessagingEngine _messagingEngine;
        private readonly IRegistration[] _registrations;

        private IDependencyResolver _dependencyResolver;

        internal static string DefaultContext = "default";
        internal static string DefaultRoute = "default";

        public TimeSpan? FailedCommandRetryDelay { get; }
        public TimeSpan? FailedEventRetryDelay { get; }
        public IEndpointResolver DefaultEndpointResolver { get; }
        public RouteMap DefaultRouteMap { get; }
        public ILogFactory LogFactory { get; private set; }
        public ILog Log { get; }

        internal CqrsEngine(
            IMessagingEngine messagingEngine,
            IEndpointResolver defaultEndpointResolver,
            ILogFactory logFactory,
            TimeSpan? failedCommandRetryDelay,
            TimeSpan? failedEventRetryDelay,
            params IRegistration[] registrations)
        {
            _messagingEngine = messagingEngine;
            LogFactory = logFactory;
            Log = LogFactory.CreateLog(this);
            _registrations = registrations;

            FailedCommandRetryDelay = failedCommandRetryDelay;
            FailedEventRetryDelay = failedEventRetryDelay;
            DefaultEndpointResolver = defaultEndpointResolver;
            DefaultRouteMap = new RouteMap(DefaultContext);
        }

        [Obsolete("Use ILogFactory")]
        internal CqrsEngine(
            IMessagingEngine messagingEngine,
            IEndpointResolver defaultEndpointResolver,
            ILog log,
            TimeSpan? failedCommandRetryDelay,
            TimeSpan? failedEventRetryDelay,
            params IRegistration[] registrations)
        {
            _messagingEngine = messagingEngine;
            Log = log;
            _registrations = registrations;

            FailedCommandRetryDelay = failedCommandRetryDelay;
            FailedEventRetryDelay = failedEventRetryDelay;
            DefaultEndpointResolver = defaultEndpointResolver;
            DefaultRouteMap = new RouteMap(DefaultContext);
        }

        internal CqrsEngine(
            ILogFactory logFactory,
            IMessagingEngine messagingEngine,
            IDependencyResolver dependencyResolver,
            IEndpointResolver defaultEndpointResolver,
            params IRegistration[] registrations)
        {
            LogFactory = logFactory;
            _messagingEngine = messagingEngine;
            Log = logFactory.CreateLog(this);
            _dependencyResolver = dependencyResolver;
            _registrations = registrations;

            DefaultEndpointResolver = defaultEndpointResolver;
            DefaultRouteMap = new RouteMap(DefaultContext);
        }

        public void Init(IContainer container)
        {
            _dependencyResolver = new DependencyResolver(container);

            ProcessRegistrations();

            EnsureEndpoints();

            AddSubscriptions();
        }

        public IEventPublisher GetEventPublisher(string context)
        {
            return new EventPublisher(this, context);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void SendCommand(object command, string targetContext)
        {
            SendMessage(
                command,
                null,
                targetContext,
                RouteType.Commands);
        }

        public void SendCommand(object command, string sourceContext, string targetContext)
        {
            SendMessage(
                command,
                sourceContext,
                targetContext,
                RouteType.Commands);
        }

        public object Resolve(Type type)
        {
            return _dependencyResolver.Resolve(type);
        }

        internal void PublishEvent(object @event, string sourceContext)
        {
            SendMessage(
                @event,
                sourceContext,
                null,
                RouteType.Events);
        }

        private void SendMessage(
            object message,
            string sourceContext,
            string targetContext,
            RouteType routeType)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            Type type = message.GetType();
            var telemtryOperation = TelemetryHelper.InitTelemetryOperation(
                routeType == RouteType.Events ? "Cqrs publish event" : "Cqrs send command",
                type.Name,
                sourceContext,
                targetContext);

            try
            {
                if (!_typeRoutePointsDict.TryGetValue(type, out var routePoints))
                {
                    RouteMap routeMap = GetRouteMap(sourceContext);
                    routePoints = routeMap.GetPublishRoutePoints(routeType, type);
                    if (routePoints.Count == 0 && routeType == RouteType.Commands)
                    {
                        routeMap = GetRouteMap(null);
                        routePoints = routeMap.GetPublishRoutePoints(routeType, type);
                        _typeRoutePointsDict[type] = routePoints;
                    }
                    _typeRoutePointsDict[type] = routePoints;
                }
                if (routePoints.Count == 0)
                    throw new InvalidOperationException($"Context '{sourceContext}' doesn't have routes for '{message.GetType()}'");

                foreach (var routePoint in routePoints.Where(i => i.TargetContext == targetContext))
                {
                    _messagingEngine.Send(
                        message,
                        routePoint.Endpoint,
                        routePoint.ProcessingGroupName);
                }
            }
            catch (Exception e)
            {
                TelemetryHelper.SubmitException(telemtryOperation, e);
                throw;
            }
            finally
            {
                TelemetryHelper.SubmitOperationResult(telemtryOperation);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            _subscription.Dispose();
        }

        private RouteMap GetRouteMap(string context)
        {
            if (context == null)
                return DefaultRouteMap;

            if (!_contextsDict.ContainsKey(context))
                throw new ArgumentException($"Сontext {context} not found");
            return _contextsDict[context].RouteMap;
        }

        private void ProcessRegistrations()
        {
            foreach (var registration in _registrations)
            {
                var context = registration.CreateContext(this);
                if (context == null)
                    continue;

                if (_contextsDict.ContainsKey(context.Name))
                    throw new InvalidOperationException(
                        $"There is an attempt to register already registered context '{context.Name}' from {registration.GetType().Name}. ");
                _contextsDict.Add(context.Name, context);
            }
        }

        private void EnsureEndpoints()
        {
            Log.WriteInfo(nameof(EnsureEndpoints), null, "Endpoints verification");

            foreach (var routeMap in new[] { DefaultRouteMap }.Concat(_contextsDict.Values.Select(c => c.RouteMap)))
            {
                foreach (var route in routeMap.Routes)
                {
                    _messagingEngine.AddProcessingGroup(route.ProcessingGroupName, route.ProcessingGroup);
                }

                Log.WriteInfo(nameof(EnsureEndpoints), null, $"Context '{routeMap.Context}':");

                routeMap.ResolveRoutes();

                foreach (var route in routeMap.Routes)
                {
                    Log.WriteInfo(nameof(EnsureEndpoints), null, $"\t{route.Type} route '{route.Name}':");
                    var routeTypeName = route.Type.ToString().ToLower().TrimEnd('s');

                    var allRoutes = route.PublishCommandRoutes
                        .Concat(route.PublishEventRoutes)
                        .Concat(route.SubscribeCommandRoutes)
                        .Concat(route.SubscribeEventRoutes);

                    foreach (var messageRoute in allRoutes)
                    {
                        var routingKey = messageRoute.Item1;
                        var endpoint = messageRoute.Item2;
                        VerifyRoutingKey(
                            routingKey,
                            endpoint,
                            routeMap.Context,
                            route.Name,
                            routeTypeName);
                    }
                }
            }
        }

        private void VerifyRoutingKey(
            RoutingKey routingKey,
            Endpoint endpoint,
            string context,
            string route,
            string routeTypeName)
        {
            bool result = true;
            if (!_messagingEngine.VerifyEndpoint(
                endpoint,
                routingKey.CommunicationType == CommunicationType.Publish ? EndpointUsage.Publish : EndpointUsage.Subscribe,
                true,
                out string error))
            {
                Log.WriteError(nameof(VerifyRoutingKey), routingKey, new InvalidOperationException(
                    string.Format(
                        "Route '{1}' within bounded context '{0}' for {2} type '{3}' has resolved endpoint {4} that is not properly configured for {5}: {6}",
                        context,
                        route,
                        routeTypeName,
                        routingKey.MessageType.Name,
                        endpoint,
                        routingKey.CommunicationType == CommunicationType.Publish ? "publishing" : "subscription",
                        error)));
                result = false;
            }

            Log.WriteInfo(nameof(VerifyRoutingKey), routingKey, $"\t\tSubscribing '{routingKey.MessageType.Name}' on {endpoint}\t{(result ? "OK" : "ERROR:" + error)}");
        }

        private void AddSubscriptions()
        {
            foreach (var context in _contextsDict.Values)
            {
                foreach (var route in context.RouteMap.Routes)
                {
                    var subscriptions = route.SubscribeCommandRoutes
                    .Concat(route.SubscribeEventRoutes)
                    .Select(r => new
                    {
                        type = r.Item1.MessageType,
                        targetContext = r.Item1.TargetContext,
                        endpoint = new Endpoint(
                            r.Item2.TransportId,
                            "",
                            r.Item2.Destination.Subscribe,
                            r.Item2.SharedDestination,
                            r.Item2.SerializationFormat)
                    })
                    .GroupBy(x => Tuple.Create(x.endpoint, x.targetContext))
                    .Select(g => new
                    {
                        endpoint = g.Key.Item1,
                        targetContext = g.Key.Item2,
                        types = g.Select(x => x.type).ToArray()
                    });

                    foreach (var subscription in subscriptions)
                    {
                        CallbackDelegate<object> callback;
                        string messageTypeName;
                        switch (route.Type)
                        {
                            case RouteType.Events:
                                callback = (@event, acknowledge, headers) =>
                                    context.EventDispatcher.Dispatch(
                                        @event,
                                        acknowledge,
                                        subscription.targetContext);
                                messageTypeName = "event";
                                break;
                            case RouteType.Commands:
                                callback = (command, acknowledge, headers) =>
                                    context.CommandDispatcher.Dispatch(
                                        command,
                                        acknowledge,
                                        subscription.targetContext);
                                messageTypeName = "command";
                                break;
                            default:
                                throw new NotSupportedException($"Route tupe {route.Type} is not supported");
                        }

                        _subscription.Add(
                            _messagingEngine.Subscribe(
                                subscription.endpoint,
                                callback,
                                (type, acknowledge) => throw new InvalidOperationException($"Unknown {messageTypeName} received: {type}"),
                                route.ProcessingGroupName,
                                0,
                                subscription.types));
                    }
                }
            }
        }
    }
}
