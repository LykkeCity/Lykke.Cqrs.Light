using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Cqrs.Light.Registration
{
    internal class ContextRegistration : IRegistration
    {
        private readonly ContextDetails _details;

        internal ContextRegistration(ContextDetails details)
        {
            if (details == null)
                throw new ArgumentNullException(nameof(details));
            if (details.СommandsHandlerDetailsList.Count == 0
                && details.ProjectionDetailsList.Count == 0
                && details.CommandsDataDict.Count == 0)
                throw new InvalidOperationException(
                    $"For context {details.ContextName} at least 1 command handler or projection or 1 publising command must be configured");

            _details = details;
        }

        public Context CreateContext(ICqrsEngine cqrsEngine)
        {
            var routeMap = new RouteMap(_details.ContextName);
            var result = new Context(
                _details.ContextName,
                routeMap,
                cqrsEngine,
                _details.FailedCommandDelay ?? cqrsEngine.FailedCommandRetryDelay,
                _details.FailedEventDelay ?? cqrsEngine.FailedEventRetryDelay);
            foreach (var commandsHandlerDetails in _details.СommandsHandlerDetailsList)
            {
                var handledCommandTypes = new HashSet<Type>();
                foreach (var commandsDetails in commandsHandlerDetails.CommandsData)
                {
                    string routeName = commandsDetails.Route;
                    var route = routeMap.GetRoute(routeName ?? CqrsEngine.DefaultRoute, RouteType.Commands);
                    if (!string.IsNullOrWhiteSpace(routeName))
                    {
                        if (commandsHandlerDetails.ThreadsDict.ContainsKey(routeName))
                            route.ProcessingGroup.ConcurrencyLevel = commandsHandlerDetails.ThreadsDict[routeName];
                        if (commandsHandlerDetails.QueuesDict.ContainsKey(routeName))
                            route.ProcessingGroup.QueueCapacity = commandsHandlerDetails.QueuesDict[routeName];
                    }
                    foreach (Type commandType in commandsDetails.Types)
                    {
                        route.AddSubscribedCommand(commandType, commandsDetails.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver);
                        handledCommandTypes.Add(commandType);
                    }
                }
                foreach (var commandsDetails in commandsHandlerDetails.LoopbackCommandsData)
                {
                    var defaultRouteMap = cqrsEngine.DefaultRouteMap;
                    var route = defaultRouteMap.GetRoute(commandsDetails.Route ?? CqrsEngine.DefaultRoute, RouteType.Commands);
                    foreach (Type commandType in commandsDetails.Types)
                    {
                        route.AddPublishedCommand(commandType, _details.ContextName, commandsDetails.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver);
                    }
                }
                foreach (var eventsDetails in commandsHandlerDetails.EventsData)
                {
                    var route = routeMap.GetRoute(eventsDetails.Route ?? CqrsEngine.DefaultRoute, RouteType.Events);
                    foreach (Type eventType in eventsDetails.Types)
                    {
                        route.AddPublishedEvent(eventType, eventsDetails.EndpointResolver?? cqrsEngine.DefaultEndpointResolver);
                    }
                }
                var commandHandler = commandsHandlerDetails.CommandHandler ?? cqrsEngine.Resolve(commandsHandlerDetails.CommandHandlerType);
                result.CommandDispatcher.AddHandler(
                    commandHandler,
                    result.EventPublisher,
                    handledCommandTypes.ToArray());
            }
            foreach (var projectionDetails in _details.ProjectionDetailsList)
            {
                foreach (var eventsDetails in projectionDetails.EventsData)
                {
                    var route = routeMap.GetRoute(eventsDetails.Route ?? CqrsEngine.DefaultRoute, RouteType.Events);
                    foreach (Type eventType in eventsDetails.Types)
                    {
                        route.AddSubscribedEvent(
                            eventType,
                            eventsDetails.Context,
                            eventsDetails.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver,
                            true);
                        var eventHandler = projectionDetails.Projection ?? cqrsEngine.Resolve(projectionDetails.ProjectionType);
                        result.EventDispatcher.AddProjectionHandler(eventType, eventsDetails.Context, eventHandler);
                    }
                }
            }
            foreach (var publichContextInfo in _details.CommandsDataDict)
            {
                var typesData = publichContextInfo.Value;
                var route = routeMap.GetRoute(typesData.Route ?? CqrsEngine.DefaultRoute, RouteType.Commands);
                foreach (Type commandType in typesData.Types)
                {
                    route.AddPublishedCommand(
                        commandType,
                        publichContextInfo.Key,
                        typesData.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver);
                }
            }
            return result;
        }
    }
}
