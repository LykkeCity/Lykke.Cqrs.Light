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

        public override bool Equals(object obj)
        {
            var other = obj as RoutingKey;
            if (other == null)
                return false;

            return MessageType == other.MessageType
                && RouteType == other.RouteType
                && CommunicationType == other.CommunicationType
                && SourceContext == other.SourceContext
                && TargetContext == other.TargetContext
                && Exclusive == other.Exclusive;
        }

        public override int GetHashCode()
        {
            var hashCode = (MessageType != null ? MessageType.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ CommunicationType.GetHashCode();
            hashCode = (hashCode * 397) ^ RouteType.GetHashCode();
            hashCode = (hashCode * 397) ^ Exclusive.GetHashCode();
            hashCode = (hashCode * 397) ^ (SourceContext != null ? SourceContext.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (TargetContext != null ? TargetContext.GetHashCode() : 0);
            return hashCode;
        }
    }
}
