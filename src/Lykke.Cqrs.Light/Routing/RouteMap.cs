using System;
using System.Collections.Generic;

namespace Lykke.Cqrs.Light.Routing
{
    public class RouteMap
    {
        private readonly Dictionary<string, Route> _routesDict = new Dictionary<string, Route>();

        internal IEnumerable<Route> Routes => _routesDict.Values;

        internal string Context { get; }

        internal RouteMap(string context)
        {
            Context = context;
        }

        internal Route GetRoute(string name, RouteType routeType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (_routesDict.TryGetValue(name, out var route))
                return route;

            route = new Route(name, Context, routeType);
            _routesDict.Add(name, route);
            return route;
        }

        internal List<PublishRoute> GetPublishRoutePoints(RouteType routeType, Type type)
        {
            var publishDirections = new List<PublishRoute>();
            foreach (var route in _routesDict.Values)
            {
                var messageRoutes = routeType == RouteType.Events ? route.PublishEventRoutes : route.PublishCommandRoutes;
                if (messageRoutes.Count == 0)
                    continue;

                foreach (var messageRoute in messageRoutes)
                {
                    if (messageRoute.Item1.MessageType != type)
                        continue;

                    publishDirections.Add(
                        new PublishRoute
                        {
                            Endpoint = messageRoute.Item2,
                            ProcessingGroupName = route.ProcessingGroupName,
                            TargetContext = messageRoute.Item1.TargetContext,
                        });
                }
            }
            return publishDirections;
        }

        internal void ResolveRoutes()
        {
            foreach (Route route in _routesDict.Values)
            {
                route.ResolveEndpoints();
            }
        }
    }
}
