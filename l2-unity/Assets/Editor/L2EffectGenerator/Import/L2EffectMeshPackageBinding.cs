#if UNITY_EDITOR
using System.Collections.Generic;

public sealed class L2EffectMeshPackageBinding
{
    public List<string> TextureNames = new List<string>();
    public List<string> TextureReferences = new List<string>();
    public int SectionCount;
    public bool TwoSided;
    public bool UseVertexColor;
}
#endif
