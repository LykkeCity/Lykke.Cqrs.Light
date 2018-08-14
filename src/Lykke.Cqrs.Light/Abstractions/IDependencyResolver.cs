using JetBrains.Annotations;
using System;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface IDependencyResolver
    {
        object Resolve(Type type);
    }
}
