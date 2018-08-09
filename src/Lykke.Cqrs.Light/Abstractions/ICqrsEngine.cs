using Autofac;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Lykke.Cqrs.Light.Routing;
using System;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface ICqrsEngine : ICommandSender, IDependencyResolver
    {
        TimeSpan? FailedCommandRetryDelay { get; }
        TimeSpan? FailedEventRetryDelay { get; }
        IEndpointResolver DefaultEndpointResolver { get; }
        RouteMap DefaultRouteMap { get; }
        ILogFactory LogFactory { get; }

        void Init(IContainer container);
        IEventPublisher GetEventPublisher(string context);
    }
}
