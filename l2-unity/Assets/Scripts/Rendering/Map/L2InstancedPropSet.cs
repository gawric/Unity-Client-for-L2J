using System;
using UnityEngine;

[Serializable]
public class L2InstancedPropBatch
{
    public Mesh mesh;
    public Material material;
    public Matrix4x4[] matrices;
    public bool castShadows;
}

[CreateAssetMenu(menuName = "L2/Instanced Prop Set")]
public class L2InstancedPropSet : ScriptableObject
{
    public L2InstancedPropBatch[] batches = Array.Empty<L2InstancedPropBatch>();
    public int instanceCount;
}
