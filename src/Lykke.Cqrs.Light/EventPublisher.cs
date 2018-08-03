using Lykke.Cqrs.Light.Abstractions;
using System;

namespace Lykke.Cqrs.Light
{
    internal class EventPublisher : IEventPublisher
    {
        private readonly CqrsEngine _cqrsEngine;
        private readonly string _context;

        internal EventPublisher(CqrsEngine cqrsEngine, string context)
        {
            _cqrsEngine = cqrsEngine;
            _context = context;
        }

        public void PublishEvent(object @event)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            _cqrsEngine.PublishEvent(@event, _context);
        }
    }
}
