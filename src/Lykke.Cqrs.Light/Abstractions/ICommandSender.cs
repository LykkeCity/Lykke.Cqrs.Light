using JetBrains.Annotations;

namespace Lykke.Cqrs.Light.Abstractions
{
    [PublicAPI]
    public interface ICommandSender
    {
        void SendCommand(object command, string targetContext);
        void SendCommand(object command, string sourceContext, string targetContext);
    }
}
