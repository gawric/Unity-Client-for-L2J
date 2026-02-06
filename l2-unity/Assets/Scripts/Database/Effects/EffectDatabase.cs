using System.Collections.Generic;
using UnityEngine;

// Эта строка позволяет создавать файл через меню ПКМ
[CreateAssetMenu(fileName = "NewEffectDatabase", menuName = "VFX/Effect Database")]
public class EffectDatabase : ScriptableObject
{
    public List<EffectData> effects = new List<EffectData>();


    [System.Serializable]
    public class EffectData
    {
        public int id;
        public BaseEffect prefab;
        public EffectSettings settings; 
    }

    public BaseEffect GetPrefab(int id)
    {
        var data = effects.Find(e => e.id == id);
        return data != null ? data.prefab : null;
    }
}
