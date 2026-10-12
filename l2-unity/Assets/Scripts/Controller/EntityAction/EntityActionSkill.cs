using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Apply-path skill execution for CharInfo / NPC / monster.
/// Mirrors local <c>PlayerWorldApply.OnMagicSkillUse</c> + physical/magic intentions,
/// but stays inside <see cref="EntityActionMachine"/> instead of <see cref="PlayerStateMachine"/>.
/// </summary>
public sealed class EntityActionSkill : IEntityActionProcess
{
    readonly L2PawnYaw _pawnYaw;

    public EntityActionSkill(L2PawnYaw pawnYaw)
    {
        _pawnYaw = pawnYaw;
    }

    public void Enter(Entity entity, object payload)
    {
        if (entity == null || entity.IsDead() || entity.Identity == null)
            return;

        MagicSkillUseDto magic = payload as MagicSkillUseDto;
        if (magic == null)
            return;

        if (TryApplyWeaponCharge(entity, magic))
            return;

        if (SetupDurationHelper.IsUsePotion(magic))
        {
            PlaySkillEffect(entity, magic);
            return;
        }

        bool wasMoving = entity.Identity != null && MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(entity.Identity.Id);
        if (IsWorldNpcOrMonster(entity))
            MobSkillLog.Begin(entity, magic.SkillId, wasMoving);
        bool keepMove = IsWorldNpcOrMonster(entity);
        EntityActionCombatLog.LogStep(entity,
            "SKILL CANCEL_MOVE nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + magic.SkillId +
            " wasMoving=" + wasMoving +
            " keepMove=" + keepMove +
            " nowL2=" + EntityActionCombatLog.L2(entity.transform.position) +
            " destL2=" + EntityActionCombatLog.L2(entity.ActionSlot.Destination));
        if (!keepMove)
            EntityActionVisual.CancelMove(entity);

        Entity target = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld.GetEntityNoLockSync(magic.TargetId)
            : null;
        bool selfCast = entity.Identity != null && entity.Identity.Id == magic.TargetId;

        if (!selfCast && target != null && target.IsDead())
        {
            EntityActionCombatLog.LogCiPawn(entity,
                "Skill SKIP deadTarget nick=" + EntityActionCombatLog.NameOf(entity) +
                " skill=" + magic.SkillId +
                " target=" + EntityActionCombatLog.Describe(target));
            return;
        }

        if (!IsWorldNpcOrMonster(entity))
            entity.InCombat = !SetupDurationHelper.IsUsePotion(magic);
        entity.ActionSlot.Target = target;
        if (target != null)
            entity.AttackTarget = target.transform;

        L2PawnRange.TrySnapUserToPacket(entity, magic.AttackerPos, "Skill pos");
        BeginFacing(entity, magic, target, selfCast);
        BeginVisualLock(entity, magic);

        EntityActionCombatLog.LogCiPawn(entity,
            "Skill.Enter nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + magic.SkillId +
            " lvl=" + magic.SkillLvl +
            " magic=" + (magic.SkillGrp != null ? magic.SkillGrp.IsMagic.ToString() : "-") +
            " hitTime=" + magic.HitTime +
            " self=" + selfCast +
            " target=" + EntityActionCombatLog.Describe(target));

        if (IncomingPacketActions.Animations != null && entity.Identity != null && magic.HitTime > 0)
            IncomingPacketActions.Animations.SetSpTimeAtk(entity.Identity.Id, magic.HitTime);

        if (TryPlayNoneAnimWorldPawnEffect(entity, magic))
            return;

        if (selfCast)
            ExecuteSelfSkill(entity, magic);
        else if (IsMagicSkill(magic))
            ExecuteMagicSkill(entity, magic, target);
        else
            ExecutePhysicalSkill(entity, magic);
    }

    public void Tick(Entity entity)
    {
        if (entity == null || entity.IsDead())
            return;

        UserEntity user = entity as UserEntity;
        if (user != null && user.IsAttackVisualPlaying())
            return;

        Entity target = entity.ActionSlot.Target;
        if (target == null && entity.AttackTarget != null)
            target = entity.AttackTarget.GetComponent<Entity>();
        if (target != null && target != entity && target.IsDead())
        {
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.CancelAttackKeepStance(entity);
            return;
        }

        if (user == null || !user.ShouldReturnFromAttack())
            return;

        user.ClearAttackVisual();
        if (entity.Identity != null)
            CombatFacingService.Instance?.EndFollow(entity.Identity.Id, "skill-tick");
        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Set(entity, EntityActionKind.Idle, null);
    }

    public static bool TryApplyWeaponCharge(Entity entity, MagicSkillUseDto magic)
    {
        if (entity == null || magic == null || !WeaponChargeSkill.IsCharge(magic.SkillId))
            return false;

        entity.IsSoulshotCharged = true;
        WeaponChargeSkill.Play(
            IncomingPacketActions.Effects,
            magic.SkillId,
            entity.GetWeaponTransform(),
            entity.transform);

        EntityActionCombatLog.LogCiPawn(entity,
            "Skill CHARGE nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + magic.SkillId);
        return true;
    }

    public static Entity ResolveEntity(int objectId)
    {
        if (objectId == 0)
            return null;

        if (IncomingPacketActions.GameWorld != null)
        {
            Entity fromWorld = IncomingPacketActions.GameWorld.GetEntityNoLockSync(objectId);
            if (fromWorld != null)
                return fromWorld;
        }

        if (PlayerEntity.Instance != null &&
            PlayerEntity.Instance.Identity != null &&
            PlayerEntity.Instance.Identity.Id == objectId)
            return PlayerEntity.Instance;

        return null;
    }

    public static MagicCastData ResolveCastData(int objectId)
    {
        Entity entity = ResolveEntity(objectId);
        return entity != null ? entity.GetMagicCastData() : null;
    }

    public static bool IsLocalPlayer(int objectId)
    {
        return PlayerEntity.Instance != null &&
            PlayerEntity.Instance.Identity != null &&
            PlayerEntity.Instance.Identity.Id == objectId;
    }

    public static void FinishRemoteCast(int objectId)
    {
        if (IsLocalPlayer(objectId))
            return;

        Entity entity = ResolveEntity(objectId);
        if (entity == null)
            return;

        CombatFacingService.Instance?.EndFollow(objectId, "skill-smb-idle");
        UserEntity user = entity as UserEntity;
        if (user != null)
            user.ClearAttackVisual();

        bool moving = entity.Identity != null && MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(entity.Identity.Id);
        bool moveAction = entity.ActionSlot != null && entity.ActionSlot.Action == EntityActionKind.Move;
        if (moving || moveAction)
        {
            if (moving)
            {
                MovementData move = MoveAllCharacters.Instance.GetMove(entity.Identity.Id);
                if (move != null)
                    move.ReplayUserGait();
            }
            EntityActionCombatLog.LogStep(entity,
                "SHOT KEEP_GAIT nick=" + EntityActionCombatLog.NameOf(entity) +
                " moving=" + moving +
                " action=" + (entity.ActionSlot != null ? entity.ActionSlot.Action.ToString() : "-"));
            return;
        }

        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Set(entity, EntityActionKind.Idle, null);
    }

    void BeginFacing(Entity entity, MagicSkillUseDto magic, Entity target, bool selfCast)
    {
        if (entity.Identity == null)
            return;

        int objectId = entity.Identity.Id;
        if (selfCast || target == null)
        {
            CombatFacingService.Instance?.EndFollow(objectId, "skill-self-or-no-target");
            return;
        }

        bool follow = IsMagicSkill(magic) || CombatFacingService.IsUsingBow(entity);
        if (follow)
        {
            int rate = IsMagicSkill(magic)
                ? L2PawnYaw.MagicRotationRateYaw
                : L2PawnYaw.RotationRateYaw;
            CombatFacingService.Ensure().BeginFollow(objectId, entity.transform, target.transform, rate);
            return;
        }

        CombatFacingService.Instance?.EndFollow(objectId, "non-bow-phys");
        _pawnYaw.RequestLook(entity.transform, target.transform.position, L2PawnYaw.RotationRateYaw);
    }

    static void BeginVisualLock(Entity entity, MagicSkillUseDto magic)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return;

        float durationSec = magic != null && magic.HitTime > 0
            ? magic.HitTime / 1000f
            : 0.4f;
        user.BeginAttackVisual(durationSec);
    }

    static void ExecutePhysicalSkill(Entity entity, MagicSkillUseDto magic)
    {
        AnimationCombo combo = GetCombo(magic);
        if (combo != null && SkillExecutor.Instance != null)
        {
            SkillExecutor.Instance.ExecuteSkill(entity, combo, ResolveEvents(entity));
            if (magic.SkillId == 3 && EffectManager.Instance != null)
            {
                EffectManager.Instance.PlayEffectSyncedToSkillAnimation(
                    magic.SkillId, entity, magic.HitTime, combo);
            }
            ArmPhysicalSkillHit(entity, magic);
            return;
        }

        EntityActionVisual.PlayPhysicalSkill(entity, magic);
        PlaySkillEffect(entity, magic);
        ArmPhysicalSkillHit(entity, magic);
    }

    static void ArmPhysicalSkillHit(Entity entity, MagicSkillUseDto magic)
    {
        if (HitManager.Instance == null || entity == null || magic == null)
            return;

        Entity target = entity.ActionSlot.Target;
        if (target == null && IncomingPacketActions.GameWorld != null)
            target = IncomingPacketActions.GameWorld.GetEntityNoLockSync(magic.TargetId);
        if (target == null || target == entity || target.IsDead())
            return;

        HitManager.Instance.ArmRemoteSkillHit(entity, target, magic);
    }

    static void ExecuteMagicSkill(Entity entity, MagicSkillUseDto magic, Entity target)
    {
        AnimationCombo combo = GetCombo(magic);
        string raceName = entity.RaceId.ToString();
        if (combo == null || SkillExecutor.Instance == null || magic.SkillGrp == null)
        {
            Debug.Log("[MAGIC_SHOT_LOG] SKIP override nick=" + EntityActionCombatLog.NameOf(entity) +
                " skill=" + magic.SkillId +
                " raceId=" + raceName +
                " combo=" + (combo != null) +
                " skillGrp=" + (magic.SkillGrp != null) +
                " executor=" + (SkillExecutor.Instance != null));
            PlaySkillEffect(entity, magic);
            EntityActionVisual.PlayPhysicalSkill(entity, magic);
            return;
        }
        Debug.Log("[MAGIC_SHOT_LOG] CAST override nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + magic.SkillId +
            " raceId=" + raceName +
            " hitTime=" + magic.HitTime);

        int objectId = entity.Identity.Id;
        string[] orderedCycle = SetupDurationHelper.BuildOrderedCycleForOverrideTiming(combo.GetAnimCycle());
        float[] durations = IncomingPacketActions.Animations != null
            ? IncomingPacketActions.Animations.GetOverrideClipsDurations(objectId, orderedCycle)
            : System.Array.Empty<float>();
        float shotEventTime = IncomingPacketActions.Animations != null
            ? SetupDurationHelper.ResolveShotEventTime(objectId, orderedCycle)
            : 0f;
        Transform targetTransform = target != null ? target.transform : entity.AttackTarget;
        float flightTimeMs = SetupDurationHelper.ResolveMagicFlightTimeMs(entity, magic.SkillId, targetTransform);
        entity.SetupTotalCastDuration(magic.HitTime, flightTimeMs, durations, shotEventTime, magic.TargetId, SetupDurationHelper.CastStyleOf(magic), SetupDurationHelper.MixComboOf(magic));

        SkillExecutor.Instance.ExecuteSkillOverride(magic.SkillGrp, entity, combo, ResolveEvents(entity));
    }

    static void ExecuteSelfSkill(Entity entity, MagicSkillUseDto magic)
    {
        if (SetupDurationHelper.IsLongCastSkill(magic))
        {
            AnimationCombo selfCombo = GetCombo(magic);
            if (selfCombo == null || SkillExecutor.Instance == null || magic.SkillGrp == null)
            {
                PlaySkillEffect(entity, magic);
                return;
            }

            int objectId = entity.Identity.Id;
            SetupDurationHelper.SetupLongCastDurationIfHitTimeNot0(magic, objectId, entity, selfCombo);
            SkillExecutor.Instance.ExecuteSkillOverride(
                magic.SkillGrp, entity, selfCombo, ResolveEvents(entity), isLong: true);
            return;
        }

        if (SetupDurationHelper.IsUsePotion(magic))
        {
            PlaySkillEffect(entity, magic);
            return;
        }

        if (IsMagicSkill(magic))
        {
            AnimationCombo combo = GetCombo(magic);
            if (combo != null && SkillExecutor.Instance != null)
            {
                int objectId = entity.Identity.Id;
                SetupDurationHelper.SetupDurationIfHitTimeNot0(magic, objectId, entity, combo);
                SkillExecutor.Instance.ExecuteSkillOverride(
                    magic.SkillGrp, entity, combo, ResolveEvents(entity));
                return;
            }
        }

        Skillgrp skillgrp = magic.SkillGrp;
        if (skillgrp != null && SkillExecutor.Instance != null)
        {
            AnimationCombo combo = new AnimationCombo(
                "-1", new[] { skillgrp.GetAnimOperationType3() }, "");
            SkillExecutor.Instance.ExecuteSkill(entity, combo, ResolveEvents(entity));
            return;
        }

        PlaySkillEffect(entity, magic);
    }

    static void PlaySkillEffect(Entity entity, MagicSkillUseDto magic)
    {
        EffectManager effects = IncomingPacketActions.Effects;
        if (effects == null || entity == null || magic == null)
            return;
        effects.PlayEffect(magic.SkillId, entity.transform, entity.GetMagicCastData());
    }

    static bool TryPlayNoneAnimWorldPawnEffect(Entity entity, MagicSkillUseDto magic)
    {
        if (!IsWorldNpcOrMonster(entity) || !IsSkillAnimationNone(magic))
            return false;

        Entity target = ResolveEntity(magic.TargetId);
        float flightTimeMs = SetupDurationHelper.ResolveMagicFlightTimeMs(
            entity,
            magic.SkillId,
            target != null ? target.transform : entity.AttackTarget);
        entity.SetupTotalCastDuration(
            magic.HitTime,
            flightTimeMs,
            Array.Empty<float>(),
            0f,
            magic.TargetId,
            SetupDurationHelper.CastStyleOf(magic),
            SetupDurationHelper.MixComboOf(magic));

        MagicCastData cast = entity.GetMagicCastData();
        int npcId = entity.Identity != null ? entity.Identity.NpcId : 0;
        string sequence;
        bool hasNpcOverride = MobSkillAnimgrpTable.Instance.TryGetSequence(
            npcId, magic.SkillId, out sequence);
        if (hasNpcOverride)
            sequence = ResolvePlayableNpcSkillSequence(entity, sequence);

        if (cast != null)
            cast.SpawnShotVisualsImmediately = !hasNpcOverride;

        EntityActionCombatLog.LogCiPawn(entity,
            "Skill NONE_ANIM FX nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + magic.SkillId +
            " npcId=" + npcId +
            " override=" + (hasNpcOverride ? sequence : "-") +
            " anim=" + (magic.SkillGrp != null ? magic.SkillGrp.Animation : "-"));
        PlaySkillEffect(entity, magic);

        if (hasNpcOverride &&
            IncomingPacketActions.Animations != null &&
            entity.Identity != null)
        {
            MobSkillLog.Play(entity, sequence);
            IncomingPacketActions.Animations.PlayMonsterAnimation(
                entity.Identity.Id, sequence);
        }

        return true;
    }

    static string ResolvePlayableNpcSkillSequence(Entity entity, string sequence)
    {
        if (entity == null ||
            string.IsNullOrEmpty(sequence) ||
            !sequence.StartsWith("spatk", StringComparison.OrdinalIgnoreCase))
        {
            return sequence;
        }

        Animator animator = entity.Animator != null
            ? entity.Animator
            : entity.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return sequence;

        string stateName = sequence;
        if (MonsterAnim.TryResolve(sequence, out string resolved, out _))
            stateName = resolved;
        if (animator.HasState(0, Animator.StringToHash(stateName)))
            return sequence;

        const string fallback = MonsterAnim.Atk01;
        if (!animator.HasState(0, Animator.StringToHash(fallback)))
            return sequence;

        Debug.LogWarning(
            "[MOBSKILL] Missing animation state nick=" + EntityActionCombatLog.NameOf(entity) +
            " npcId=" + (entity.Identity != null ? entity.Identity.NpcId : 0) +
            " requested=" + sequence +
            " fallback=" + fallback);
        return fallback;
    }

    public static string AnimatorStateName(Animator animator)
    {
        if (animator == null)
            return "none";

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            return StateName(state) + "->" + StateName(next);
        }

        return StateName(state);
    }

    static string StateName(AnimatorStateInfo state)
    {
        if (state.IsName("atk01")) return "atk01";
        if (state.IsName("atkwait")) return "atkwait";
        if (state.IsName("wait")) return "wait";
        if (state.IsName("walk")) return "walk";
        if (state.IsName("run")) return "run";
        if (state.IsName("spatk01")) return "spatk01";
        if (state.IsName("death")) return "death";
        return "other";
    }

    static bool IsWorldNpcOrMonster(Entity entity)
    {
        return entity is MonsterEntity || entity is NpcEntity;
    }

    static bool IsSkillAnimationNone(MagicSkillUseDto magic)
    {
        if (magic == null)
            return true;
        if (magic.SkillGrp == null)
            return true;
        string anim = magic.SkillGrp.Animation;
        return string.IsNullOrWhiteSpace(anim) ||
               string.Equals(anim, "None", StringComparison.OrdinalIgnoreCase);
    }

    static AnimationCombo GetCombo(MagicSkillUseDto magic)
    {
        if (magic == null || SkillgrpTable.Instance == null)
            return null;
        return SkillgrpTable.Instance.GetAnimComboBySkillId(magic.SkillId, magic.SkillLvl);
    }

    static AnimationEventsBase ResolveEvents(Entity entity)
    {
        if (entity == null || entity.Identity == null || IncomingPacketActions.Animations == null)
            return null;
        return IncomingPacketActions.Animations.GetAnimationEvents(entity.Identity.Id);
    }

    static bool IsMagicSkill(MagicSkillUseDto magic)
    {
        return magic != null && magic.SkillGrp != null && magic.SkillGrp.IsMagic == 1;
    }

}

public static class MobSkillLog
{
    const string Key = "MOBSKILL";

    sealed class Trace
    {
        public int skillId;
        public string before = "-";
        public bool wasMoving;
        public string seq = "-";
        public float clip = -1f;
        public float meleeMs = -1f;
        public float patkspd = -1f;
        public string request = "-";
        public bool clipEnded;
        public bool stuck;
    }

    static readonly Dictionary<int, Trace> _traces = new Dictionary<int, Trace>();

    public static void Begin(Entity entity, int skillId, bool wasMoving)
    {
        Trace trace = Get(entity);
        trace.skillId = skillId;
        trace.before = EntityActionSkill.AnimatorStateName(AnimatorOf(entity));
        trace.wasMoving = wasMoving;
        trace.seq = "-";
        trace.clip = -1f;
        trace.meleeMs = -1f;
        trace.patkspd = -1f;
        trace.request = "-";
        trace.clipEnded = false;
        trace.stuck = false;
        Write(entity, trace, "start");
    }

    public static void Play(Entity entity, string sequence)
    {
        Trace trace = Get(entity);
        trace.seq = string.IsNullOrEmpty(sequence) ? "-" : sequence;
    }

    public static void Enter(Entity entity, float clipLength, float meleeTimeMs, float patkspd)
    {
        Trace trace = Get(entity);
        trace.clip = clipLength;
        trace.meleeMs = meleeTimeMs;
        trace.patkspd = patkspd;
        Write(entity, trace, "enter");
    }

    public static void ClipEnd(Entity entity)
    {
        Get(entity).clipEnded = true;
    }

    public static string CurrentLocomotion(Entity entity)
    {
        bool moving = entity != null &&
            entity.Identity != null &&
            MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(entity.Identity.Id);
        if (!moving)
            return "wait";

        return entity.Running ? "run" : "walk";
    }

    public static void Done(Entity entity, string request)
    {
        Trace trace = Get(entity);
        trace.request = request;
        Write(entity, trace, "done");
    }

    public static void Stuck(Entity entity)
    {
        Trace trace = Get(entity);
        trace.stuck = true;
        Write(entity, trace, "stuck");
    }

    static Trace Get(Entity entity)
    {
        int id = entity != null && entity.Identity != null ? entity.Identity.Id : 0;
        Trace trace;
        if (!_traces.TryGetValue(id, out trace))
        {
            trace = new Trace();
            _traces[id] = trace;
        }

        return trace;
    }

    static void Write(Entity entity, Trace trace, string phase)
    {
        Animator animator = AnimatorOf(entity);
        bool moving = entity != null && entity.Identity != null &&
            MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(entity.Identity.Id);
        float norm = animator != null ? animator.GetCurrentAnimatorStateInfo(0).normalizedTime : -1f;
        Debug.Log(
            Key +
            " phase=" + phase +
            " nick=" + EntityActionCombatLog.NameOf(entity) +
            " skill=" + trace.skillId +
            " before=" + trace.before +
            " wasMoving=" + trace.wasMoving +
            " seq=" + trace.seq +
            " clip=" + trace.clip.ToString("F3") +
            " meleeMs=" + trace.meleeMs.ToString("F0") +
            " patkspd=" + trace.patkspd.ToString("F3") +
            " now=" + EntityActionSkill.AnimatorStateName(animator) +
            " norm=" + norm.ToString("F3") +
            " moving=" + moving +
            " inCombat=" + (entity != null && entity.InCombat) +
            " request=" + trace.request +
            " clipEnded=" + trace.clipEnded +
            " stuck=" + trace.stuck);
    }

    static Animator AnimatorOf(Entity entity)
    {
        if (entity == null || entity.GetAnimatorController() == null)
            return null;
        return entity.GetAnimatorController().GetAnimator();
    }
}
