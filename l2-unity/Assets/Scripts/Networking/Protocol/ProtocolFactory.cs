/// <summary>
/// Fábrica de protocolos. Mantiene InterludeProtocol intacto y crea la rama
/// Lucera166Protocol; el perfil decide cuál se usa.
/// </summary>
public sealed class ProtocolFactory : IProtocolFactory
{
    private readonly IIncomingPacketAutoRegistry _packets;
    private readonly IOutgoingPacketAutoRegistry _outgoing;

    public ProtocolFactory(
        IIncomingPacketAutoRegistry packets,
        IOutgoingPacketAutoRegistry outgoing)
    {
        _packets = packets;
        _outgoing = outgoing;
    }

    public IProtocol Create(ServerProfile profile)
    {
        if (profile == null)
        {
            profile = ServerProfile.Default;
        }

        switch (profile.Protocol)
        {
            case ChronicleProtocol.LuceraClassic166:
                UnityEngine.Debug.Log("[Protocol] Lucera Classic 166");
                return new Lucera166Protocol(_packets, _outgoing);
            case ChronicleProtocol.InterludeAcis:
            default:
                UnityEngine.Debug.Log("[Protocol] L2J ACIS Interlude");
                return new InterludeProtocol(_packets, _outgoing);
        }
    }
}
