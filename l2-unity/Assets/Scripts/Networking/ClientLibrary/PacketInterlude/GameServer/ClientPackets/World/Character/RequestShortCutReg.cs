using UnityEngine;

public class RequestShortCutReg : ClientPacket
{
    public RequestShortCutReg(int typeId, int world_slot, int id , int level) : base((byte)GameInterludeClientPacketType.RequestShortCutReg)
    {
        
        WriteI(typeId);
        //WriteI(slot + (page * 12));
       // if (world_slot > 12) world_slot = world_slot + 1;
        WriteI(world_slot);
        WriteI(id);
        WriteI(level);

        BuildPacket();
    }
}
