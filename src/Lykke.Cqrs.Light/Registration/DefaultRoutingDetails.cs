using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class DefaultRoutingDetails
    {
        internal Dictionary<string, TypesData> CommandsDataDict { get; set; }

        internal DefaultRoutingDetails()
        {
            CommandsDataDict = new Dictionary<string, TypesData>();
        }

        public DefaultRoutingDetails PublishingCommands(string toContext, params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                null,
                null,
                commandTypes);
        }

        public DefaultRoutingDetails PublishingCommands(
            string toContext,
            string route,
            params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                route,
                null,
                commandTypes);
        }

        public DefaultRoutingDetails PublishingCommands(
            string toContext,
            IEndpointResolver endpointResolver,
            params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                null,
                endpointResolver,
                commandTypes);
        }

        public DefaultRoutingDetails PublishingCommands(
            string toContext,
            string route,
            IEndpointResolver endpointResolver,
            params Type[] commandTypes)
        {
            if (string.IsNullOrWhiteSpace(toContext))
                throw new ArgumentNullException(nameof(toContext));
            if (commandTypes == null || commandTypes.Length == 0)
                throw new ArgumentNullException(nameof(commandTypes));
            if (commandTypes.Any(i => i == null))
                throw new ArgumentException("Command types list can't contain null value");
            if (CommandsDataDict.ContainsKey(toContext))
                throw new ArgumentException($"Command types are already registered for context {toContext}");

            CommandsDataDict.Add(
                toContext,
                new TypesData
                {
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = commandTypes
                });

            return this;
        }
    }
}
