using System;

namespace Lykke.Cqrs.Light.Routing
{
    public class RoutingKey
    {
        internal Type MessageType { get; set; }
        internal RouteType RouteType { get; set; }
        internal CommunicationType CommunicationType { get; set; }
        internal string SourceContext { get; set; }
        internal string TargetContext { get; set; }
        internal bool Exclusive { get; set; }
    }
}
