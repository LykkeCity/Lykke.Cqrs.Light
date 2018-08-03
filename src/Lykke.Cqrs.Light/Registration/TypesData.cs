using Lykke.Cqrs.Light.Abstractions;
using System;

namespace Lykke.Cqrs.Light.Registration
{
    internal class TypesData
    {
        internal string Context { get; set; }
        internal string Route { get; set; }
        internal IEndpointResolver EndpointResolver { get; set; }
        internal Type[] Types { get; set; }
    }
}
