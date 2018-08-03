using JetBrains.Annotations;
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
    public class EventDispatcherTests : IDisposable
    {
        private readonly ILogFactory _logFactory;

        public EventDispatcherTests()
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
            var dispatcher = new EventDispatcher(null, _logFactory);
            var handler = new EventHandler();
            dispatcher.AddHandler(typeof(string), "testBC", handler);
            dispatcher.AddHandler(typeof(int), "testBC", handler);
            dispatcher.Dispatch("test", (delay, acknowledge) => { }, "testBC");
            dispatcher.Dispatch(1, (delay, acknowledge) => { }, "testBC");
            Assert.Equal(handler.HandledEvents, new object[] { "test", 1 });
        }

        [Fact]
        public void MultipleHandlersDispatchTest()
        {
            var dispatcher = new EventDispatcher(null, _logFactory);
            var handler1 = new EventHandler();
            var handler2 = new EventHandler();
            dispatcher.AddHandler(typeof(string), "testBC", handler1);
            dispatcher.AddHandler(typeof(string), "testBC2", handler2);
            dispatcher.Dispatch("test", (delay, acknowledge) => { }, "testBC");
            Assert.Equal(handler1.HandledEvents, new[] { "test" });
            dispatcher.Dispatch("test", (delay, acknowledge) => { }, "testBC2");
            Assert.Equal(handler2.HandledEvents, new[] { "test" });
        }

        [Fact]
        public void FailingHandlersRegistrationWithSameContextTest()
        {
            var dispatcher = new EventDispatcher(null, _logFactory);
            var handler1 = new EventHandler();
            var handler2 = new EventHandler(true);
            dispatcher.AddHandler(typeof(string), "testBC", handler1);
            Assert.Throws<InvalidOperationException>(() => dispatcher.AddHandler(typeof(string), "testBC", handler2));
        }

        [Fact]
        public void FailingHandlersDispatchTest()
        {
            var dispatcher = new EventDispatcher(null, _logFactory);
            var handler1 = new EventHandler();
            var handler2 = new EventHandler(true);
            dispatcher.AddHandler(typeof(string), "testBC", handler1);
            dispatcher.AddHandler(typeof(string), "testBC2", handler2);
            Tuple<long, bool> result = null;
            dispatcher.Dispatch("test", (delay, acknowledge) => { result = Tuple.Create(delay, acknowledge); }, "testBC");
            Assert.Equal(handler1.HandledEvents, new[] { "test" });
            Assert.NotNull(result);
            Assert.Equal(0, result.Item1);
            Assert.True(result.Item2);
            dispatcher.Dispatch("test", (delay, acknowledge) => { result = Tuple.Create(delay, acknowledge); }, "testBC2");
            Assert.Equal(handler2.HandledEvents, new[] { "test" });
            Assert.NotNull(result);
            Assert.Equal(EventDispatcher.FailedEventRetryDelay, result.Item1);
            Assert.False(result.Item2);
        }

        [Fact]
        public void RetryingHandlersDispatchTest()
        {
            var dispatcher = new EventDispatcher(null, _logFactory);
            var handler = new EventHandler();
            dispatcher.AddHandler(typeof(Exception), "testBC", handler);
            Tuple<long, bool> result = null;
            dispatcher.Dispatch(new Exception(), (delay, acknowledge) => { result = Tuple.Create(delay, acknowledge); }, "testBC");
            Assert.NotNull(result);
            Assert.False(result.Item2);
            Assert.Equal(100, result.Item1);
        }

        [Fact]
        public void BatchDispatchTest()
        {
            var dispatcher = new EventDispatcher(null, _logFactory);
            var handler = new EventHandler();
            dispatcher.AddHandler(typeof(string), "testBC", handler);
            Tuple<long, bool> result = null;
            handler.FailOnce = true;
            dispatcher.Dispatch("a", (delay, acknowledge) => { result = Tuple.Create(delay, acknowledge); }, "testBC");
            dispatcher.Dispatch("b", (delay, acknowledge) => { }, "testBC");
            dispatcher.Dispatch("с", (delay, acknowledge) => { }, "testBC");

            Assert.NotNull(result);
            Assert.False(result.Item2);
            Assert.Equal(3, handler.HandledEvents.Count);
        }

        [Fact]
        public void TestExceptionForEventHadler()
        {
            var dispatcher = new EventDispatcher(null, _logFactory);
            var asyncHandler = new EventHandlerWithException();
            dispatcher.AddHandler(typeof(string), "testBC", asyncHandler);
            int failedCount = 0;
            dispatcher.Dispatch(
                "test",
                (delay, acknowledge) =>
                {
                    if (!acknowledge)
                        ++failedCount;
                },
                "testBC");
            Assert.True(1 == failedCount, "Async event handler was not processed properly");
        }
    }

    class EventHandlerWithException : IEventHandler<string>
    {
        [UsedImplicitly]
        public Task<HandlingResult> HandleAsync(string e, ICommandSender commandSender)
        {
            throw new InvalidOperationException();
        }
    }

    class EventHandler : IEventHandler<string>, IEventHandler<int>, IEventHandler<Exception>
    {
        private readonly bool _fail;

        internal readonly List<object> HandledEvents = new List<object>();

        internal bool FailOnce { get; set; }

        internal EventHandler(bool fail = false)
        {
            _fail = fail;
        }

        public Task<HandlingResult> HandleAsync(string e, ICommandSender commandSender)
        {
            HandledEvents.Add(e);
            if (_fail || FailOnce)
            {
                FailOnce = false;
                throw new Exception();
            }
            return Task.FromResult(HandlingResult.Ok());
        }

        public Task<HandlingResult> HandleAsync(int e, ICommandSender commandSender)
        {
            HandledEvents.Add(e);
            return _fail
                ? Task.FromResult(HandlingResult.Fail(TimeSpan.FromMilliseconds(600)))
                : Task.FromResult(HandlingResult.Ok());
        }

        public Task<HandlingResult> HandleAsync(Exception e, ICommandSender commandSender)
        {
            HandledEvents.Add(e);
            return Task.FromResult(HandlingResult.Fail(TimeSpan.FromMilliseconds(100)));
        }
    }
}
