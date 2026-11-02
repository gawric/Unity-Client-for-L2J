using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One effect NCubicSkillProcess spawns on this frame.
/// </summary>
public struct CubicStrikeFx
{
    public string Name;

    /// <summary>Prologue s_u013_x: parent to the cubic. The shot uses the target.</summary>
    public bool OnCubic;

    /// <summary>AEmitter::SetSizeScale argument. 1 leaves the prefab scale.</summary>
    public float SizeScale;

    /// <summary>AttachToBone(target, effect, 0, 1).</summary>
    public bool AttachToTarget;

    public CubicStrikeFx(string name, bool onCubic, float sizeScale, bool attachToTarget)
    {
        Name = name;
        OnCubic = onCubic;
        SizeScale = sizeScale;
        AttachToTarget = attachToTarget;
    }
}

/// <summary>
/// Arms a cubic cast and steps ANCubics::NCubicSkillProcess (Live 0x1E06D0).
/// Movement while CAST stays in CubicMotion.UpdateCast.
/// </summary>
public static class CubicCast
{
    const string Prologue = "LineageEffect.s_u013_x";

    /// <summary>
    /// Native equivalent: ANCubics::NCubicSkillInit(owner, target, skillId).
    /// Body is only 0x35 bytes: three early-outs, then it arms the cast.
    /// </summary>
    public static void NCubicSkillInit(CubicInstance cubic, Transform owner,
                                       Transform target, int skillId)
    {
        if (target == null) return;
        if (owner == null) return;

        cubic.SkillId = skillId;
        cubic.Target = target;

        if (skillId == 0) return;

        cubic.MovementState = CubicConstants.StateCast;
        cubic.SkillActiveTime = 0f;
        cubic.CastEffectFired = false;
    }

    /// <summary>
    /// One frame of ANCubics::NCubicSkillProcess. Appends the effects this frame
    /// spawns. State returns to orbit only on the frame the shot fires, which is
    /// the <c>mov byte ptr [esi+4DDh], 0</c> after the threshold, not every frame.
    ///
    /// Open measurements, not filled in here:
    /// TrailerPrePivot.Z = sign * USkeletalMesh[+0x98] (type 2 plus, type 3 times -1).
    /// Hook AEmitter vtable+0x7C (GetEmitter) and read mesh+0x98 on the shot frame.
    /// </summary>
    public static void NCubicSkillProcess(CubicInstance cubic, float deltaTime, List<CubicStrikeFx> spawned)
    {
        if (cubic.MovementState != CubicConstants.StateCast)
            return;

        if (cubic.SkillActiveTime == 0f && cubic.CubicType != 0)
        {
            spawned.Add(new CubicStrikeFx(Prologue, true, 1f, false));
        }

        if (!cubic.CastEffectFired && ShotReady(cubic))
        {
            EmitShot(cubic, spawned);
            cubic.CastEffectFired = true;
            cubic.MovementState = CubicConstants.StateSpawn;
        }

        cubic.SkillActiveTime += deltaTime;
    }

    /// <summary>
    /// Mirrors the loop at the end of APawn::SpawnNTransientEffect: walk the
    /// owner's cubics and arm the ones whose CubicType matches the branch that
    /// <paramref name="skillId"/> selects (see CubicConstants.CubicTypeForSkill).
    /// Returns the number of cubics armed.
    /// </summary>
    public static int ArmCastingCubics(CubicCollection collection, Transform owner, Transform target, int skillId)
    {
        List<CubicInstance> cubics = collection.Items;
        int type = CubicConstants.CubicTypeForSkill(skillId);
        if (type == 0)
            return 0;

        int armed = 0;
        for (int i = 0; i < cubics.Count; i++)
        {
            if (cubics[i].CubicType != type)
                continue;

            NCubicSkillInit(cubics[i], owner, target, skillId);
            armed++;
        }
        return armed;
    }

    static bool ShotReady(CubicInstance cubic)
    {
        // Case 9 compares SkillID immediately. It does not wait on SkillActiveTime.
        if (cubic.CubicType == 9)
            return true;

        // Type 1 and 7: fcomp 2.0 / test ah,5 / jp skip. Others: fcomp 1.8 / test ah,41h / jnz skip.
        float gate = (cubic.CubicType == 1 || cubic.CubicType == 7) ? 2f : 1.8f;
        return cubic.SkillActiveTime > gate;
    }

    static void EmitShot(CubicInstance cubic, List<CubicStrikeFx> spawned)
    {
        switch (cubic.CubicType)
        {
            case 1:
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u000_c", false, 0.6f, false));
                break;
            case 2:
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u003_c", false, 1f, false));
                // The binary spawns LineageEffect.m_u003_b twice, each with its own FNMover.
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u003_b", false, 1f, false));
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u003_b", false, 1f, false));
                break;
            case 3:
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u001_b", false, 1f, false));
                break;
            case 4:
            case 6:
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u007_b", false, 1f, true));
                break;
            case 5:
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u018_b", false, 1f, false));
                break;
            case 7:
                spawned.Add(new CubicStrikeFx("LineageEffect.m_u037_d", false, 0.6f, false));
                break;
            case 8:
                spawned.Add(new CubicStrikeFx("LineageEffect.s_u011_a", false, 1f, false));
                break;
            case 9:
                if (cubic.SkillId == 5115)
                    spawned.Add(new CubicStrikeFx("LineageEffect.ph_hate_ta", false, 1f, true));
                else if (cubic.SkillId == 5116)
                    spawned.Add(new CubicStrikeFx("LineageEffect.m_u013_c", false, 1f, false));
                break;
        }
    }
}
