using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-frame orbit, follow, and cast translation. Native ANCubics::GetVelocity.
/// Each pass moves only the cubics whose MovementState matches that branch.
/// </summary>
public static class CubicMotion
{
    // ── Per-frame orbit update ───────────────────────────────────────────

    /// <summary>
    /// Native equivalent: <c>ANCubics::GetVelocity</c> (Live 0x1DF200), ORBIT
    /// branch (case 0). Call once per frame per collection.
    ///
    /// Native order of operations, reproduced here in the same order:
    /// <list type="number">
    /// <item>read the owner's pose -> anchor + rotation axis (AnchorFor)</item>
    /// <item>if the cubic is more than 512 units away, FarMoveActor it to
    /// owner.Location + 100 Z (the "caught up the teleport" snap)</item>
    /// <item>if DestLocation is still all-zero, seed it with the anchor</item>
    /// <item>offset = RotateAngleAxis(ownerForward, slotAngle, axis) * 1.5r</item>
    /// <item>DestLocation = anchor + offset; Velocity = normalize(DestLocation -
    /// Location) * speed, speed 100 unless riding (carrier velocity)</item>
    /// <item>Step = Velocity * DeltaTime, clamped to the remaining distance</item>
    /// </list>
    ///
    /// Note the orbit angle is recomputed from (Num, slot) every frame, so a
    /// cubic whose slot changed in CheckCubicStatus / Pass 3 slides to its new
    /// place without any extra bookkeeping.
    /// </summary>
    /// <remarks>
    /// UNITS: every vector and float argument is in Unity meters, so the native
    /// constants below (which are Unreal units) are converted with
    /// <see cref="VectorUtils.ConvertL2UuToMeters"/> instead of being used raw.
    /// </remarks>
    /// <param name="ownerLocation">Owner Location (+0x1BC), Unity space.</param>
    /// <param name="boneOrigin">"e_bone" origin, for the BONE anchor.</param>
    /// <param name="ownerForward">Owner rotation vector (FRotatorToVector).</param>
    /// <param name="ownerCollisionRadius">Owner CollisionRadius (+0x2F0), meters.</param>
    /// <param name="ownerCollisionHeight">Owner CollisionHeight (+0x2F4), meters.</param>
    /// <param name="waitType">Controller WaitType (+0x435).</param>
    /// <param name="isDead">Controller controllerFlags &amp; 1.</param>
    /// <param name="isRiding">Pawn RideFlags &amp; 1, i.e. the owner sits on a
    /// vehicle; native then follows the CARRIER's velocity, not the owner's.</param>
    /// <param name="ownerSpeed">Pawn->Velocity.Size(), used when not riding.
    /// Native never normalizes it, so only the approach speed changes, never the
    /// final resting position.</param>
    /// <param name="carrierSpeed">Controller+0x3BC carrier Velocity.Size(), used
    /// when riding. Pass 0 (or omit) to keep the 100 UU/s default.</param>
    /// <param name="deltaTime">Frame delta.</param>
    public static void UpdateOrbit(CubicCollection collection, Vector3 ownerLocation, Vector3 boneOrigin,
                            Vector3 ownerForward, float ownerCollisionRadius,
                            float ownerCollisionHeight, byte waitType,
                            bool isDead, bool isRiding, float ownerSpeed,
                            float deltaTime, float carrierSpeed = 0f)
    {
        List<CubicInstance> _cubics = collection.Items;
        // Native: > 512 UU away (flt_1F47838) -> FarMoveActor snap.
        float farSnapDistance = VectorUtils.ConvertL2UuToMeters(512f);
        float farSnapZOffset = VectorUtils.ConvertL2UuToMeters(100f); // dbl_1F23C40

        // flt_1F3FA68 is 100 UU/s in native units; positions here are meters, so
        // the speed has to be converted too or the cubic would crawl 52.5x slow.
        float defaultOrbitSpeed = VectorUtils.ConvertL2UuToMeters(100f);

        CubicConstants.AnchorFor(waitType, isDead, isRiding, ownerLocation,
                                 boneOrigin, ownerCollisionHeight,
                                 out Vector3 anchor, out Vector3 axis);

        int num = _cubics.Count;

        for (int i = 0; i < num; i++)
        {
            CubicInstance cubic = _cubics[i];
            if (cubic.MovementState != CubicConstants.StateSpawn)
                continue;

            float distToOwner = Vector3.Distance(ownerLocation, cubic.Location);
            bool tooFar = distToOwner > farSnapDistance;

            if (tooFar)
            {
                Vector3 snap = ownerLocation;
                snap.z += farSnapZOffset;
                cubic.Location = snap; // native: XLevel->vtbl[0xA8] FarMoveActor
                cubic.Velocity = Vector3.zero;
            }

            // Native prologue (step E) seeds the sticky DestLocation with the
            // anchor when it is unset or the cubic was snapped. The ORBIT branch
            // then overwrites it with anchor + offset on the very next line, so
            // this only matters while the cubic is not yet orbiting.
            if (cubic.DestLocation == Vector3.zero || tooFar)
                cubic.DestLocation = anchor;

            int angle = CubicConstants.OrbitAngleFor(num, cubic.CubicIndex);
            Vector3 offset = CubicConstants.OrbitOffset(ownerForward, angle, axis,
                                                        ownerCollisionRadius);
            cubic.DestLocation = anchor + offset;

            // Speed: 100 UU/s by default; while riding the carrier's speed
            // (Controller+0x3BC -> Velocity), otherwise the owner's own speed.
            // A zero carrier speed falls back to the default, as in the ASM
            // where the carrier pointer is null-checked before the read.
            float speed = defaultOrbitSpeed;
            if (isRiding)
            {
                if (carrierSpeed > 0f)
                    speed = carrierSpeed;
            }
            else
            {
                speed = ownerSpeed;
            }

            Vector3 diff = cubic.DestLocation - cubic.Location;
            float diffSize = diff.magnitude;
            // Keep about 12 cm short of the ring while the owner is moving,
            // so the stop catch-up has a visible path. Closer than that, wait.
            const float TrailMeters = 0.12f;
            float closable = diffSize - TrailMeters;
            if (closable < 0f)
                closable = 0f;

            if (closable > 0.0001f && speed > 0f)
                cubic.Velocity = diff / diffSize * speed;
            else
                cubic.Velocity = Vector3.zero;

            Vector3 step = cubic.Velocity * deltaTime;
            if (step.magnitude > closable)
                step = diffSize > 0.0001f ? diff / diffSize * closable : Vector3.zero;
            cubic.Location += step;
        }
    }

    /// <summary>
    /// Native equivalent: <c>ANCubics::GetVelocity</c> (Live 0x1DF200), FOLLOW
    /// branch (case 1) — the owner stands still. This is where the idle bob
    /// lives, and it is NOT a cosmetic extra: the native code re-rolls the
    /// destination every frame from appFrand().
    ///
    /// <para>Order of operations, as in the ASM:</para>
    /// <list type="number">
    /// <item>anchor += RotateAngleAxis(ownerForward, angle, axis) * 1.5r</item>
    /// <item>if distance(anchor, Location) &gt; CollisionHeight / 5, DestLocation
    /// = anchor; otherwise DestLocation is left untouched ("sticky")</item>
    /// <item>if |DestLocation - Location| &lt; 1.0 on EVERY axis (this is a
    /// delta-box, not a zero test), re-roll the jitter around the anchor:
    /// jx, jy in [-1, 1], jz in [0, 0.25H], one shared sign</item>
    /// <item>Coef = appFrand() * GroundSpeed / 80 / 3 (1.8 before the re-roll);
    /// Velocity = (DestLocation - Location) * Coef, NOT normalized</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Live: with CollisionHeight 22.8 the target spans 0.5H = 11.4 UU, and the
    /// measured owner-relative peak-to-peak was 3.26..3.53 UU, i.e. the cubic
    /// rarely reaches the full excursion. Coef is only ~0.48/s, which is exactly
    /// why the motion reads as a slow float rather than a vibration.
    /// </remarks>
    /// <param name="ownerLocation">Owner Location (+0x1BC), Unity space.</param>
    /// <param name="boneOrigin">"e_bone" origin, for the BONE anchor.</param>
    /// <param name="ownerForward">Owner rotation vector (FRotatorToVector).</param>
    /// <param name="ownerCollisionRadius">Owner CollisionRadius (+0x2F0), meters.</param>
    /// <param name="ownerCollisionHeight">Owner CollisionHeight (+0x2F4), meters.</param>
    /// <param name="groundSpeed">Pawn GroundSpeed (+0x1550), native UU.</param>
    /// <param name="waitType">Controller WaitType (+0x435).</param>
    /// <param name="isDead">Controller controllerFlags &amp; 1.</param>
    /// <param name="isRiding">Pawn RideFlags &amp; 1.</param>
    /// <param name="deltaTime">Frame delta.</param>
    /// <param name="rand">Uniform [0,1) source. Defaults to UnityEngine.Random;
    /// inject a deterministic one for tests.</param>
    public static void UpdateFollow(CubicCollection collection, Vector3 ownerLocation, Vector3 boneOrigin,
                             Vector3 ownerForward, float ownerCollisionRadius,
                             float ownerCollisionHeight, float groundSpeed,
                             byte waitType, bool isDead, bool isRiding,
                             float deltaTime, Func<float> rand = null)
    {
        List<CubicInstance> _cubics = collection.Items;
        if (rand == null) rand = () => UnityEngine.Random.value;

        // Height is already meters (same unit as Location). Native reach is H/5
        // in that same unit: 22.8 UU -> 4.56 UU. Converting H/5 again shrinks the
        // sticky window ~52x, so the bob target is thrown away every frame.
        float reach = ownerCollisionHeight / 5f;
        float deltaBox = VectorUtils.ConvertL2UuToMeters(1f);
        float signRef = VectorUtils.ConvertL2UuToMeters(2f);
        float coefBase = 1.8f; // flt_1F47834

        CubicConstants.AnchorFor(waitType, isDead, isRiding, ownerLocation,
                                 boneOrigin, ownerCollisionHeight,
                                 out Vector3 anchor, out Vector3 axis);

        int num = _cubics.Count;

        for (int i = 0; i < num; i++)
        {
            CubicInstance cubic = _cubics[i];
            if (cubic.MovementState != CubicConstants.StateFollow)
                continue;

            // bDead / WaitType == 2 flips the angle by half a turn (0x8000).
            int angle = CubicConstants.OrbitAngleFor(num, cubic.CubicIndex);
            if (isDead || waitType == 2)
                angle += 0x8000;

            Vector3 offset = CubicConstants.OrbitOffset(ownerForward, angle, axis,
                                                        ownerCollisionRadius);
            Vector3 anchorWithOffset = anchor + offset;

            if (Vector3.Distance(anchorWithOffset, cubic.Location) > reach)
                cubic.DestLocation = anchorWithOffset;
            // else: keep the previous destination ("sticky").

            Vector3 delta = cubic.DestLocation - cubic.Location;
            bool arrived = Mathf.Abs(delta.x) < deltaBox
                        && Mathf.Abs(delta.y) < deltaBox
                        && Mathf.Abs(delta.z) < deltaBox;

            float coef = coefBase;
            if (arrived)
            {
                // appFrand spans are UU (±1, and 0.25*H). Location is meters.
                // Leaving ±1 unconverted is a ±1 m lunge; live bob is ~3.5 UU.
                float jx = VectorUtils.ConvertL2UuToMeters(rand() * 2f - 1f);
                float jy = VectorUtils.ConvertL2UuToMeters(rand() * 2f - 1f);
                float jz = rand() * ownerCollisionHeight * 0.25f;

                // One shared sign for all three axes, chosen by where the sticky
                // destination sits relative to the anchor.
                float sign = (anchorWithOffset.z + signRef) >= cubic.DestLocation.z
                    ? 1f : -1f;

                cubic.DestLocation = new Vector3(
                    anchorWithOffset.x + sign * jx,
                    anchorWithOffset.y + sign * jy,
                    anchorWithOffset.z + sign * jz);

                coef = rand() * groundSpeed / 80f / 3f;
            }

            Vector3 diff = cubic.DestLocation - cubic.Location;
            cubic.Velocity = diff * coef; // deliberately not normalized

            Vector3 step = cubic.Velocity * deltaTime;
            if (step.magnitude > diff.magnitude)
                step = diff;
            cubic.Location += step;
        }
    }

    /// <summary>
    /// Native equivalent: <c>ANCubics::GetVelocity</c> (Live 0x1DF200), CAST
    /// branch (case 2, loc_1B7FEA0). Call once per frame per cubic while
    /// <see cref="CubicInstance.MovementState"/> == StateCast.
    ///
    /// <para>Live verified 2026-10-25 (9 cast sessions, one per type, probe
    /// <c>probe_cubic_cast_visual.js</c>). The destination depends on the cubic
    /// TYPE, which is the whole "flies at the monster vs hovers at the owner"
    /// difference:</para>
    /// <code>
    ///   type 1,3,8     DestLocation = Anchor          (stays at the orbit anchor)
    ///   type 2,4,5,6,7,9 DestLocation = TargetBonePos (flies to the monster)
    ///   default        RotPerSecond = 180 deg/s, return
    /// </code>
    ///
    /// <para>Velocity = (DestLocation - Location) * (target.GroundSpeed / 22.0)
    /// — NOT normalized, so the approach speed scales with how far the cubic is
    /// from its destination. The target is <c>TargetPawn</c> (+0x4F4), never the
    /// owner, so GroundSpeed is read from the monster even for Life Cubic (whose
    /// TargetPawn happens to BE the owner).</para>
    ///
    /// <para>Measured correlation (gapMax = |Dest - Location| at the switch
    /// frame, gapTgtMin = |Dest - target| at that frame, velMax = |Velocity|):</para>
    /// <list type="table">
    /// <item><term>1 Storm</term><description>gapMax 11.2, gapTgtMin 40.2, velMax 25.8 — anchor</description></item>
    /// <item><term>2 Vampiric</term><description>gapMax 38.3, gapTgtMin 15.8, velMax 88.6 — to monster</description></item>
    /// <item><term>3 Life</term><description>gapMax 10.8, gapTgtMin 22.4, velMax 40.7 — anchor (on owner)</description></item>
    /// <item><term>4 Viper</term><description>gapMax 38.2, gapTgtMin 16.5, velMax 88.5 — to monster</description></item>
    /// <item><term>5 Poltergeist</term><description>gapMax 38.8, gapTgtMin 16.1, velMax 89.9 — to monster</description></item>
    /// <item><term>6 Binding</term><description>gapMax 37.5, gapTgtMin 17.1, velMax 86.8 — to monster</description></item>
    /// <item><term>7 Aqua</term><description>gapMax 37.7, gapTgtMin 17.0, velMax 87.0 — to monster</description></item>
    /// <item><term>8 Spark</term><description>gapMax 10.8, gapTgtMin 40.2, velMax 25.0 — anchor</description></item>
    /// <item><term>9 Attract</term><description>gapMax 6.9, gapTgtMin 45.8, velMax 12.7 — anchor</description></item>
    /// </list>
    /// </summary>
    /// <param name="ownerLocation">Owner Location (+0x1BC), Unity space, for the anchor.</param>
    /// <param name="boneOrigin">Owner "e_bone" origin, for the BONE anchor.</param>
    /// <param name="ownerForward">Owner rotation vector (FRotatorToVector).</param>
    /// <param name="targetBonePos">TargetPawn "e_bone" origin (already summed with
    /// the alias-tag coords), Unity space. Only used by the offensive types.</param>
    /// <param name="targetGroundSpeed">TargetPawn GroundSpeed (+0x1550) in UU/s.
    /// The native divisor 22 is a length in UU, so UU/s divided by UU is 1/s and
    /// applies to a meter offset unchanged.</param>
    /// <param name="ownerCollisionHeight">Owner CollisionHeight (+0x2F4), for the
    /// BONE / MIDAIR anchors (meters).</param>
    /// <param name="waitType">Controller WaitType (+0x435).</param>
    /// <param name="isDead">Controller controllerFlags &amp; 1.</param>
    /// <param name="isRiding">Pawn RideFlags &amp; 1.</param>
    /// <param name="deltaTime">Frame delta.</param>
    public static void UpdateCast(CubicCollection collection, Vector3 ownerLocation, Vector3 boneOrigin,
                           Vector3 ownerForward, Vector3 targetBonePos,
                           float targetGroundSpeed, float ownerCollisionHeight,
                           byte waitType, bool isDead, bool isRiding,
                           float deltaTime)
    {
        List<CubicInstance> _cubics = collection.Items;
        CubicConstants.AnchorFor(waitType, isDead, isRiding, ownerLocation,
                                 boneOrigin, ownerCollisionHeight,
                                 out Vector3 anchor, out _);

        // dbl_1F3FAA0 = 22 UU. targetGroundSpeed is UU/s, so the quotient is 1/s.
        const float SpeedDivisor = 22f;
        float speedCoef = targetGroundSpeed / SpeedDivisor;

        for (int i = 0; i < _cubics.Count; i++)
        {
            CubicInstance cubic = _cubics[i];
            if (cubic.MovementState != CubicConstants.StateCast) continue;

            switch (cubic.CubicType)
            {
                case 1: case 3: case 8:
                    cubic.DestLocation = anchor;
                    break;
                case 2: case 4: case 5: case 6: case 7: case 9:
                    cubic.DestLocation = targetBonePos;
                    break;
                default:
                    continue; // native: RotPerSecond = 180 deg/s, no move
            }

            Vector3 diff = cubic.DestLocation - cubic.Location;
            cubic.Velocity = diff * speedCoef; // deliberately NOT normalized

            Vector3 step = cubic.Velocity * deltaTime;
            if (step.magnitude > diff.magnitude)
                step = diff;
            cubic.Location += step;
        }
    }
}
