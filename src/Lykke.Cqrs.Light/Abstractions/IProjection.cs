using JetBrains.Annotations;
using System.Threading.Tasks;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface IProjection<in T>
    {
        Task<HandlingResult> HandleAsync(T evt);
    }
}
