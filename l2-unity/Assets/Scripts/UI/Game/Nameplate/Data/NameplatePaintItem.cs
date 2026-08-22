using UnityEngine;

/// <summary>
/// One nameplate ready to project and draw this frame.
/// </summary>
public struct NameplatePaintItem
{
    public int Id;
    public Vector3 World;
    public string Name;
    public string Title;
    public Color NameColor;
    public Color TitleColor;
    public bool IsLocalPlayer;
    public L2TargetRenderType BubbleType;
    public bool ScreenValid;
    public float Depth;
    public float NameW;
    public float XName;
    public float YNameTop;
}
