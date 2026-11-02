using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Client-side restoration of the Interlude "Cubic" chain (flying lights that
/// orbit the owner and periodically cast on their own).
///
/// Everything here is derived from the native client (engine.dll) and verified
/// live with Frida on 2026-10-24 (casting) and 2026-10-25 (removal).
/// Addresses are Live RVAs (IDA VA - 0x19A0000).
///
/// ── How the native client learns about cubics ────────────────────────────
/// There is NO standalone "summon cubic" packet. The server embeds
/// `short count` + `short ids[]` inside the ordinary CharInfo (0x03) and
/// UserInfo (0x04) packets. The native handler calls
///   APawn::CheckCubicStatus(short count, short* ids)      Live 0x1E1C20
/// which reconciles APawn::NCubicArray (+0x1674) with that list:
///   * count == 0                     -> APawn::DeleteAllNCubic()   Live 0x1E1A30
///   * id in packet, missing in array -> APawn::AddNCubic(id)       Live 0x1DEE10
///   * cubic in array, missing in pkt -> APawn::DeleteNCubic(id)    Live 0x1E1B30
///   * remaining slots get CubicIndex = 0..Num-1 at ANCubics+0x4EC
///
/// ── Removal semantics (Live verified 2026-10-25) ─────────────────────────
/// Sessions `del_all` and `del_partial`, probe `probe_cubic_delete.js`, with
/// NCubicArray dumped before/after every entry point:
///
///   del_all      count=1 ids=[1] -> AddNCubic -> Num 0->1
///                count=0 ids=NULL -> DeleteAllNCubic -> Num 1->0, array empty
///
///   del_partial  3 cubics live, array order ["3","2","1"] (newest first),
///                packet shrinks to count=2 ids=[1,3]:
///                DeleteNCubic(2) -> Num 3->2, middle element gone,
///                the very same 3 and 1 actor pointers survive;
///                Pass 3 then renumbers CubicIndex 0,1,2 -> 0,1.
///
/// Consequences this implementation must keep:
///   * `count == 0` means "delete everything", never "nothing to do".
///   * DeleteNCubic removes ONE type per call; slot indices are NOT fixed up
///     by it - only Pass 3 renumbers CubicsIndex.
///   * AddNCubic inserts at index 0, so array order is the reverse of arrival
///     order and never matches the packet order. Match by CubicType only.
///
/// APawn::AddNCubic (verified live: spawned "s_u013_a1", class s_u013_a):
///   if (NCubicArray.Num > 3) return 0;                  // hard cap of 3
///   if (CubicType > 10) return 0;                       // valid types 1..10
///   spawn = Owner.Location + (0, 0, 100);               // verified +100 in Z
///   switch (CubicType-1) selects the emitter class name;
///   SpawnActor(...) -> actor.CubicType(+0x4DC) = CubicType;
///                     actor.Owner(+0x3C)     = Owner;
///   NCubicArray.Insert(0, 1, 1);
///
/// ── How a cubic casts ────────────────────────────────────────────────────
/// ANCubics::Tick (Live 0x1E1F20) runs every frame per cubic:
///   AEmitter::Tick -> if (Owner == 0) return;
///   distance = |Owner.Location - Cubic.Location|;
///   SetDisabled(...) based on distance; then
///   ANCubics::GetVelocity(Owner, DeltaTime)          Live 0x1DF200
///   ANCubics::NCubicSkillProcess(Owner, DeltaTime)   Live 0x1E06D0
///
/// NCubicSkillProcess bails out immediately unless MovementState(+0x4DD) == 2.
/// That state is set ONLY by ANCubics::NCubicSkillInit(owner, target, skillId)
/// (Live 0x1DEB90), which is only reached from APawn::SpawnNTransientEffect
/// (Live 0x1EFB30).
///
/// SpawnNTransientEffect dispatches on skillId, and EACH branch filters its own
/// CubicType(+0x4DC) while walking owner->NCubicArray(+0x1674 / +0x1678):
///
///   skillId   CubicType   server skill              aCis cubic
///   -------   ---------   -----------------------   ------------------
///   20004     —           —                         hero ceremony (e_u091_a)
///   20005     —           —                         CursedWeapon, GPFs if armed
///   4049      1           4049 Cubic Drain          STORM_CUBIC     (1)
///   4050      2           4050 Cubic DD             VAMPIRIC_CUBIC  (2)
///   4051      3           4051 Cubic Heal           LIFE_CUBIC      (3)
///   4052      4           4052 Poison               VIPER_CUBIC     (4)
///   4053-55   5           4053/4054/4055            POLTERGEIST     (5)
///   4143      —           —                         "m_u006_e", no loop
///   4164      6           4164 Paralysis            BINDING_CUBIC   (6)
///   4165      7           4165 Icy Air              AQUA_CUBIC      (7)
///   4166      8           4166 Shock                SPARK_CUBIC     (8)
///   5115/5116 9           5115 Cubic Hate / 5116     ATTRACT_CUBIC   (9)
///   3156      —           Item Skill: Firework      visual only
///
/// The mapping is strictly 1:1 and self-consistent: aCis gives each cubic type
/// the skill that same type's client branch looks for (Cubic.java ctor switch).
/// So a type-8 Spark Cubic casts 4166, and 4166 arms only type 8.
///
/// Two server-side details that make replaying a cast tricky:
///   * every non-Life cubic only fires from Cubic.fireAction() while the owner
///     is in ATTACK stance (AttackStanceTaskManager);
///   * LIFE_CUBIC (3) bypasses that gate entirely and instead needs
///     pickFriendlyTarget() to succeed: owner hpRatio &lt; 1 first, then a
///     Rnd.get(100) roll (14% above 60% HP, 34% in 30..60%, 54% below 30%).
///     Lower the owner's HP before asking a Life Cubic to cast.
///
/// Note 4053..4055 all jump to ONE block (loc_1B90EE2) that contains a single
/// loop checking `CubicType == 5`. Types 6/7/8 have their own skillIds.
///
/// VERIFIED LIVE 2026-10-24 (frida + Replay cast sessions, one cubic per type):
/// every skillId armed ONLY its own CubicType, and NCubicSkillProcess ran in
/// CAST (state 2) for 299..401 frames per session. No cross-firing:
///   4049->1  4050->2  4051->3  4052->4  4053/54/55->5
///   4164->6  4165->7  4166->8  5115/5116->9
///
/// SocialAction 16/17 are NOT cubic casts: 16 is the hero ceremony and 17 is the
/// cursed-weapon ceremony (which GPFs the native client unless the pawn really
/// holds Zariche 0x1FFE / Akamanah 0x21F1). A cubic cast arrives as MagicSkillUse
/// and reaches SpawnNTransientEffect through UGameEngine::OnReceiveMagicSkillUse.
///
/// ── Where a cubic sits (Live verified 2026-10-25) ────────────────────────
/// There is no "assign slot" message either. ANCubics::GetVelocity rebuilds the
/// orbit target every frame from exactly two integers:
///   * Pawn.NCubicArray.Num  (+0x1678) — how many cubics the owner has
///   * this NCubic.CubicIndex (+0x4EC) — its own slot, renumbered by Pass 3
/// and the target is
///   DestLocation = Anchor + RotateAngleAxis(ownerForward, angle(Num, slot), axis)
///                           * 1.5 * owner.CollisionRadius
///
///   Num  slot  angle    degrees  result
///   ---  ----  -------  -------  ------------------------------------------
///    1    0    0x6000    135      lone cubic
///    2    0    0x6000    135
///    2    1    0xA000    225      90 deg from slot 0
///    3    0    0x6000    135
///    3    1    0x8000    180      45 deg steps, middle lands directly behind
///    3    2    0xA000    225
///   >3    any  0x6000    135      `mov eax,6000h` default
///
/// Because the angle depends only on (Num, slot), a new cubic joining does not
/// "push" the others: the renumbering in Pass 3 changes their slots, and the
/// next frame recomputes everyone's target. That is the whole "they shift"
/// effect. See <see cref="CubicConstants.OrbitAngleFor"/> and
/// <see cref="CubicCollection.UpdateOrbit"/>.
///
/// ── Motion (Live verified 2026-10-26, session `cubic_move`) ──────────────
/// Probe `probe_cubic_follow.js`, 3 cubics, then MoveToLocation 659 UU on X,
/// a 90-degree turn and 437 UU on Y, then StopMove. ANCubics::GetMovementState
/// picks the branch purely from the owner:
///   Acceleration(+0x1E0) != 0 -> ORBIT (0)   "he is running"
///   Acceleration(+0x1E0) == 0 -> FOLLOW (1)  "he stands still"
///
///   ORBIT (running)   1.5 * CollisionRadius = 11.25 ring, live 10.45..10.67.
///                     The ring is rebuilt around the CURRENT anchor every
///                     frame, so it stays glued to the owner and never lags
///                     behind as a whole; only the phase angle rotates
///                     (RotPerSecond 180 deg/s).
///                     Speed = |Pawn.Velocity| = 116.2 UU/s, and the owner
///                     really moves 1.149 UU/frame while the cubic moves 1.128
///                     (ratio 0.981..0.982), so the cubic merely MATCHES his
///                     speed and cannot close the gap: the trailing distance
///                     slowly GROWS (a leash, not a chaser), and the size of
///                     the gap depends on where the cubic sits on the ring
///                     relative to the run direction (it rotates 180 deg/s).
///   FOLLOW (stopped)  the remaining gap closes in 126..147 frames
///                     (~1.32..1.54 s at 95.4 fps) with Z constant, i.e. the
///                     horizontal approach finishes BEFORE the vertical jitter
///                     starts; the 1.0 UU delta-box then hands over to the
///                     idle bob.
///
/// The idle bob is real and lives in FOLLOW only: jz in [0, 0.25H] with a random
/// sign gives a 0.5H ceiling, and the measured owner-relative peak-to-peak is
/// 3.26..3.53 UU of 11.4 UU at CollisionHeight 22.8. Measure Z RELATIVE to the
/// owner: the pawn settles after a run, which otherwise reads as 46..80 UU.
/// </summary>
public static class CubicConstants
{
    /// <summary>Native hard cap: APawn::AddNCubic returns 0 when Num > 3.</summary>
    public const int MaxCubics = 3;

    /// <summary>Native max CubicType (AddNCubic returns 0 when type > 10).</summary>
    public const int MaxCubicType = 10;

    /// <summary>
    /// Spawn offset above the owner's Location:
    /// AddNCubic does `Location.z += 100.0` (dbl_1F23C40).
    /// Verified live: pawn z=-3424.53 -> first cubic z=-3324.53.
    /// </summary>
    public const float SpawnZOffset = 100f;

    /// <summary>
    /// ANCubics+0x4DD values (ANCubics::GetMovementState, Live 0x1DEA30).
    /// </summary>
    public const byte StateSpawn = 0;
    public const byte StateFollow = 1;
    public const byte StateCast = 2;

    /// <summary>
    /// CubicType -> emitter object name, from the jump table in AddNCubic
    /// (jpt_1B7EEB7). Types 10 returns NAME_None in native code, i.e. rejected.
    /// Note the duplicates: 2/8 share s_u013_d, 5/7 share s_u013_b,
    /// 4/6 share s_u013_e — they differ by behaviour, not by visual.
    /// </summary>
    public static string EmitterName(int cubicType)
    {
        switch (cubicType)
        {
            case 1: return "LineageEffect.s_u013_a";
            case 2: return "LineageEffect.s_u013_d";
            case 3: return "LineageEffect.s_u013_c";
            case 4: return "LineageEffect.s_u013_e";
            case 5: return "LineageEffect.s_u013_b";
            case 6: return "LineageEffect.s_u013_e";
            case 7: return "LineageEffect.s_u013_b";
            case 8: return "LineageEffect.s_u013_d";
            case 9: return "LineageEffect.e_u096_cubic";
            default: return null;
        }
    }

    /// <summary>
    /// CubicType that APawn::SpawnNTransientEffect arms for a given skillId, taken
    /// from the three-level dispatch at loc_1B90C78 / loc_1B910D0 / jpt_1B90DE5.
    /// Returns 0 when the skill never touches a cubic (20004/20005/3156/4143).
    /// </summary>
    public static int CubicTypeForSkill(int skillId)
    {
        switch (skillId)
        {
            case 4049: return 1;  // STORM_CUBIC
            case 4050: return 2;  // VAMPIRIC_CUBIC
            case 4051: return 3;  // LIFE_CUBIC
            case 4052: return 4;  // VIPER_CUBIC
            case 4053:
            case 4054:
            case 4055: return 5;  // POLTERGEIST_CUBIC (all three -> loc_1B90EE2)
            case 4164: return 6;  // BINDING_CUBIC
            case 4165: return 7;  // AQUA_CUBIC
            case 4166: return 8;  // SPARK_CUBIC
            case 5115:
            case 5116: return 9;  // ATTRACT_CUBIC
            default: return 0;    // 20004/20005/3156/4143 and everything else
        }
    }

    /// <summary>
    /// True when this cubic type can be told to cast at all (types 1..9 all have
    /// a server skill and a client branch).
    /// </summary>
    public static bool CanCast(int cubicType) => cubicType >= 1 && cubicType <= 9;

    // ── Cast visuals (Live verified 2026-10-25) ──────────────────────────────
    // NCubicSkillProcess (Live 0x1E06D0) writes ONLY three fields on the cubic
    // itself: CastSkillID (+0x510), MovementState (+0x4DD) and
    // SkillActiveTime (+0x4F8). It never scales, tints or re-materials the cubic
    // — so "the cubic got brighter / bigger" is the spawned effect, not the
    // cubic. The interesting writes are on the spawned AEmitter:
    //   * eff[+0x484] = Emitter.TrailerPrePivot.Z (Vector at +0x47C)
    //   * AEmitter::SetSizeScale(eff, 0.6f)  (torch 0x1F4574C)
    //   * eff[+0x5F0] / eff[+0x540] = SkillID, eff[+0x4E4] = TargetPawn||Owner
    // SetSizeScale multiplies ~30 particle size fields but early-returns unless
    // bSetSizeScale (+0x4D8) & 1 is set — only Storm (1) and Aqua (7) reach it.

    /// <summary>
    /// Time (seconds) of SkillActiveTime after which the per-type effect is
    /// spawned. From the switch in NCubicSkillProcess: type 1 (Storm) and type 7
    /// (Aqua) wait for > 2.0, everything else for > 1.8.
    /// </summary>
    public static float CastSwitchTime(int cubicType)
        => (cubicType == 1 || cubicType == 7) ? 2.0f : 1.8f;

    /// <summary>
    /// Emitter class spawned by the cast tail for this type (verified live by
    /// the StaticLoadClass order and the PlaySkillSound(soundIndex==2) class).
    /// Null means the branch spawns nothing at index 2.
    /// </summary>
    /// <summary>
    /// Skill ExplosionAction played when the cubic's NSkillProjectile reaches the target.
    /// The shot class itself is <see cref="CastEffectName"/>.
    /// </summary>
    public static string ImpactEffectName(int skillId)
    {
        switch (skillId)
        {
            case 4049: return "m_u000_d";
            default: return null;
        }
    }

    public static string CastEffectName(int cubicType)
    {
        switch (cubicType)
        {
            case 1: return "LineageEffect.m_u000_c";
            case 2: return "LineageEffect.m_u003_c";
            case 3: return "LineageEffect.m_u001_b";
            case 4: return "LineageEffect.m_u007_b";
            case 5: return "LineageEffect.m_u018_b";
            case 6: return "LineageEffect.m_u007_b";
            case 7: return "LineageEffect.m_u037_d";
            case 8: return "LineageEffect.s_u011_a";
            case 9: return "LineageEffect.m_u013_c";
            default: return null;
        }
    }

    /// <summary>
    /// Sign of the spawned effect's TrailerPrePivot.Z (eff+0x484). Vampiric (2,
    /// offensive) writes +TargetMesh[+0x98] and Life (3, heal) writes the same
    /// value multiplied by -1.0 (fmul dbl_1F40660). This single sign is the whole
    /// visible "heal vs attack" difference. Live values: +24.0 / -25.3.
    /// </summary>
    public static float CastTrailerSign(int cubicType)
        => cubicType == 3 ? -1f : (cubicType == 2 ? 1f : 0f);

    /// <summary>
    /// Value passed to AEmitter::SetSizeScale for this type, or 0 when the branch
    /// never calls it. Live: only type 1 (Storm) and type 7 (Aqua) call it, 0.6f.
    /// </summary>
    public static float CastSizeScale(int cubicType)
        => (cubicType == 1 || cubicType == 7) ? 0.6f : 0f;

    /// <summary>
    /// Particle lifetime adjustment: prologue calls AdjustParticleLife with 2.0
    /// for type 1 (flt_1F24CF0) and 1.8 for everyone else (flt_1F47834).
    /// </summary>
    public static float CastParticleLife(int cubicType)
        => cubicType == 1 ? 2.0f : 1.8f;

    /// <summary>
    /// True when the cast branch targets the monster rather than the owner.
    /// Only LIFE_CUBIC (3) resolves target == owner — NCubicSkillInit live log:
    /// type 3 "targetIsOwner": true, every other type false.
    /// </summary>
    public static bool CastTargetsOwner(int cubicType) => cubicType == 3;

    // ── Orbit placement ──────────────────────────────────────────────────
    // ANCubics::GetVelocity (Live 0x1DF200) rebuilds the orbit target from
    // scratch on EVERY frame out of just two numbers: the owner's cubic count
    // (NCubicArray.Num, +0x1678) and this cubic's own slot (CubicIndex, +0x4EC).
    // Nothing else feeds the angle — not CubicType, not the order of arrival.
    //
    // Native angle unit: 65536 == full turn, so 0x6000 = 135 degrees.

    // The three values form a fan of 45-degree steps around the owner. In UE
    // space (X forward, Y right, Z up) rotating the owner's forward vector by
    // these angles about the vertical axis yields, on a unit circle:
    //   0x6000 (135 deg) -> (-0.707, +0.707)  behind-right
    //   0x8000 (180 deg) -> (-1.000,  0.000)  directly behind
    //   0xA000 (225 deg) -> (-0.707, -0.707)  behind-left
    // Live dX/dY for slot 0 was -7.84 / +8.02 with an 11.21 horizontal radius,
    // i.e. 0.707 * 11.21 = 7.93 on each axis — matching 135 deg exactly.

    /// <summary>0x6000 = 135 deg. Slot 0 always, and the lone cubic whenever
    /// Num is 1 or out of range.</summary>
    public const int AngleSlot0 = 0x6000;

    /// <summary>0x8000 = 180 deg. Middle slot when Num == 3 (directly behind).</summary>
    public const int AngleSlot1 = 0x8000;

    /// <summary>0xA000 = 225 deg. Last slot for Num 2 and 3.</summary>
    public const int AngleSlot2 = 0xA000;

    /// <summary>
    /// Native equivalent: the angle fragment of ANCubics::GetVelocity
    /// (jump table 0x1B7F58E, branches loc_1B7F595 orbit / loc_1B7F949 follow).
    ///
    /// The ASM picks the angle with the branchless `neg / sbb / and / add`
    /// idiom, which is just `slot != 0 ? base + mask : base`:
    /// <code>
    ///   mov  eax, 6000h            ; default
    ///   cmp  ecx, 1 / jnz L2
    ///   cmp  [esi+4ECh], 0 / jz Done            ; 1 cubic  -> 135
    /// L2: cmp ecx, 2 / jnz L3
    ///   neg eax / sbb eax,eax / and eax,4000h / add eax,6000h   ; 2 -> 135 / 225
    /// L3: cmp ecx, 3 / jnz Done
    ///   test eax,eax / jz Done                  ; slot 0   -> 135
    ///   sub eax,1 / neg / sbb / and 2000h / add 8000h  ; slot 1 -> 180, slot 2 -> 225
    /// </code>
    ///
    /// Live verified 2026-10-25 with `probe_cubic_orbit.js`, edge-triggered on
    /// the (Num, slot) tuple:
    ///   1 cubic   slot 0 -> 0x6000
    ///   2 cubics  slot 0 -> 0x6000, slot 1 -> 0xA000  (90.5 deg apart)
    ///   3 cubics  slot 0 -> 0x6000, slot 1 -> 0x8000, slot 2 -> 0xA000
    ///             (44.9 / 45.5 deg apart)
    ///   after deleting the middle cubic the survivors fell back to 135 / 225,
    ///   which is the clearest proof that the angle follows Num, not history.
    /// </summary>
    public static int OrbitAngleFor(int cubicCount, int slotIndex)
    {
        if (cubicCount == 1) return AngleSlot0;

        if (cubicCount == 2)
            return slotIndex != 0 ? AngleSlot2 : AngleSlot0; // 135 / 225

        if (cubicCount == 3)
        {
            if (slotIndex == 0) return AngleSlot0; // 135
            if (slotIndex == 1) return AngleSlot1; // 180
            return AngleSlot2;                     // 225
        }

        // Num == 0 or Num > 3: the ASM falls through to the 0x6000 default.
        return AngleSlot0;
    }

    /// <summary>
    /// Orbit radius: `1.5 * owner.CollisionRadius` (dbl_1F2A4A8).
    /// The multiplier applies to the OWNER's radius (+0x2F0), never to the
    /// cubic's own capsule — an earlier version of this note had that wrong.
    /// Live: radius 7.5 -> 11.25, matching the observed 11.21..11.23 horizontal
    /// delta between consecutive slots.
    /// </summary>
    public const float OrbitRadiusScale = 1.5f;

    /// <summary>
    /// Native equivalent: the BONE / TOP / MIDAIR anchor split at the top of
    /// ANCubics::GetVelocity (loc_1B7F32B / loc_1B7F3A1 / loc_1B7F41B).
    ///
    /// The branch order matters — the ASM tests death and WaitType==2 FIRST:
    /// <code>
    ///   mov  ecx,[ebx+14D8h]                 ; Controller
    ///   test byte [ecx+41Ch],1 / jnz MIDAIR  ; dead
    ///   cmp  byte [ecx+435h],2 / jz  MIDAIR  ; lying down
    ///   ; WaitType 0 or 4, or riding -> BONE, otherwise TOP
    /// </code>
    /// <list type="bullet">
    /// <item>BONE: e_bone origin + CollisionHeight/3, axis = bone XAxis.
    /// Live: standing pawn (height 22.8) sits here, orbit plane is horizontal.</item>
    /// <item>TOP: owner Location + CollisionHeight, axis = root ZAxis.</item>
    /// <item>MIDAIR: owner Location - CollisionHeight/2, axis = root YAxis.</item>
    /// </list>
    ///
    /// AXIS MAPPING: UE x = forward, y = right, z = up, and this project's
    /// VectorUtils converts with a cyclic permutation (x,y,z) -&gt; (y,z,x)
    /// (ConvertToUnityUnscaled), which preserves handedness. So:
    ///   native XAxis (forward) -&gt; Unity Vector3.forward
    ///   native YAxis (right)   -&gt; Unity Vector3.right
    ///   native ZAxis (up)      -&gt; Unity Vector3.up
    /// All vectors passed in here must already be Unity-space.
    /// </summary>
    public static void AnchorFor(byte waitType, bool isDead, bool isRiding,
                                 Vector3 ownerLocation, Vector3 boneOrigin,
                                 float collisionHeight,
                                 out Vector3 anchor, out Vector3 axis)
    {
        // Death and "lying" are checked before anything else, exactly as in ASM.
        if (isDead || waitType == 2)
        {
            anchor = ownerLocation;
            anchor.z -= collisionHeight * 0.5f;
            axis = Vector3.right;    // native: root YAxis
            return;
        }

        if (waitType == 0 || waitType == 4 || isRiding)
        {
            anchor = boneOrigin;
            anchor.z += collisionHeight / 3f;
            axis = Vector3.forward;  // native: bone XAxis
            return;
        }

        anchor = ownerLocation;
        anchor.z += collisionHeight;
        axis = Vector3.up;           // native: root ZAxis
    }

    /// <summary>
    /// Native equivalent: `FV_RotateAngleAxis(ownerForward, angle, axis) * radius`
    /// added on top of the anchor (case 0 ORBIT and case 5 VEHICLE).
    /// <paramref name="angle"/> is in native units (65536 per turn), which is
    /// why it is converted before rotating.
    /// </summary>
    public static Vector3 OrbitOffset(Vector3 ownerForward, int angle,
                                      Vector3 axis, float ownerCollisionRadius)
    {
        float degrees = angle * (360f / 65536f);
        // Unreal is left-handed and so is Unity, so no axis flip is needed.
        Vector3 rotated = Quaternion.AngleAxis(degrees, axis) * ownerForward;
        return rotated * (OrbitRadiusScale * ownerCollisionRadius);
    }
}
