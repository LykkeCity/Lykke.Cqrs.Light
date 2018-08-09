using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using System;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class SagaRegistration : IRegistration
    {
        private readonly SagaDetails _details;

        internal SagaRegistration(SagaDetails details)
        {
            if (details.PublishingCommandsData.Count == 0)
                throw new InvalidOperationException($"For context {details.ContextName} at least 1 publishing command must be configured");
            if (details.ListeningEventsData.Count == 0)
                throw new InvalidOperationException($"For context {details.ContextName} at least 1 listening event must be configured");

            _details = details;
        }

        public Context CreateContext([NotNull] ICqrsEngine cqrsEngine)
        {
            if (cqrsEngine == null)
                throw new ArgumentNullException();

            var routeMap = new RouteMap(_details.ContextName);
            var result = new Context(
                _details.ContextName,
                routeMap,
                cqrsEngine,
                cqrsEngine.FailedCommandRetryDelay,
                _details.FailedEventDelay ?? cqrsEngine.FailedEventRetryDelay);
            foreach (var commandsDetails in _details.PublishingCommandsData)
            {
                string routeName = commandsDetails.Route;
                var route = routeMap.GetRoute(routeName ?? CqrsEngine.DefaultRoute, RouteType.Commands);
                if (!string.IsNullOrWhiteSpace(routeName))
                {
                    if (_details.ThreadsDict.TryGetValue(routeName, out var threadsCount))
                        route.ProcessingGroup.ConcurrencyLevel = threadsCount;
                    if (_details.QueuesDict.TryGetValue(routeName, out var queueCapacity))
                        route.ProcessingGroup.QueueCapacity = queueCapacity;
                }
                foreach (Type commandType in commandsDetails.Types)
                {
                    route.AddPublishedCommand(
                        commandType,
                        commandsDetails.Context,
                        commandsDetails.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver);
                }
            }
            foreach (var eventsDetails in _details.ListeningEventsData)
            {
                string routeName = eventsDetails.Route;
                var route = routeMap.GetRoute(routeName ?? CqrsEngine.DefaultRoute, RouteType.Events);
                if (!string.IsNullOrWhiteSpace(routeName))
                {
                    if (_details.ThreadsDict.TryGetValue(routeName, out var threadsCount))
                        route.ProcessingGroup.ConcurrencyLevel = threadsCount;
                    if (_details.QueuesDict.TryGetValue(routeName, out var queueCapacity))
                        route.ProcessingGroup.QueueCapacity = queueCapacity;
                }
                foreach (Type eventType in eventsDetails.Types)
                {
                    route.AddSubscribedEvent(
                        eventType,
                        eventsDetails.Context,
                        eventsDetails.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver,
                        false);
                    var eventHandler = _details.Saga ?? cqrsEngine.Resolve(_details.SagaType);
                    result.EventDispatcher.AddHandler(eventType, eventsDetails.Context, eventHandler);
                }
            }
            return result;
        }
    }
}
