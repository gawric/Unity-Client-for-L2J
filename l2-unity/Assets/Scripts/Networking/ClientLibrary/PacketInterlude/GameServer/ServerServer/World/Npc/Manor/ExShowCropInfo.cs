using System.Collections.Generic;
using UnityEngine;

public class ExShowCropInfo : ServerPacket
{
    private int _manorId;
    private List<CropProcure> _list = new List<CropProcure>();
    public List<CropProcure> List { get => _list; }
    private bool _hideButtons;
    public ExShowCropInfo(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _hideButtons = ReadB() == 1;
        _manorId = ReadI();
        var unk1 = ReadI();
        var size = ReadI();

        for (int i = 0; i < size; i++)
        {
            int cropId = ReadI();
            int cropAmount = ReadI();
            int startCropAmount = ReadI();
            int priceCrop = ReadI();
            int isReward1 = ReadB();

            int seedLevel = ReadI();
            int reward1 = ReadB();
            int reward1ItemId = ReadI();
            int reward2 = ReadB();
            int reward2ItemId = ReadI();

            CropProcure crop = new CropProcure(cropId, cropAmount, priceCrop, startCropAmount , isReward1);
            crop.AddSeed(new Seed(seedLevel, reward1, reward1ItemId, reward2, reward2ItemId));
            _list.Add(crop);
        }
    }
}
