using System.Collections.Generic;
using UnityEngine;

public class ExShowCropInfoDto : IWireDto
{
    private int _manorId;
    private List<CropProcure> _list = new List<CropProcure>();
    public List<CropProcure> List { get => _list; }
    private bool _hideButtons;
    

    public void ReadFrom(PacketReader reader)
    {
        _hideButtons = reader.ReadB() == 1;
        _manorId = reader.ReadI();
        var unk1 = reader.ReadI();
        var size = reader.ReadI();

        for (int i = 0; i < size; i++)
        {
            int cropId = reader.ReadI();
            int cropAmount = reader.ReadI();
            int startCropAmount = reader.ReadI();
            int priceCrop = reader.ReadI();
            int isReward1 = reader.ReadB();

            int seedLevel = reader.ReadI();
            int reward1 = reader.ReadB();
            int reward1ItemId = reader.ReadI();
            int reward2 = reader.ReadB();
            int reward2ItemId = reader.ReadI();

            CropProcure crop = new CropProcure(cropId, cropAmount, priceCrop, startCropAmount , isReward1);
            crop.AddSeed(new Seed(seedLevel, reward1, reward1ItemId, reward2, reward2ItemId));
            _list.Add(crop);
        }
    }
}
