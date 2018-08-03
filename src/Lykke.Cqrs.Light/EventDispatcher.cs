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
    internal class EventDispatcher
    {
        internal const long FailedEventRetryDelay = 60000;

        private readonly Dictionary<Type, Dictionary<string, (Func<object, Task<HandlingResult>>, string)>> _handlersDict =
            new Dictionary<Type, Dictionary<string, (Func<object, Task<HandlingResult>>, string)>>();
        private readonly ICommandSender _commandSender;
        private readonly ILog _log;
        private readonly long _failedEventRetryDelayInMs;

        internal EventDispatcher(
            ICommandSender commandSender,
            ILogFactory logFactory,
            TimeSpan? failedEventRetryDelay = null)
        {
            _commandSender = commandSender;
            _log = logFactory.CreateLog(this);
            _failedEventRetryDelayInMs = failedEventRetryDelay.HasValue
                ? (long)failedEventRetryDelay.Value.TotalMilliseconds
                : FailedEventRetryDelay;
        }

        internal EventDispatcher(
            ICommandSender commandSender,
            ILog log,
            TimeSpan? failedEventRetryDelay = null)
        {
            _commandSender = commandSender;
            _log = log;
            _failedEventRetryDelayInMs = failedEventRetryDelay.HasValue
                ? (long)failedEventRetryDelay.Value.TotalMilliseconds
                : FailedEventRetryDelay;
        }

        internal void AddHandler(
            Type eventType,
            string context,
            object eventHandler)
        {
            if (!_handlersDict.ContainsKey(eventType))
                _handlersDict.Add(eventType, new Dictionary<string, (Func<object, Task<HandlingResult>>, string)>());
            Type handlerType = eventHandler.GetType();
            var contextDict = _handlersDict[eventType];
            if (contextDict.ContainsKey(context))
                throw new InvalidOperationException(
                    $"Event handler for event {eventType.Name} is already registered in context {context}. Can't register for {handlerType.Name}.");

            var methodInfo = handlerType.GetMethod(nameof(IEventHandler<object>.HandleAsync), new[] { eventType, typeof(ICommandSender) });
            if (methodInfo == null)
                throw new InvalidOperationException($"Event handler {handlerType.Name} must implement IEventHandler<{eventType.Name}>.");

            contextDict.Add(context, (e => (Task<HandlingResult>)methodInfo.Invoke(eventHandler, new [] { e, _commandSender}), handlerType.Name));
        }

        internal void AddProjectionHandler(
            Type eventType,
            string context,
            object eventHandler)
        {
            if (!_handlersDict.ContainsKey(eventType))
                _handlersDict.Add(eventType, new Dictionary<string, (Func<object, Task<HandlingResult>>, string)>());
            Type handlerType = eventHandler.GetType();
            var contextDict = _handlersDict[eventType];
            if (contextDict.ContainsKey(context))
                throw new InvalidOperationException(
                    $"Event handler for event {eventType.Name} is already registered in context {context}. Can't register for {handlerType.Name}.");

            var methodInfo = handlerType.GetMethod(nameof(IProjection<object>.HandleAsync), new[] { eventType });
            if (methodInfo == null)
                throw new InvalidOperationException($"Event handler {handlerType.Name} must implement IProjection<{eventType.Name}>.");

            contextDict.Add(context, (e => (Task<HandlingResult>)methodInfo.Invoke(eventHandler, new[] { e }), handlerType.Name));
        }

        internal void Dispatch(
            object @event,
            AcknowledgeDelegate acknowledge,
            string targetContext)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            if (acknowledge == null)
                throw new ArgumentNullException(nameof(acknowledge));

            Type eventType = @event.GetType();
            if (!_handlersDict.TryGetValue(eventType, out var contextDict)
                || !contextDict.TryGetValue(targetContext, out var handleData))
            {
                acknowledge(0, true);
                return;
            }

            string eventHandlerTypeName = handleData.Item2;
            _log.WriteInfo(eventHandlerTypeName, @event, $"Event of type {eventType.Name} is being processed for context '{targetContext}'");

            var telemtryOperation = TelemetryHelper.InitTelemetryOperation(
                "Cqrs handle events",
                eventHandlerTypeName,
                eventType.Name,
                targetContext);
            try
            {
                HandlingResult result = handleData.Item1(@event).GetAwaiter().GetResult();
                if (result.Retry)
                    acknowledge(result.RetryDelay.HasValue ? (long)result.RetryDelay.Value.TotalMilliseconds : _failedEventRetryDelayInMs, false);
                else
                    acknowledge(0, true);
            }
            catch (Exception ex)
            {
                _log.WriteError(eventHandlerTypeName, $"{eventType.Name}: {@event.ToJson()}", ex);

                acknowledge(_failedEventRetryDelayInMs, false);

                TelemetryHelper.SubmitException(telemtryOperation, ex);
            }
            finally
            {
                TelemetryHelper.SubmitOperationResult(telemtryOperation);
            }
        }
    }
}
