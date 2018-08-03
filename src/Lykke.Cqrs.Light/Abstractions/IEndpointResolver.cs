using JetBrains.Annotations;
using Lykke.Cqrs.Light.Routing;
using Lykke.Messaging.Contract;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface IEndpointResolver
    {
        Endpoint Resolve(string route, RoutingKey key);
    }
}
