using Lykke.Messaging.Contract;

namespace Lykke.Cqrs.Light.Routing
{
    internal class PublishRoute
    {
        internal Endpoint Endpoint { get; set; }
        internal string ProcessingGroupName { get; set; }
        internal string TargetContext { get; set; }
    }
}
