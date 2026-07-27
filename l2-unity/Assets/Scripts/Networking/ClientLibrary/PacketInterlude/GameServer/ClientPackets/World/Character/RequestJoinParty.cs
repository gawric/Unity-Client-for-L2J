public class RequestJoinParty : ClientPacket
{
    private string _name;
    private int _partyDistributionTypeId;

    public RequestJoinParty(string name, int partyDistributionTypeId) : base((byte)GameInterludeClientPacketType.RequestJoinParty)
    {
        _name = name;
        _partyDistributionTypeId = partyDistributionTypeId;
        WriteOtherS(_name);
        WriteI(_partyDistributionTypeId);
        BuildPacket();
    }
}
