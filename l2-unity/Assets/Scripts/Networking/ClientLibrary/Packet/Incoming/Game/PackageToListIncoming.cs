using UnityEngine;

[IncomingGamePacket(GameServerPacketType.PackageToList)]
public sealed class PackageToListIncoming : IncomingWirePacket<PackageToListDto>
{
    public override void Apply(PackageToListDto packet)
    {
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.ShowList.AddList(packet.Players);
            IncomingPacketActions.ShowList.ShowWindow();
        });
    }
}
