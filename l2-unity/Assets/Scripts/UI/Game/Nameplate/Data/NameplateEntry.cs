using UnityEngine;

/// <summary>
/// Cached world nameplate entity state.
/// </summary>
public struct NameplateEntry
{
    public int Id;
    public Transform Target;
    public CharacterController CC;
    public CapsuleCollider Capsule;
    public Entity Entity;
    public string Name;
    public string Title;
    public Color NameColor;
    public Color TitleColor;
    public bool Visible;
}
