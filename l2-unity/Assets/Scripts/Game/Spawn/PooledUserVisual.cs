using UnityEngine;

/// <summary>
/// Marks a pooled UserEntity that already has face/hair built (not a bare container).
/// </summary>
public sealed class PooledUserVisual : MonoBehaviour
{
    public bool Built;
    public byte Face;
    public byte HairStyle;
    public byte HairColor;
}
