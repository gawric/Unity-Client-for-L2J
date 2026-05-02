
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewEffectDatabase", menuName = "VFX/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    public List<EffectData> effects = new List<EffectData>();



    [System.Serializable]
    public class EffectData
    {
        public int id;
        public BaseEffect prefab;
        public string comment = "";
        public EffectSettings settings;

        [Tooltip("If enabled, cast timing uses HitTime only (flight time forced to 0). Use for melee / instant hit magic — no projectile travel in sync.")]
        public bool ignoreFlightTimeForCast;
    }

    public BaseEffect GetPrefab(int id)
    {
        var data = effects.Find(e => e.id == id);
        return data != null ? data.prefab : null;
    }

    public bool TryGetEffectData(int id, out EffectData data)
    {
        data = effects.Find(e => e.id == id);
        return data != null && data.prefab != null;
    }

    public bool ShouldIgnoreFlightTimeForCast(int effectId)
    {
        EffectData data = effects.Find(e => e.id == effectId);
        return data != null && data.ignoreFlightTimeForCast;
    }
}
