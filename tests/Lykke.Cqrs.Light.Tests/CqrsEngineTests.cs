using Lykke.Common.Log;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Cqrs.Light.Registration;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Lykke.Messaging;
using Lykke.Messaging.Contract;
using Lykke.Messaging.Serialization;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lykke.Cqrs.Light.Tests
{
    public class CqrsEngineTests : IDisposable
    {
        private readonly ILogFactory _logFactory;

        public CqrsEngineTests()
        {
            _logFactory = LogFactory.Create().AddUnbufferedConsole();
        }

        public void Dispose()
        {
            _logFactory?.Dispose();
        }

        [Fact]
        public void ListenSameCommandOnDifferentEndpointsTest()
        {
            using (var messagingEngine = new MessagingEngine(
                _logFactory,
                new TransportResolver(
                    new Dictionary<string, TransportInfo>
                    {
                        {"InMemory", new TransportInfo("none", "none", "none", null)}
                    })))
            {
                var cqrsCommandHandler = new CqrsCommandHandler();
                using (var engine = new CqrsEngine(
                    _logFactory,
                    messagingEngine,
                    null,
                    new InMemoryEndpointResolver(),
                    Register.Context("bc", d =>
                        d.AddCommandsHandler(cqrsCommandHandler, c =>
                            c.PublishingEvents("eventExchange", typeof(int))
                            .ListeningCommands("exchange1", typeof(string))
                            .ListeningCommands("exchange2", typeof(string))))))
                {
                    engine.Init(null);
                    messagingEngine.Send("test1", new Endpoint("InMemory", "exchange1", serializationFormat: SerializationFormat.Json));
                    messagingEngine.Send("test2", new Endpoint("InMemory", "exchange2", serializationFormat: SerializationFormat.Json));
                    messagingEngine.Send("test3", new Endpoint("InMemory", "exchange3", serializationFormat: SerializationFormat.Json));
                    Thread.Sleep(6000);
                    Assert.Equal(2, cqrsCommandHandler.AcceptedCommands.Count);
                    Assert.Contains("test1", cqrsCommandHandler.AcceptedCommands);
                    Assert.Contains("test2", cqrsCommandHandler.AcceptedCommands);
                }
            }
        }

        [Fact]
        public void ContextUsesDefaultRouteForCommandPublishingIfItDoesNotHaveItsOwnTest()
        {
            var bcCommands = new Endpoint("InMemory", "bcCommands", serializationFormat: SerializationFormat.Json);
            var defaultCommands = new Endpoint("InMemory", "defaultCommands", serializationFormat: SerializationFormat.Json);
            using (var messagingEngine = new MessagingEngine(
                _logFactory,
                new TransportResolver(
                    new Dictionary<string, TransportInfo>
                    {
                        {"InMemory", new TransportInfo("none", "none", "none", null)}
                    })))
            {
                using (var engine = new CqrsEngine(
                    _logFactory,
                    messagingEngine,
                    null,
                    new InMemoryEndpointResolver(),
                    Register.Context("bc2", d => d.PublishingCommands("bc1", "bcCommands", typeof(int))),
                    Register.DefaultRouting(d => d.PublishingCommands("bc1", "defaultCommands", typeof(string), typeof(int))
                    )))
                {
                    engine.Init(null);
                    var received = new AutoResetEvent(false);
                    using (messagingEngine.Subscribe(defaultCommands, o => received.Set(), s => { }, typeof(string)))
                    {
                        engine.SendCommand("test", "bc2", "bc1");
                        var wait = received.WaitOne(2000);
                        Assert.True(wait);
                    }
                    using (messagingEngine.Subscribe(bcCommands, o => received.Set(), s => { }, typeof(int)))
                    {
                        engine.SendCommand(1, "bc2", "bc1");
                        Assert.True(received.WaitOne(2000));
                    }
                }
            }
        }

        [Fact]
        public void FluentApiTest()
        {
            var messagingEngine =
                new MessagingEngine(
                    _logFactory,
                    new TransportResolver(new Dictionary<string, TransportInfo>
                    {
                        {"InMemory", new TransportInfo("none", "none", "none", null)},
                        {"rmq", new TransportInfo("none", "none", "none", null)}
                    }));
            var commandHandler = new CqrsCommandHandler();
            var projection = new CqrsProjection();
            var saga = new CqrsSaga();
            using (messagingEngine)
            {
                var engine = new CqrsEngine(
                    _logFactory,
                    messagingEngine,
                    null,
                    new InMemoryEndpointResolver(),
                    Register.Context("bc", d =>
                        d.FailedCommandRetryDelay(TimeSpan.FromMinutes(1))
                        .FailedEventRetryDelay(TimeSpan.FromMinutes(1))
                        .PublishingCommands("operations", "operationsCommandsRoute", typeof(string))
                        .AddProjection(projection, pd => pd.ListeningEvents("operations", "operationEventsRoute", typeof(int)))
                        .AddCommandsHandler(commandHandler, c =>
                            c.ListeningCommands("commandsRoute", true, typeof(string))
                            .ListeningCommands("explicitlyPrioritizedCommandsRoute", typeof(string))
                            .ListeningCommands("prioritizedCommandsRoute", new InMemoryEndpointResolver(), typeof(string))
                            .PublishingEvents("eventsRoute", typeof(int))
                            .MultiThreaded("explicitlyPrioritizedCommandsRoute", 10)
                            .QueueCapacity("explicitlyPrioritizedCommandsRoute", 1024)
                            .MultiThreaded("prioritizedCommandsRoute", 10)
                            .QueueCapacity("prioritizedCommandsRoute", 1024))),
                    Register.Saga(saga, "saga", d =>
                        d.ListeningEvents("operations", "operationEventsRoute", typeof(int))
                        .FailedEventRetryDelay(TimeSpan.FromMinutes(1))
                        .PublishingCommands("operations", "operationsCommandsRoute", typeof(string))
                        .MultiThreaded("operationEventsRoute", 2)
                        .QueueCapacity("operationEventsRoute", 2)
                        .MultiThreaded("operationsCommandsRoute", 2)
                        .QueueCapacity("operationsCommandsRoute", 2)),
                    Register.DefaultRouting(d =>
                        d.PublishingCommands("operations", "defaultCommandsRoute", typeof(string), typeof(int)))
                );
                engine.Init(null);
            }
        }
    }

    class CqrsCommandHandler : ICommandHandler<string>
    {
        public List<object> AcceptedCommands = new List<object>();

        private int _processingTimeout;

        public CqrsCommandHandler(int processingTimeout)
        {
            _processingTimeout = processingTimeout;
        }
        public CqrsCommandHandler()
            : this(0)
        {
        }

        public Task<HandlingResult> HandleAsync(string command, IEventPublisher eventPublisher)
        {
            Thread.Sleep(_processingTimeout);

            AcceptedCommands.Add(command);

            return Task.FromResult(HandlingResult.Ok());
        }
    }

    class CqrsSaga : IEventHandler<int>
    {
        public static List<string> Messages = new List<string>();
        public static ManualResetEvent Complete = new ManualResetEvent(false);

        public Task<HandlingResult> HandleAsync(int @event, ICommandSender sender)
        {
            var message = $"Event is caught by saga: {@event}";
            Messages.Add(message);

            Complete.Set();

            return Task.FromResult(HandlingResult.Ok());
        }
    }

    class CqrsProjection : IProjection<int>
    {
        public Task<HandlingResult> HandleAsync(int evt)
        {
            throw new NotImplementedException();
        }
    }
}
