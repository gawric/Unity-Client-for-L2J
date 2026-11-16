[IncomingGamePacket(GameServerPacketType.PartySmallWindowAll)]
public sealed class PartySmallWindowAllIncoming : IncomingWirePacket<PartySmallWindowAllDto>
{
    public override void Apply(PartySmallWindowAllDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Party.Replace(packet.LeaderId, packet.DistributionType, packet.Members));
    }
}

[IncomingGamePacket(GameServerPacketType.PartySmallWindowAdd)]
public sealed class PartySmallWindowAddIncoming : IncomingWirePacket<PartySmallWindowAddDto>
{
    public override void Apply(PartySmallWindowAddDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Party.Add(packet.LeaderId, packet.DistributionType, packet.Member));
    }
}

[IncomingGamePacket(GameServerPacketType.PartySmallWindowUpdate)]
public sealed class PartySmallWindowUpdateIncoming : IncomingWirePacket<PartySmallWindowUpdateDto>
{
    public override void Apply(PartySmallWindowUpdateDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Party.Update(packet.Member));
    }
}

[IncomingGamePacket(GameServerPacketType.PartySmallWindowDelete)]
public sealed class PartySmallWindowDeleteIncoming : IncomingWirePacket<PartySmallWindowDeleteDto>
{
    public override void Apply(PartySmallWindowDeleteDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Party.Remove(packet.ObjectId));
    }
}

[IncomingGamePacket(GameServerPacketType.PartySmallWindowDeleteAll)]
public sealed class PartySmallWindowDeleteAllIncoming : IncomingWirePacket<PartySmallWindowDeleteAllDto>
{
    public override void Apply(PartySmallWindowDeleteAllDto packet)
    {
        IncomingPacketActions.Queue(() => IncomingPacketActions.Game?.Party.Clear());
    }
}
