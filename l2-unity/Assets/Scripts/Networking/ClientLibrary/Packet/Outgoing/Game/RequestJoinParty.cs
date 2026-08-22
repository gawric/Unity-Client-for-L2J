/// <summary>
/// Invites a player to the local player's party by name - the server looks the target up by name
/// (not object id). PartyDistributionTypeId only matters when this creates a brand new party (the
/// server ignores it once a party already exists).
/// </summary>
[OutgoingCommandPacket(typeof(RequestJoinPartyCommand))]
public sealed class RequestJoinParty : OutgoingWirePacket<RequestJoinPartyDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.RequestJoinParty;

    public RequestJoinParty(RequestJoinPartyCommand command) : this(command.TargetName, command.PartyDistributionTypeId) { }

    public RequestJoinParty(string targetName, int partyDistributionTypeId)
    {
        Dto.TargetName = targetName;
        Dto.PartyDistributionTypeId = partyDistributionTypeId;
    }
}
