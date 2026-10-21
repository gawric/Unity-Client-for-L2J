using System;
using UnityEngine;

public class MagicSkillLaunchedDto : IWireDto
{
    private int _objectId;
    private int _skillId;
    private int _skillLvl;
    private int[] _targetArray;

    

    public int ObjectId { get => _objectId; }
    public int SkillId { get => _skillId; }
    public int SkillLvl { get => _skillLvl; }
    public int[] TargetArray { get => _targetArray; }

    public void ReadFrom(PacketReader reader)
    {
         _objectId = reader.ReadI();
         _skillId = reader.ReadI();
         _skillLvl = reader.ReadI();
        int _targetSize = reader.ReadI();
        _targetArray = new int[_targetSize];
        for (int i = 0; i < _targetSize; i++)
        {
            _targetArray[i] = reader.ReadI();
        }
        // Debug.Log("");
    }
}
