using UnityEngine;

/// <summary>
/// Apply-path skill execution for CharInfo / NPC / monster.
/// Mirrors local <c>PlayerWorldApply.OnMagicSkillUse</c> + physical/magic intentions,
/// but stays inside <see cref="EntityActionMachine"/> instead of <see cref="PlayerStateMachine"/>.
/// </summary>
public sealed class EntityActionSkill : IEntityActionProcess
{
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
        if (entity == null || magic == null || !IsWeaponChargeShot(magic.SkillId))
            return false;

        entity.IsSoulshotCharged = true;
        Transform weapon = entity.GetWeaponTransform();
        EffectManager effects = IncomingPacketActions.Effects;
        if (effects != null && weapon != null)
            effects.PlayEffect(magic.SkillId, weapon);
        else if (effects != null)
            effects.PlayEffect(magic.SkillId, entity.transform);

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

        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Set(entity, EntityActionKind.Idle, null);
    }

    static void BeginFacing(Entity entity, MagicSkillUseDto magic, Entity target, bool selfCast)
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
            CombatFacingService.Ensure().BeginFollow(objectId, entity.transform, target.transform);
            return;
        }

        CombatFacingService.Instance?.EndFollow(objectId, "non-bow-phys");
        EntityActionVisual.FaceTowards(entity, target.transform.position);
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
        if (combo == null || SkillExecutor.Instance == null || magic.SkillGrp == null)
        {
            PlaySkillEffect(entity, magic);
            EntityActionVisual.PlayPhysicalSkill(entity, magic);
            return;
        }

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
        entity.SetupTotalCastDuration(magic.HitTime, flightTimeMs, durations, shotEventTime, magic.TargetId);

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

    static bool IsWeaponChargeShot(int skillId)
    {
        return skillId == (int)SpecialSkillType.SoulshotNg ||
               skillId == (int)SpecialSkillType.SpiritshotNg;
    }
}
