using JetBrains.Annotations;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface IRegistration
    {
        Context CreateContext(ICqrsEngine cqrsEngine);
    }
}
