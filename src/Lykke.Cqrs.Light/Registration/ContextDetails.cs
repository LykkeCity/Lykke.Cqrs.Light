using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using Lykke.Cqrs.Light.Abstractions;

namespace Lykke.Cqrs.Light.Registration
{
    [PublicAPI]
    public class ContextDetails
    {
        internal string ContextName { get; }
        internal List<CommandsHandlerDetails> СommandsHandlerDetailsList { get; }
        internal List<ProjectionDetails> ProjectionDetailsList { get; }
        internal Dictionary<string, TypesData> CommandsDataDict { get; set; }
        internal TimeSpan? FailedCommandDelay { get; set; }
        internal TimeSpan? FailedEventDelay { get; set; }

        internal ContextDetails(string context)
        {
            ContextName = context;
            СommandsHandlerDetailsList = new List<CommandsHandlerDetails>();
            ProjectionDetailsList = new List<ProjectionDetails>();
            CommandsDataDict = new Dictionary<string, TypesData>();
        }

        public ContextDetails AddCommandsHandler<T>(Action<CommandsHandlerDetails> detailsSetter)
        {
            var details = new CommandsHandlerDetails(typeof(T));
            detailsSetter.Invoke(details);
            if (details.CommandsData.Count == 0)
                throw new InvalidOperationException($"Listening commands list is not specified for command handler {typeof(T).Name}");
            if (details.EventsData.Count == 0)
                throw new InvalidOperationException($"Publishing events list is not specified for command handler {typeof(T).Name}");

            СommandsHandlerDetailsList.Add(details);
            return this;
        }

        public ContextDetails AddCommandsHandler<T>(ICommandHandler<T> commandHandler, Action<CommandsHandlerDetails> detailsSetter)
        {
            var details = new CommandsHandlerDetails(commandHandler);
            detailsSetter.Invoke(details);
            if (details.CommandsData.Count == 0)
                throw new InvalidOperationException($"Listening commands list is not specified for command handler {details.CommandHandlerType.Name}");
            if (details.EventsData.Count == 0)
                throw new InvalidOperationException($"Publishing events list is not specified for command handler {details.CommandHandlerType.Name}");

            СommandsHandlerDetailsList.Add(details);
            return this;
        }

        public ContextDetails FailedCommandRetryDelay(TimeSpan failRetryDelay)
        {
            FailedCommandDelay = failRetryDelay;
            return this;
        }

        public ContextDetails AddProjection<T>(Action<ProjectionDetails> detailsSetter)
        {
            var details = new ProjectionDetails(typeof(T));
            detailsSetter.Invoke(details);
            if (details.EventsData.Count == 0)
                throw new InvalidOperationException($"Listening events list is not specified for command handler {typeof(T).Name}");

            ProjectionDetailsList.Add(details);
            return this;
        }

        public ContextDetails AddProjection<T>(IProjection<T> projection, Action<ProjectionDetails> detailsSetter)
        {
            var details = new ProjectionDetails(projection);
            detailsSetter.Invoke(details);
            if (details.EventsData.Count == 0)
                throw new InvalidOperationException($"Listening events list is not specified for command handler {typeof(T).Name}");

            ProjectionDetailsList.Add(details);
            return this;
        }

        public ContextDetails PublishingCommands(string toContext, params Type[] commandTypes)
        {
            return PublishingCommands(
                toContext,
                null,
                null,
                commandTypes);
        }

        public ContextDetails PublishingCommands(
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

        public ContextDetails PublishingCommands(
            string toContext,
            string route,
            IEndpointResolver endpointResolver,
            params Type[] commandTypes)
        {
            if (string.IsNullOrWhiteSpace(toContext))
                throw new ArgumentNullException(toContext);
            if (commandTypes == null || commandTypes.Length == 0)
                throw new ArgumentNullException();
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

        public ContextDetails FailedEventRetryDelay(TimeSpan failRetryDelay)
        {
            FailedEventDelay = failRetryDelay;
            return this;
        }
    }
}
