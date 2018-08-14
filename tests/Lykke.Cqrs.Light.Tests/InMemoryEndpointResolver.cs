using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using Lykke.Messaging.Contract;
using Lykke.Messaging.Serialization;

namespace Lykke.Cqrs.Light.Tests
{
    internal class InMemoryEndpointResolver : IEndpointResolver
    {
        public Endpoint Resolve(string route, RoutingKey key)
        {
            return new Endpoint(
                "InMemory",
                /*key.LocalBoundedContext + "." + */route,
                true,
                SerializationFormat.Json);
        }
    }
}
