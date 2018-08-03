﻿using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class ProjectionDetails
    {
        internal List<TypesData> EventsData { get; set; }
        internal Type ProjectionType { get; }
        internal object Projection { get; }

        internal ProjectionDetails(Type projectionType)
        {
            ProjectionType = projectionType;
            EventsData = new List<TypesData>();
        }

        internal ProjectionDetails(object projection)
        {
            Projection = projection;
            ProjectionType = projection.GetType();
            EventsData = new List<TypesData>();
        }

        public ProjectionDetails ListeningEvents(
            string fromContext,
            string route,
            params Type[] commandTypes)
        {
            return ListeningEvents(
                fromContext,
                route,
                null,
                commandTypes);
        }

        public ProjectionDetails ListeningEvents(
            string fromContext,
            string route,
            IEndpointResolver endpointResolver,
            params Type[] eventTypes)
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

            EventsData.Add(
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
