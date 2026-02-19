using System;
using UnityEngine;

public class MagicSkillLaunched : ServerPacket
{
    private int _objectId;
    private int _skillId;
    private int _skillLvl;
    private int[] _targetArray;

    public MagicSkillLaunched(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
         _objectId = ReadI();
         _skillId = ReadI();
         _skillLvl = ReadI();
        int _targetSize = ReadI();
        _targetArray = new int[_targetSize];
        for (int i = 0; i < _targetSize; i++)
        {
            _targetArray[i] = ReadI();
        }
        Debug.Log("");
    }
}
