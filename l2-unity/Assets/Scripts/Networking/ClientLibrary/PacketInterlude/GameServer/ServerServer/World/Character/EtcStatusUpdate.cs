using UnityEngine;

public class EtcStatusUpdate : ServerPacket
{
    private int _defaultDeathPenalty = 5076;
    private int _defaultWeightPenalty = 4270;
    private int _defaultWeaponPenalty = 6209;
    private int _level = 0;
    public EtcStatusUpdate(byte[] d) : base(d)
    {
        DeathPenalty = new int[2] { _defaultDeathPenalty, _level };
        WeightPenalty = new int[2] { _defaultWeightPenalty, _level };
        WeaponPenalty = new int[2] { _defaultWeaponPenalty, _level };
        Parse();
    }

    public int[] DeathPenalty {get;set;}
    public int[] WeaponPenalty { get; set; }
    public int ChatBanned { get; set; }
    public int[] WeightPenalty { get; set; }
    public override void Parse()
    {
        int charges = ReadI();
        WeightPenalty[1] = ReadI();
        ChatBanned = ReadI();
        int dangerAREA = ReadI();
        WeaponPenalty[1] = ReadI();
        int cHARM_OF_COURAGE = ReadI();
        DeathPenalty[1] = ReadI();

    }
}
