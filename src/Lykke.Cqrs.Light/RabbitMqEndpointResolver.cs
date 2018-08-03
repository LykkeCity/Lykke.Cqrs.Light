using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using Lykke.Messaging.Contract;
using Lykke.Messaging.Serialization;
using System;
using System.Collections.Generic;

namespace Lykke.Cqrs.Light
{
    [PublicAPI]
    public class RabbitMqEndpointResolver : IEndpointResolver
    {
        private readonly Dictionary<Tuple<string, RoutingKey>, Endpoint> _cache =
            new Dictionary<Tuple<string, RoutingKey>, Endpoint>();

        private readonly string _transport;
        private readonly SerializationFormat _serializationFormat;
        private readonly string _exclusiveQueuePostfix;
        private readonly string _environmentPrefix;

        public RabbitMqEndpointResolver(
            string transport = "RabbitMq",
            SerializationFormat serializationFormat = SerializationFormat.MessagePack,
            string exclusiveQueuePostfix = null,
            string environment = "lykke")
        {
            _environmentPrefix = environment != null ? $"{environment}." : string.Empty;
            _exclusiveQueuePostfix = $".{exclusiveQueuePostfix ?? "projections"}";
            _transport = transport;
            _serializationFormat = serializationFormat;
        }

        public Endpoint Resolve(string route, RoutingKey key)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(Tuple.Create(route, key), out var ep))
                    return ep;

                ep = CreateEndpoint(route, key);
                _cache.Add(Tuple.Create(route, key), ep);
                return ep;
            }
        }

        private Endpoint CreateEndpoint(string route, RoutingKey key)
        {
            var endpoint = new Endpoint
            {
                SerializationFormat = _serializationFormat,
                SharedDestination = true,
                TransportId = _transport,
            };
            switch (key.RouteType)
            {
                case RouteType.Commands:
                    var rmqRoutingKey = key.MessageType.Name;
                    switch (key.CommunicationType)
                    {
                        case CommunicationType.Subscribe:
                            endpoint.Destination = new Destination
                            {
                                Publish = CreateExchangeName(
                                    $"{key.SourceContext}.{GetKewordByRoutType(key.RouteType)}.exchange/{rmqRoutingKey}"),
                                Subscribe = CreateQueueName(
                                    $"{key.SourceContext}.queue.{GetKewordByRoutType(key.RouteType)}.{route}",
                                    key.Exclusive)
                            };
                            break;
                        case CommunicationType.Publish:
                            endpoint.Destination = new Destination
                            {
                                Publish = CreateExchangeName(
                                    $"{key.TargetContext}.{GetKewordByRoutType(key.RouteType)}.exchange/{rmqRoutingKey}"),
                            };
                            break;
                        default:
                            throw new NotSupportedException($"Communication type {key.CommunicationType} is not supported");
                    }
                    break;
                case RouteType.Events:
                    switch (key.CommunicationType)
                    {
                        case CommunicationType.Subscribe:
                            endpoint.Destination = new Destination
                            {
                                Publish = CreateExchangeName(
                                    $"{key.TargetContext}.{GetKewordByRoutType(key.RouteType)}.exchange/{key.MessageType.Name}"),
                                Subscribe = CreateQueueName(
                                    $"{key.SourceContext}.queue.{key.TargetContext}.{GetKewordByRoutType(key.RouteType)}.{route}",
                                    key.Exclusive)
                            };
                            break;
                        case CommunicationType.Publish:
                            endpoint.Destination = new Destination
                            {
                                Publish = CreateExchangeName(
                                    $"{key.SourceContext}.{GetKewordByRoutType(key.RouteType)}.exchange/{key.MessageType.Name}"),
                            };
                            break;
                        default:
                            throw new NotSupportedException($"Communication type {key.CommunicationType} is not supported");
                    }
                    break;
                default:
                    throw new NotSupportedException($"Route type {key.RouteType} ");
            }
            return endpoint;
        }

        private string CreateQueueName(string queue, bool exclusive)
        {
            return $"{_environmentPrefix}{queue}{(exclusive ? _exclusiveQueuePostfix : string.Empty)}";
        }

        private string CreateExchangeName(string exchange)
        {
            return $"topic://{_environmentPrefix}{exchange}";
        }

        private string GetKewordByRoutType(RouteType routeType)
        {
            return routeType.ToString().ToLower();
        }
    }
}
