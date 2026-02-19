using System.Collections.Generic;
using UnityEngine;

public class ShortBuffStatusUpdate : ServerPacket
{
    private EffectHolder _effect;
    public EffectHolder Effect { get => _effect;}
    public ShortBuffStatusUpdate(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {

            int id = ReadI();
            int level = ReadI();
            int duration = ReadI();
            _effect = new EffectHolder(id, level, duration);
    }
}
