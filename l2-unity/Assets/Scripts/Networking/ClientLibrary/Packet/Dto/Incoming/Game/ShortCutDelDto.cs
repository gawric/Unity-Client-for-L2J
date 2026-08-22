using UnityEngine;

public class ShortCutDelDto : IWireDto
{
    private int _slot;

    public int Slot { get => _slot; }

    
    public void ReadFrom(PacketReader reader)
    {
        int world_slot = reader.ReadI();
        int unk1 = reader.ReadI();
        _slot = world_slot;
        int slot = world_slot % 12;
        int page = world_slot / 12;

        Debug.Log("world_slot : " + world_slot + " slot : " + slot + " page : " + page);
        
    }
}
