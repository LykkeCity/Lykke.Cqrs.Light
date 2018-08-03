using Autofac;
using Lykke.Cqrs.Light.Abstractions;
using System;

namespace Lykke.Cqrs.Light
{
    internal class DependencyResolver : IDependencyResolver
    {
        private readonly IContainer _container;

        internal DependencyResolver(IContainer container)
        {
            _container = container;
        }

        public object Resolve(Type type)
        {
            return _container.Resolve(type);
        }
    }
}
