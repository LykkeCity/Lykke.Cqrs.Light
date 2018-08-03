using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Messaging.Contract;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Cqrs.Light
{
    internal class CommandDispatcher
    {
        private readonly Dictionary<Type, (Func<object, Task<HandlingResult>>, string)> _handlersDict =
            new Dictionary<Type, (Func<object, Task<HandlingResult>>, string)>();
        private readonly ILog _log;
        private readonly long _failedCommandRetryDelayInMs;

        internal CommandDispatcher(ILogFactory logFactory, TimeSpan? failedCommandRetryDelay = null)
        {
            _log = logFactory.CreateLog(this);
            _failedCommandRetryDelayInMs = failedCommandRetryDelay.HasValue
                ? (long)failedCommandRetryDelay.Value.TotalMilliseconds
                : 60000;
        }

        internal CommandDispatcher(ILog log, TimeSpan? failedCommandRetryDelay = null)
        {
            _log = log;
            _failedCommandRetryDelayInMs = failedCommandRetryDelay.HasValue
                ? (long)failedCommandRetryDelay.Value.TotalMilliseconds
                : 60000;
        }

        internal void AddHandler(
            object commandHandler,
            IEventPublisher eventPublisher,
            params Type[] commandTypes)
        {
            Type handlerType = commandHandler.GetType();

            foreach (var commandType in commandTypes)
            {
                if (_handlersDict.ContainsKey(commandType))
                    throw new InvalidOperationException(
                        $"Command handler for command {commandType.Name} is already registered. Can't register for {handlerType.Name}.");

                var methodInfo = handlerType.GetMethod(nameof(ICommandHandler<object>.HandleAsync), new[] { commandType, typeof(IEventPublisher) });
                if (methodInfo == null)
                    throw new InvalidOperationException($"Command handler {handlerType.Name} must implement ICommandHandler<{commandType.Name}>.");

                _handlersDict.Add(commandType, (c => (Task<HandlingResult>)methodInfo.Invoke(commandHandler, new[] { c, eventPublisher }), handlerType.Name));
            }
        }

        internal void Dispatch(
            object command,
            AcknowledgeDelegate acknowledge,
            string targetContext)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (acknowledge == null)
                throw new ArgumentNullException(nameof(acknowledge));

            string commandType = command.GetType().Name;

            if (!_handlersDict.TryGetValue(command.GetType(), out var handlerData))
            {
                _log.WriteWarning(commandType, command, $"No handler was registered for a command of type {commandType}");
                acknowledge(_failedCommandRetryDelayInMs, false);
                return;
            }

            string handlerTypeName = handlerData.Item2;
            _log.WriteInfo(handlerTypeName, command, $"Command of type {commandType} is being processed");

            var telemtryOperation = TelemetryHelper.InitTelemetryOperation(
                "Cqrs handle command",
                handlerTypeName,
                commandType,
                targetContext);
            try
            {
                HandlingResult result = handlerData.Item1(command).GetAwaiter().GetResult();
                acknowledge(result.RetryDelay.HasValue ? (long)result.RetryDelay.Value.TotalMilliseconds : _failedCommandRetryDelayInMs, !result.Retry);
            }
            catch (Exception ex)
            {
                _log.WriteError(handlerTypeName, $"{commandType}: {command.ToJson()}", ex);

                acknowledge(_failedCommandRetryDelayInMs, false);

                TelemetryHelper.SubmitException(telemtryOperation, ex);
            }
            finally
            {
                TelemetryHelper.SubmitOperationResult(telemtryOperation);
            }
        }
    }
}
