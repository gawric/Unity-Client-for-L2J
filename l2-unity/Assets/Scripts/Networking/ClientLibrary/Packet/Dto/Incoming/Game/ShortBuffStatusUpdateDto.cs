using System.Collections.Generic;
using UnityEngine;

public class ShortBuffStatusUpdateDto : IWireDto
{
    private EffectHolder _effect;
    public EffectHolder Effect { get => _effect;}

    public static ShortBuffStatusUpdateDto From(int id, int level, int duration)
    {
        return new ShortBuffStatusUpdateDto { _effect = new EffectHolder(id, level, duration) };
    }

    public void ReadFrom(PacketReader reader)
    {

            int id = reader.ReadI();
            int level = reader.ReadI();
            int duration = reader.ReadI();
            _effect = new EffectHolder(id, level, duration);
    }
}
