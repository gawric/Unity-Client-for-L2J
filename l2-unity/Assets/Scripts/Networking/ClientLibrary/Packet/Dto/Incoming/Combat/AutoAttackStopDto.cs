using System;
using UnityEngine;

public class AutoAttackStopDto : IWireDto {
    public int EntityId { get; private set; }

    

    public void ReadFrom(PacketReader reader) {
        try {
            EntityId = reader.ReadI();
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }
}
