[IncomingGamePacket(GameServerPacketType.PetInfo)]
public sealed class PetInfoIncoming : IncomingPacket<NpcInfoDto>
{
    public override NpcInfoDto Read(PacketReader reader)
    {
        NpcInfoDto dto = new NpcInfoDto();
        dto.ReadPet(reader);
        return dto;
    }

    public override void Apply(NpcInfoDto packet)
    {
        if (packet.Identity != null)
            SpawnFrameBudget.ClearCancel(packet.Identity.Id);
        StorageNpc.getInstance().AddNpcInfo(packet);
        IncomingPacketActions.QueueWorld(apply => apply.UpdateNpc(packet));
    }
}
