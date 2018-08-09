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
        internal List<TypesData> ListeningCommandsData { get; }
        internal List<TypesData> LoopbackCommandsData { get; }
        internal List<TypesData> PublishingEventsData { get; }
        internal Dictionary<string, uint> ThreadsDict { get; }
        internal Dictionary<string, uint> QueuesDict { get; }
        internal Type CommandHandlerType { get; }
        internal object CommandHandler { get; }

        internal CommandsHandlerDetails(Type commandHandlerType)
        {
            CommandHandlerType = commandHandlerType;
            ListeningCommandsData = new List<TypesData>();
            LoopbackCommandsData = new List<TypesData>();
            PublishingEventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        internal CommandsHandlerDetails(object commandHandler)
        {
            CommandHandlerType = commandHandler.GetType();
            CommandHandler = commandHandler;
            ListeningCommandsData = new List<TypesData>();
            LoopbackCommandsData = new List<TypesData>();
            PublishingEventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        public CommandsHandlerDetails ListeningCommands([NotNull] string route, [NotNull] params Type[] commandTypes)
        {
            return ListeningCommands(
                route,
                false,
                null,
                commandTypes);
        }

        public CommandsHandlerDetails ListeningCommands(
            [NotNull] string route,
            bool withLoopback,
            [NotNull] params Type[] commandTypes)
        {
            return ListeningCommands(
                route,
                withLoopback,
                null,
                commandTypes);
        }

        public CommandsHandlerDetails ListeningCommands(
            [NotNull] string route,
            [CanBeNull] IEndpointResolver endpointResolver = null,
            [NotNull] params Type[] commandTypes)
        {
            return ListeningCommands(
                route,
                false,
                endpointResolver,
                commandTypes);
        }

        public CommandsHandlerDetails ListeningCommands(
            [NotNull] string route,
            bool withLoopback,
            [CanBeNull] IEndpointResolver endpointResolver = null,
            [NotNull] params Type[] commandTypes)
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

            ListeningCommandsData.Add(
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

        public CommandsHandlerDetails PublishingEvents([NotNull] params Type[] eventTypes)
        {
            return PublishingEvents(
                null,
                null,
                eventTypes);
        }

        public CommandsHandlerDetails PublishingEvents([CanBeNull] IEndpointResolver endpointResolver = null, [NotNull] params Type[] eventTypes)
        {
            return PublishingEvents(
                null,
                endpointResolver,
                eventTypes);
        }

        public CommandsHandlerDetails PublishingEvents([CanBeNull] string route = null, [NotNull] params Type[] eventTypes)
        {
            return PublishingEvents(
                route,
                null,
                eventTypes);
        }

        public CommandsHandlerDetails PublishingEvents(
            [CanBeNull] string route = null,
            [CanBeNull] IEndpointResolver endpointResolver = null,
            [NotNull] params Type[] eventTypes)
        {
            if (eventTypes == null || eventTypes.Length == 0)
                throw new ArgumentNullException(nameof(eventTypes));
            if (eventTypes.Any(i => i == null))
                throw new ArgumentException("Event types list can't contain null value");

            PublishingEventsData.Add(
                new TypesData
                {
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = eventTypes,
                });

            return this;
        }

        public CommandsHandlerDetails MultiThreaded([NotNull] string route, uint threadCount)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            if (threadCount == 0)
                throw new ArgumentException($"Argument {nameof(threadCount)} must have positive value");

            ThreadsDict[route] = threadCount;
            return this;
        }

        public CommandsHandlerDetails QueueCapacity([NotNull] string route, uint queueCapacity)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            if (queueCapacity == 0)
                throw new ArgumentException($"Argument {nameof(queueCapacity)} must have positive value");

            QueuesDict[route] = queueCapacity;
            return this;
        }
    }
}
