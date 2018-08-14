using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using System;

namespace Lykke.Cqrs.Light
{
    [PublicAPI]
    public class Context
    {
        internal RouteMap RouteMap { get; }
        internal EventDispatcher EventDispatcher { get; }
        internal CommandDispatcher CommandDispatcher { get; }
        internal IEventPublisher EventPublisher { get; }

        internal string Name { get; }

        internal Context(
            string name,
            RouteMap routeMap,
            ICqrsEngine cqrsEngine,
            TimeSpan? failedCommandRetryDelay = null,
            TimeSpan? failedEventRetryDelay = null)
        {
            Name = name;
            RouteMap = routeMap;
            CommandDispatcher = new CommandDispatcher(cqrsEngine.LogFactory, failedCommandRetryDelay);
            EventDispatcher = new EventDispatcher(
                cqrsEngine,
                cqrsEngine.LogFactory,
                failedEventRetryDelay);
            EventPublisher = cqrsEngine.GetEventPublisher(name);
        }
    }
}
