using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Routing;
using System;

namespace Lykke.Cqrs.Light.Registration
{
    internal class DefaultRoutingRegistration : IRegistration
    {
        private readonly DefaultRoutingDetails _details;

        internal DefaultRoutingRegistration(DefaultRoutingDetails details)
        {
            if (details.PublishingCommandsDataDict.Count == 0)
                throw new InvalidOperationException("Publishing commands list can't be empty");

            _details = details;
        }

        public Context CreateContext([ItemNotNull] ICqrsEngine cqrsEngine)
        {
            if (cqrsEngine == null)
                throw new ArgumentNullException();

            var routeMap = cqrsEngine.DefaultRouteMap;
            foreach (var pair in _details.PublishingCommandsDataDict)
            {
                var route = routeMap.GetRoute(pair.Value.Route ?? CqrsEngine.DefaultRoute, RouteType.Commands);
                foreach (Type commandType in pair.Value.Types)
                {
                    route.AddPublishedCommand(
                        commandType,
                        pair.Key,
                        pair.Value.EndpointResolver ?? cqrsEngine.DefaultEndpointResolver);
                }
            }
            return null;
        }
    }
}
