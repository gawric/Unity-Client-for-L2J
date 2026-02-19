using UnityEngine;

public class ShortCutDel : ServerPacket
{
    private int _slot;

    public int Slot { get => _slot; }

    public ShortCutDel(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
        int world_slot = ReadI();
        int unk1 = ReadI();
        _slot = world_slot;
        int slot = world_slot % 12;
        int page = world_slot / 12;

        Debug.Log("world_slot : " + world_slot + " slot : " + slot + " page : " + page);
        
    }
}
