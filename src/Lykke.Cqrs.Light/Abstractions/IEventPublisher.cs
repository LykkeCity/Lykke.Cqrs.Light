using JetBrains.Annotations;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface IEventPublisher
    {
        void PublishEvent(object @event);
    }
}
