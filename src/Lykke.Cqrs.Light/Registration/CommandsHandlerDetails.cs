using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class CommandsHandlerDetails
    {
        internal List<TypesData> CommandsData { get; set; }
        internal List<TypesData> LoopbackCommandsData { get; set; }
        internal List<TypesData> EventsData { get; set; }
        internal Dictionary<string, uint> ThreadsDict { get; set; }
        internal Dictionary<string, uint> QueuesDict { get; set; }
        internal Type CommandHandlerType { get; set; }
        internal object CommandHandler { get; set; }

        internal CommandsHandlerDetails(Type commandHandlerType)
        {
            CommandHandlerType = commandHandlerType;
            CommandsData = new List<TypesData>();
            LoopbackCommandsData = new List<TypesData>();
            EventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        internal CommandsHandlerDetails(object commandHandler)
        {
            CommandHandlerType = commandHandler.GetType();
            CommandHandler = commandHandler;
            CommandsData = new List<TypesData>();
            LoopbackCommandsData = new List<TypesData>();
            EventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        public CommandsHandlerDetails ListeningCommands(string route, params Type[] commandTypes)
        {
            return ListeningCommands(
                route,
                false,
                null,
                commandTypes);
        }

        public CommandsHandlerDetails ListeningCommands(
            string route,
            bool withLoopback,
            params Type[] commandTypes)
        {
            return ListeningCommands(
                route,
                withLoopback,
                null,
                commandTypes);
        }

        public CommandsHandlerDetails ListeningCommands(
            string route,
            IEndpointResolver endpointResolver,
            params Type[] commandTypes)
        {
            return ListeningCommands(
                route,
                false,
                endpointResolver,
                commandTypes);
        }

        public CommandsHandlerDetails ListeningCommands(
            string route,
            bool withLoopback,
            IEndpointResolver endpointResolver,
            params Type[] commandTypes)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentNullException(nameof(route));
            if (commandTypes == null || commandTypes.Length == 0)
                throw new ArgumentNullException(nameof(commandTypes));
            if (commandTypes.Any(i => i == null))
                throw new ArgumentException("Command types list can't contain null value");
            Type interfaceGenericType = typeof(ICommandHandler<>);
            foreach (Type commandType in commandTypes)
            {
                var mustImplementType = interfaceGenericType.MakeGenericType(commandType);
                if (!mustImplementType.IsAssignableFrom(CommandHandlerType))
                    throw new InvalidOperationException($"Command handler {CommandHandlerType.Name} must implement ICommandHandler<{commandType.Name}> interface");
            }

            CommandsData.Add(
                new TypesData
                {
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = commandTypes,
                });

            if (withLoopback)
                LoopbackCommandsData.Add(
                    new TypesData
                    {
                        Route = route,
                        EndpointResolver = endpointResolver,
                        Types = commandTypes
                    });

            return this;
        }

        public CommandsHandlerDetails PublishingEvents(params Type[] eventTypes)
        {
            return PublishingEvents(
                null,
                null,
                eventTypes);
        }

        public CommandsHandlerDetails PublishingEvents(IEndpointResolver endpointResolver, params Type[] eventTypes)
        {
            return PublishingEvents(
                null,
                endpointResolver,
                eventTypes);
        }

        public CommandsHandlerDetails PublishingEvents(string route, params Type[] eventTypes)
        {
            return PublishingEvents(
                route,
                null,
                eventTypes);
        }

        public CommandsHandlerDetails PublishingEvents(
            string route,
            IEndpointResolver endpointResolver,
            params Type[] eventTypes)
        {
            if (eventTypes == null || eventTypes.Length == 0)
                throw new ArgumentNullException(nameof(eventTypes));
            if (eventTypes.Any(i => i == null))
                throw new ArgumentException("Event types list can't contain null value");

            EventsData.Add(
                new TypesData
                {
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = eventTypes,
                });

            return this;
        }

        public CommandsHandlerDetails MultiThreaded(string route, uint threadCount)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            ThreadsDict[route] = threadCount;
            return this;
        }

        public CommandsHandlerDetails QueueCapacity(string route, uint queueCapacity)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            ThreadsDict[route] = queueCapacity;
            return this;
        }
    }
}
