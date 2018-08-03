using JetBrains.Annotations;
using System.Threading.Tasks;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface IEventHandler<in T>
    {
        Task<HandlingResult> HandleAsync(T evt, ICommandSender commandSender);
    }
}
