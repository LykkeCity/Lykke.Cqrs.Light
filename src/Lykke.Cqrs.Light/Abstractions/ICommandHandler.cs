using JetBrains.Annotations;
using System.Threading.Tasks;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface ICommandHandler<in T>
    {
        Task<HandlingResult> HandleAsync(T command, IEventPublisher eventPublisher);
    }
}
