using JetBrains.Annotations;
using Lykke.Cqrs.Light.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class SagaDetails
    {
        internal string ContextName { get; }
        internal List<TypesData> PublishingCommandsData { get; }
        internal List<TypesData> ListeningEventsData { get;  }
        internal Dictionary<string, uint> ThreadsDict { get; }
        internal Dictionary<string, uint> QueuesDict { get; }
        internal Type SagaType { get; }
        internal object Saga { get; }

        internal TimeSpan? FailedEventDelay { get; set; }

        internal SagaDetails(string contextName, Type sagaType)
        {
            ContextName = contextName;
            SagaType = sagaType;
            PublishingCommandsData = new List<TypesData>();
            ListeningEventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        internal SagaDetails(string contextName, object saga)
        {
            ContextName = contextName;
            Saga = saga;
            SagaType = saga.GetType();
            PublishingCommandsData = new List<TypesData>();
            ListeningEventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        public SagaDetails ListeningEvents(
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

        public SagaDetails ListeningEvents(
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
                throw new ArgumentException("Event types list can't contain null value");
            var interfaceType = typeof(IEventHandler<>);
            foreach (Type eventType in eventTypes)
            {
                var mustImplementType = interfaceType.MakeGenericType(eventType);
                if (!mustImplementType.IsAssignableFrom(SagaType))
                    throw new InvalidOperationException($"Saga {SagaType.Name} must implement IEventHandler<{eventType.Name}> interface");
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

        public SagaDetails PublishingCommands([NotNull] string toContext, [NotNull] params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                null,
                null,
                commandTypes);
        }

        public SagaDetails PublishingCommands(
            [NotNull] string toContext,
            [CanBeNull] string route = null,
            [NotNull] params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                route,
                null,
                commandTypes);
        }

        public SagaDetails PublishingCommands(
            [NotNull] string toContext,
            [CanBeNull] IEndpointResolver endpointResolver = null,
            [NotNull] params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                null,
                endpointResolver,
                commandTypes);
        }

        public SagaDetails PublishingCommands(
            [NotNull] string toContext,
            [CanBeNull] string route = null,
            [CanBeNull] IEndpointResolver endpointResolver = null,
            [NotNull] params Type[] commandTypes)
        {
            if (string.IsNullOrWhiteSpace(toContext))
                throw new ArgumentNullException(nameof(toContext));
            if (commandTypes == null || commandTypes.Length == 0)
                throw new ArgumentNullException(nameof(commandTypes));
            if (commandTypes.Any(i => i == null))
                throw new ArgumentException("Command types list can't contain null value");

            PublishingCommandsData.Add(
                new TypesData
                {
                    Context = toContext,
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = commandTypes,
                });

            return this;
        }

        public SagaDetails MultiThreaded([NotNull] string route, uint threadCount)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            if (threadCount == 0)
                throw new ArgumentException($"Argument {nameof(threadCount)} must have positive value");

            ThreadsDict[route] = threadCount;
            return this;
        }

        public SagaDetails QueueCapacity([NotNull] string route, uint queueCapacity)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            if (queueCapacity == 0)
                throw new ArgumentException($"Argument {nameof(queueCapacity)} must have positive value");

            QueuesDict[route] = queueCapacity;
            return this;
        }

        public SagaDetails FailedEventRetryDelay(TimeSpan failRetryDelay)
        {
            if (failRetryDelay.Ticks <= 0)
                throw new ArgumentException("Delay must have some non-negative duration");

            FailedEventDelay = failRetryDelay;
            return this;
        }
    }
}
