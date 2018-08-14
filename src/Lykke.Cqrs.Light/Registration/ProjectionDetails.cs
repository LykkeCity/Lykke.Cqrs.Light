using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class ProjectionDetails
    {
        internal List<TypesData> ListeningEventsData { get; }
        internal Type ProjectionType { get; }
        internal object Projection { get; }

        internal ProjectionDetails(Type projectionType)
        {
            ProjectionType = projectionType;
            ListeningEventsData = new List<TypesData>();
        }

        internal ProjectionDetails(object projection)
        {
            Projection = projection;
            ProjectionType = projection.GetType();
            ListeningEventsData = new List<TypesData>();
        }

        public ProjectionDetails ListeningEvents(
            [NotNull] string fromContext,
            [NotNull] string route,
            [NotNull] params Type[] eventTypes)
        {
            return ListeningEvents(
                fromContext,
                route,
                null,
                eventTypes);
        }

        public ProjectionDetails ListeningEvents(
            [NotNull] string fromContext,
            [NotNull] string route,
            [CanBeNull] IEndpointResolver endpointResolver = null,
            [NotNull] params Type[] eventTypes)
        {
            if (string.IsNullOrWhiteSpace(fromContext))
                throw new ArgumentNullException(nameof(fromContext));
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentNullException(nameof(route));
            if (eventTypes == null || eventTypes.Length == 0)
                throw new ArgumentNullException(nameof(eventTypes));
            if (eventTypes.Any(i => i == null))
                throw new ArgumentException("Command types list can't contain null value");
            var interfaceType = typeof(IProjection<>);
            foreach (Type eventType in eventTypes)
            {
                var mustImplementType = interfaceType.MakeGenericType(eventType);
                if (!mustImplementType.IsAssignableFrom(ProjectionType))
                    throw new InvalidOperationException($"Projection {ProjectionType.Name} must implement IProjection<{eventType.Name}> interface");
            }

            ListeningEventsData.Add(
                new TypesData
                {
                    Context = fromContext,
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = eventTypes,
                });

            return this;
        }
    }
}
