using UnityEngine;

public class EtcStatusUpdateDto : IWireDto
{
    private int _defaultDeathPenalty = 5076;
    private int _defaultWeightPenalty = 4270;
    private int _defaultWeaponPenalty = 6209;
    private int _level = 0;
    public EtcStatusUpdateDto()
    {
        DeathPenalty = new int[2] { _defaultDeathPenalty, _level };
        WeightPenalty = new int[2] { _defaultWeightPenalty, _level };
        WeaponPenalty = new int[2] { _defaultWeaponPenalty, _level };
    }

    public int[] DeathPenalty {get;set;}
    public int[] WeaponPenalty { get; set; }
    public int ChatBanned { get; set; }
    public int[] WeightPenalty { get; set; }
    public void ReadFrom(PacketReader reader)
    {
        int charges = reader.ReadI();
        WeightPenalty[1] = reader.ReadI();
        ChatBanned = reader.ReadI();
        int dangerAREA = reader.ReadI();
        WeaponPenalty[1] = reader.ReadI();
        int cHARM_OF_COURAGE = reader.ReadI();
        DeathPenalty[1] = reader.ReadI();

    }
}
