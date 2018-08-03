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
        internal Type SagaType { get; }
        internal List<TypesData> CommandsData { get;  }
        internal List<TypesData> EventsData { get;  }
        internal Dictionary<string, uint> ThreadsDict { get; }
        internal Dictionary<string, uint> QueuesDict { get; }
        internal object Saga { get; }

        internal TimeSpan? FailedEventDelay { get; set; }

        internal SagaDetails(string contextName, Type sagaType)
        {
            ContextName = contextName;
            SagaType = sagaType;
            CommandsData = new List<TypesData>();
            EventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        internal SagaDetails(string contextName, object saga)
        {
            ContextName = contextName;
            Saga = saga;
            SagaType = saga.GetType();
            CommandsData = new List<TypesData>();
            EventsData = new List<TypesData>();
            ThreadsDict = new Dictionary<string, uint>();
            QueuesDict = new Dictionary<string, uint>();
        }

        public SagaDetails ListeningEvents(
            string fromContext,
            string route,
            params Type[] eventTypes)
        {
            return ListeningEvents(
                fromContext,
                route,
                null,
                eventTypes);
        }

        public SagaDetails ListeningEvents(
            string fromContext,
            string route,
            IEndpointResolver endpointResolver,
            params Type[] eventTypes)
        {
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

        public SagaDetails PublishingCommands(string toContext, params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                null,
                null,
                commandTypes);
        }

        public SagaDetails PublishingCommands(
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

        public SagaDetails PublishingCommands(
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

        public SagaDetails PublishingCommands(
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

            CommandsData.Add(
                new TypesData
                {
                    Context = toContext,
                    Route = route,
                    EndpointResolver = endpointResolver,
                    Types = commandTypes,
                });

            return this;
        }

        public SagaDetails MultiThreaded(string route, uint threadCount)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            ThreadsDict[route] = threadCount;
            return this;
        }

        public SagaDetails QueueCapacity(string route, uint queueCapacity)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));
            ThreadsDict[route] = queueCapacity;
            return this;
        }

        public SagaDetails FailedEventRetryDelay(TimeSpan failRetryDelay)
        {
            FailedEventDelay = failRetryDelay;
            return this;
        }
    }
}
