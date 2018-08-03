using Lykke.Common.Log;
using Lykke.Cqrs.Light.Abstractions;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Lykke.Cqrs.Light.Tests
{
    public class CommandDispatcherTests : IDisposable
    {
        private readonly ILogFactory _logFactory;

        public CommandDispatcherTests()
        {
            _logFactory = LogFactory.Create().AddUnbufferedConsole();
        }

        public void Dispose()
        {
            _logFactory?.Dispose();
        }

        [Fact]
        public void WireTest()
        {
            var dispatcher = new CommandDispatcher(_logFactory);
            var commandHandler = new CommandHandler();
            dispatcher.AddHandler(commandHandler, null, typeof(string), typeof(int));
            dispatcher.Dispatch("test", (delay, acknowledge) => { }, "route");
            dispatcher.Dispatch(1, (delay, acknowledge) => { }, "route");
            Assert.Equal(commandHandler.HandledCommands, new object[] { "test", 1 });
        }

        [Fact]
        public void MultipleHandlersAreNotAllowedDispatchTest()
        {
            var dispatcher = new CommandDispatcher(_logFactory);
            var handler1 = new CommandHandler();
            var handler2 = new CommandHandler();

            dispatcher.AddHandler(handler1, null, typeof(string));

            Assert.Throws<InvalidOperationException>(() => dispatcher.AddHandler(handler2, null, typeof(string)));
        }

        [Fact]
        public void DispatchOfUnknownCommandShouldFailTest()
        {
            var dispatcher = new CommandDispatcher(_logFactory);
            var ack = true;
            dispatcher.Dispatch("testCommand", (delay, acknowledge) => { ack = acknowledge; }, "route");
            Assert.False(ack);
        }

        [Fact]
        public void FailingCommandTest()
        {
            var dispatcher = new CommandDispatcher(_logFactory);
            var commandHandler = new CommandHandler();
            dispatcher.AddHandler(commandHandler, null, typeof(DateTime));
            bool ack = true;
            dispatcher.Dispatch(DateTime.Now, (delay, acknowledge) => { ack = false; }, "route");
            Assert.False(ack);
        }
    }

    class CommandHandler : ICommandHandler<string>, ICommandHandler<int>, ICommandHandler<DateTime>
    {
        public readonly List<object> HandledCommands = new List<object>();

        public Task<HandlingResult> HandleAsync(string command, IEventPublisher eventPublisher)
        {
            HandledCommands.Add(command);

            return Task.FromResult(HandlingResult.Ok());
        }

        public Task<HandlingResult> HandleAsync(int command, IEventPublisher eventPublisher)
        {
            HandledCommands.Add(command);

            return Task.FromResult(HandlingResult.Ok());
        }

        public Task<HandlingResult> HandleAsync(DateTime command, IEventPublisher eventPublisher)
        {
            throw new Exception();
        }
    }
}
