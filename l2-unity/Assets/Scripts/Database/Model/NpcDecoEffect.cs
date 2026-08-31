using UnityEngine;

[System.Serializable]
public class NpcDecoEffect
{
    [SerializeField] int _npcId;
    [SerializeField] string _className;
    [SerializeField] string _meshName;
    [SerializeField] string _decoEffect;
    [SerializeField] float _scale = 1f;

    public int NpcId { get { return _npcId; } set { _npcId = value; } }
    public string ClassName { get { return _className; } set { _className = value; } }
    public string MeshName { get { return _meshName; } set { _meshName = value; } }
    public string DecoEffect { get { return _decoEffect; } set { _decoEffect = value; } }
    public float Scale { get { return _scale; } set { _scale = value; } }

    public bool HasEffectName
    {
        get { return !string.IsNullOrWhiteSpace(_decoEffect); }
    }
}
