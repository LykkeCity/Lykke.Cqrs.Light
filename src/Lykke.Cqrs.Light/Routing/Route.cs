using Lykke.Cqrs.Light.Abstractions;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using System;
using System.Collections.Generic;

namespace Lykke.Cqrs.Light.Routing
{
    internal class Route
    {
        private readonly string _context;
        private readonly Dictionary<RoutingKey, IEndpointResolver> _routeResolvers = new Dictionary<RoutingKey, IEndpointResolver>();

        internal string ProcessingGroupName => $"cqrs.{(_context ?? "default")}.{Name}";

        internal string Name { get; }
        internal RouteType Type { get; }
        internal ProcessingGroupInfo ProcessingGroup { get; }
        internal List<(RoutingKey, Endpoint)> PublishCommandRoutes { get; }
        internal List<(RoutingKey, Endpoint)> PublishEventRoutes { get; }
        internal List<(RoutingKey, Endpoint)> SubscribeCommandRoutes { get; }
        internal List<(RoutingKey, Endpoint)> SubscribeEventRoutes { get; }

        internal Route(
            string name,
            string context,
            RouteType routeType)
        {
            Name = name;
            _context = context;
            Type = routeType;
            ProcessingGroup = new ProcessingGroupInfo();
            PublishCommandRoutes = new List<(RoutingKey, Endpoint)>();
            PublishEventRoutes = new List<(RoutingKey, Endpoint)>();
            SubscribeCommandRoutes = new List<(RoutingKey, Endpoint)>();
            SubscribeEventRoutes = new List<(RoutingKey, Endpoint)>();
        }

        internal void AddPublishedCommand(
            Type command,
            string targetContext,
            IEndpointResolver resolver)
        {
            if (Type != RouteType.Commands)
                throw new ApplicationException(string.Format("Can not publish commands with events route '{0}'.", Name));

            var routingKey = new RoutingKey
            {
                SourceContext = _context,
                MessageType = command,
                RouteType = RouteType.Commands,
                CommunicationType = CommunicationType.Publish,
                TargetContext = targetContext
            };
            _routeResolvers[routingKey] = resolver;
        }

        internal void AddPublishedEvent(Type @event, IEndpointResolver resolver)
        {
            if (Type != RouteType.Events)
                throw new ApplicationException(string.Format("Can not publish for events with commands route '{0}'.", Name));

            var routingKey = new RoutingKey
            {
                SourceContext = _context,
                RouteType = RouteType.Events,
                MessageType = @event,
                CommunicationType = CommunicationType.Publish
            };
            _routeResolvers[routingKey] = resolver;
        }

        internal void AddSubscribedCommand(Type command, IEndpointResolver resolver)
        {
            if (Type != RouteType.Commands)
                throw new ApplicationException(string.Format("Can not subscribe for commands on events route '{0}'.", Name));

            var routingKey = new RoutingKey
            {
                SourceContext = _context,
                MessageType = command,
                RouteType = RouteType.Commands,
                CommunicationType = CommunicationType.Subscribe
            };
            _routeResolvers[routingKey] = resolver;
        }

        internal void AddSubscribedEvent(
            Type @event,
            string fromContext,
            IEndpointResolver resolver,
            bool exclusive)
        {
            if (Type != RouteType.Events)
                throw new ApplicationException(string.Format("Can not subscribe for events on commands route '{0}'.", Name));

            var routingKey = new RoutingKey
            {
                SourceContext = _context,
                RouteType = RouteType.Events,
                MessageType = @event,
                TargetContext = fromContext,
                CommunicationType = CommunicationType.Subscribe,
                Exclusive = exclusive,
            };
            _routeResolvers[routingKey] = resolver;
        }

        internal void ResolveEndpoints()
        {
            foreach (var pair in _routeResolvers)
            {
                var endpoint = pair.Value.Resolve(Name, pair.Key);
                var route = (pair.Key, endpoint);
                switch (pair.Key.CommunicationType)
                {
                    case CommunicationType.Publish:
                        if (pair.Key.RouteType == RouteType.Commands)
                            PublishCommandRoutes.Add(route);
                        else
                            PublishEventRoutes.Add(route);
                        break;
                    case CommunicationType.Subscribe:
                        if (pair.Key.RouteType == RouteType.Commands)
                            SubscribeCommandRoutes.Add(route);
                        else
                            SubscribeEventRoutes.Add(route);
                        break;
                    default:
                        throw new NotSupportedException($"CommunicationType {pair.Key.CommunicationType} is not supported");
                }
            }
        }
    }
}
