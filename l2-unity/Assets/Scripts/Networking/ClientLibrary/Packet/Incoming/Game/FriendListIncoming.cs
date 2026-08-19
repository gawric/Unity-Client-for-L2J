using UnityEngine;

[IncomingGamePacket(GameServerPacketType.FriendList)]
public sealed class FriendListIncoming : IncomingPacket<FriendListIncomingDto>
{
    public override FriendListIncomingDto Read(PacketReader reader)
    {
        return new FriendListIncomingDto();
    }

    public override void Apply(FriendListIncomingDto dto)
    {
        Debug.Log("Friend List SUccess");
    }
}
