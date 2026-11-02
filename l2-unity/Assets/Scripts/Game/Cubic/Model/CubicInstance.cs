using UnityEngine;

/// <summary>
/// Runtime state of one cubic, mirroring the ANCubics layout fields that the
/// verified code paths actually touch.
/// </summary>
public sealed class CubicInstance
{
    /// <summary>ANCubics+0x4DC (byte): 1..9, chosen by AddNCubic.</summary>
    public int CubicType;

    /// <summary>ANCubics+0x4DD (byte): 0 spawn, 1 follow, 2 cast.</summary>
    public byte MovementState = CubicConstants.StateSpawn;

    /// <summary>ANCubics+0x4EC (int): slot index 0..2, assigned by slot order.</summary>
    public int CubicIndex;

    /// <summary>ANCubics+0x1BC (FVector).</summary>
    public Vector3 Location;

    /// <summary>ANCubics+0x4E0 (FVector): the "sticky" destination.</summary>
    public Vector3 DestLocation;

    /// <summary>ANCubics+0x1D4 (FVector).</summary>
    public Vector3 Velocity;

    /// <summary>ANCubics+0x4F0 (int): skill the cubic will cast.</summary>
    public int SkillId = -1;

    /// <summary>
    /// MagicSkillUse that armed this cast. The shot reuses the CharInfo
    /// none-anim effect path, which needs the packet's target and hit time.
    /// </summary>
    public MagicSkillUseDto CastPacket;

    /// <summary>True after this cast asked EffectManager for the skill's listed visual.</summary>
    public bool CharInfoShotChecked;

    /// <summary>True when that listed visual spawned, so the native emitters stay quiet.</summary>
    public bool CharInfoShotPlayed;

    /// <summary>ANCubics+0x4F4 (APawn*): cast target; null until told to cast.</summary>
    public Transform Target;

    /// <summary>ANCubics+0x4F8 (float): time spent in the current cast.</summary>
    public float SkillActiveTime;

    /// <summary>
    /// True once the per-type cast effect (<see cref="CubicConstants.CastEffectName"/>)
    /// has been spawned for the current cast. The native code does not need this
    /// flag (it guards on SkillActiveTime crossing the threshold exactly once),
    /// but a per-frame caller does.
    /// </summary>
    public bool CastEffectFired;

    /// <summary>Instantiated emitter root, when one could be resolved.</summary>
    public GameObject Visual;

    /// <summary>One-shot appear log. Not a native field.</summary>
    public bool AppearanceLogged;

    /// <summary>Emitter source name, from <see cref="CubicConstants.EmitterName"/>.</summary>
    public string EmitterName;
}
