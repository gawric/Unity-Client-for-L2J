using UnityEngine;

public static class EntityActionVisual
{
    public static void CancelMove(Entity entity)
    {
        if (entity == null || entity.Identity == null || MoveAllCharacters.Instance == null)
            return;
        if (entity.Identity.Id > 0)
        {
            MagicGaitTrace.Event(entity, "CancelMove", null);
            MoveAllCharacters.Instance.CancelMove(entity.Identity.Id);
            MoveAllCharacters.Instance.CancelRotate(entity.Identity.Id);
        }
    }

    public static void FreezeUserMove(Entity entity)
    {
        UserEntity user = entity as UserEntity;
        if (user == null || user.IsDead())
            return;
        NetworkAnimationController nac = user.GetAnimatorController();
        if (nac == null)
            return;
        nac.SetAnimatorSpeed(0f);
    }

    public static void StartMove(Entity entity, bool walking, bool force = false)
    {
        if (entity is UserEntity userStart)
        {
            // Debug.Log(userStart.LogTag + " Visual.StartMove walking=" + walking +
                // " hasAnim=" + userStart.HasAnimator() +
                // " nac=" + (userStart.GetAnimatorController() != null));
            PlayUserLocomotion(userStart, walking ? "walk" : "run", null, force);
            return;
        }

        if (!CanPlayAnim(entity))
        {
            CrasherSlideLog.Event(entity, "StartMove-NOANIM", "walking=" + walking);
            return;
        }

        if (entity is NpcEntity npc)
            npc.OnStartL2jMoving(walking);
        else if (entity is MonsterEntity monster && IncomingPacketActions.Animations != null)
        {
            string anim = walking ? AnimationNames.MONSTER_WALK.ToString() : AnimationNames.MONSTER_RUN.ToString();
            CrasherSlideLog.Event(monster, "StartMove", "anim=" + anim + " force=" + force);
            IncomingPacketActions.Animations.PlayMonsterAnimation(monster.Identity.Id, anim);
        }
    }

    public static void PlayDisappearWait(Entity entity)
    {
        if (entity == null || entity.IsDead())
            return;
        if (entity is UserEntity user)
        {
            PlayUserLocomotion(user, "wait", null, true);
            return;
        }
        StopMove(entity);
    }

    public static void StopMove(Entity entity)
    {
        if (entity is UserEntity userStop)
        {
            // Debug.Log(userStop.LogTag + " Visual.StopMove");
            PlayUserLocomotion(userStop, "wait");
            return;
        }

        if (!CanPlayAnim(entity))
            return;

        if (entity is NpcEntity npc)
            npc.OnStopL2jMoving();
        else if (entity is MonsterEntity monster)
        {
            if (IncomingPacketActions.Animations != null && monster.Identity != null)
            {
                CrasherSlideLog.Event(monster, "StopMove", "anim=wait");
                IncomingPacketActions.Animations.PlayMonsterAnimation(
                    monster.Identity.Id,
                    AnimationNames.MONSTER_WAIT.ToString());
            }
        }
    }

    public static void PlayMeleeAttack(Entity entity)
    {
        if (!CanPlayAnim(entity))
            return;

        if (entity is UserEntity user)
        {
            PlayUserMeleeAttack(user);
            return;
        }

        if (entity is MonsterEntity monster && IncomingPacketActions.Animations != null)
        {
            CrasherSlideLog.Event(monster, "PlayMelee", "anim=atk01");
            IncomingPacketActions.Animations.PlayMonsterAnimation(monster.Identity.Id, AnimationNames.MONSTER_ATK01.ToString());
        }
    }

    public static void PlayPhysicalSkill(Entity entity, MagicSkillUseDto magic)
    {
        if (!CanPlayAnim(entity))
            return;

        if (entity is UserEntity user)
        {
            PlayUserPhysicalSkill(user, magic);
            return;
        }

        PlayMeleeAttack(entity);
    }

    public static void RestoreLocomotion(Entity entity, string state)
    {
        if (entity == null || entity.Identity == null)
            return;
        if (state != "walk" && state != "run" && state != "wait")
            state = "wait";

        if (state == "wait")
        {
            StopMove(entity);
            return;
        }

        if (IncomingPacketActions.Animations == null)
            return;
        IncomingPacketActions.Animations.PlayMonsterAnimation(entity.Identity.Id, state);
    }

    public static void PlayStandWait(Entity entity)
    {
        if (entity == null || entity.IsDead())
            return;
        UserEntity user = entity as UserEntity;
        if (user != null && user.IsAttackVisualPlaying())
        {
            MagicGaitTrace.Event(entity, "PlayStandWait-SKIP", "attackVis=true");
            return;
        }
        MagicGaitTrace.Event(entity, "PlayStandWait",
            "inCombat=" + entity.InCombat + " → " + (entity.InCombat ? "atkwait" : "wait"));
        if (entity.InCombat)
            PlayCombatWait(entity);
        else
            StopMove(entity);
    }

    public static void PlayCombatWait(Entity entity, float? fadeDuration = null)
    {
        UserEntity userWait = entity as UserEntity;
        if (userWait != null && userWait.IsAttackVisualPlaying())
            return;

        if (entity is UserEntity user)
        {
            MagicGaitTrace.Event(entity, "PlayCombatWait", "→atkwait");
            PlayUserLocomotion(user, "atkwait", fadeDuration);
            return;
        }

        if (!CanPlayAnim(entity))
            return;

        if (entity is MonsterEntity monster && IncomingPacketActions.Animations != null && monster.Identity != null)
        {
            CrasherSlideLog.Event(monster, "PlayCombatWait", "anim=atkwait");
            IncomingPacketActions.Animations.PlayMonsterAnimation(monster.Identity.Id, "atkwait");
        }
    }

    public static void PlaySocial(Entity entity, int actionId)
    {
        if (actionId == 15 && IncomingPacketActions.Bus != null)
            IncomingPacketActions.Bus.LevelUp(entity, entity != null && entity.Identity != null ? entity.Identity.Id : 0);

        if (entity is UserEntity user)
        {
            NetworkAnimationController nac = user.GetAnimatorController();
            if (nac == null)
                return;
            nac.SetAnimatorSpeed(1f);
            nac.CrossFadeInFixedTime("social01", LocomotionCrossFadeSettings.FixedDuration);
            return;
        }

        if (entity is NpcEntity || entity is MonsterEntity)
        {
            if (IncomingPacketActions.Animations != null && entity.Identity != null)
                IncomingPacketActions.Animations.PlayMonsterAnimation(entity.Identity.Id, "social01");
        }
    }

    public static void PlayWaitType(Entity entity, WaitType waitType)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return;

        if (waitType == WaitType.WT_SITTING)
        {
            NetworkAnimationController nac = user.GetAnimatorController();
            if (nac == null)
                return;
            nac.SetAnimatorSpeed(1f);
            nac.CrossFadeInFixedTime("sit", LocomotionCrossFadeSettings.FixedDuration);
            return;
        }

        if (waitType == WaitType.WT_START_FAKEDEATH)
        {
            // WaitType 2 is not bDead. OnMoveTo stores it back to standing and walks.
            PlayUserDeath(user, false);
            return;
        }

        if (waitType == WaitType.WT_STOP_FAKEDEATH)
        {
            PlayRevive(user);
            return;
        }

        PlayUserLocomotion(user, "wait");
    }

    public static void PlayDeath(Entity entity, bool alreadyCorpse = false)
    {
        if (!alreadyCorpse && IsPlayingDeath(entity))
        {
            if (entity != null)
                entity.SetDead(true);
            return;
        }

        if (entity != null)
            entity.SetDead(true);

        if (!CanPlayAnim(entity))
            return;

        if (entity is UserEntity user)
        {
            PlayUserDeath(user, alreadyCorpse);
            return;
        }

        if (entity is MonsterEntity monster && IncomingPacketActions.Animations != null)
            IncomingPacketActions.Animations.PlayMonsterAnimation(monster.Identity.Id, AnimationNames.DEAD.ToString());
    }

    static void PlayUserLocomotion(UserEntity user, string move, float? fadeDuration = null, bool force = false)
    {
        if (user == null || user.IsDead())
            return;
        // During an attack swing we must NOT override the stance back to a stand
        // (wait/atkwait), but locomotion (run/walk) must still play so a chasing
        // pawn keeps running toward its target instead of freezing mid-swing.
        if (!force && user.IsAttackVisualPlaying() && move != null &&
            (move == "wait" || move == "atkwait"))
            return;

        NetworkAnimationController nac = user.GetAnimatorController();
        Animator animator = nac != null ? nac.GetAnimator() : null;
        string suffix = user.WeaponAnim;
        string state = move + "_" + suffix;
        // Debug.Log(user.LogTag + " Visual.PlayUserLocomotion move=" + move +
            // " weapon=" + suffix + " state=" + state +
            // " fade=" + (fadeDuration.HasValue ? fadeDuration.Value.ToString("F2") : "default") +
            // " nac=" + (nac != null) +
            // " animator=" + (animator != null ? animator.name : "null") +
            // " controller=" + (animator != null && animator.runtimeAnimatorController != null
                // ? animator.runtimeAnimatorController.name : "null"));
        if (nac == null)
            return;

        nac.SetAnimatorSpeed(1f);
        nac.SetPAtkSpeed(1f);
        float duration = fadeDuration ?? LocomotionCrossFadeSettings.ResolveDuration(state, nac);
        bool already = PlayerLocomotionCrossFade.IsAlreadyPlaying(nac, state);
        bool played;
        if (force)
        {
            // Bypass IsAlreadyPlaying short-circuit: caller explicitly wants this
            // state (re), even if the animator reports the clip as already active
            // or <none> mid-transition (pickup->run race).
            nac.CrossFadeInFixedTime(state, duration);
            played = true;
        }
        else
        {
            played = PlayerLocomotionCrossFade.TryPlay(nac, state, duration);
        }
        MagicGaitTrace.Event(user, "PlayUserLocomotion",
            "move=" + move +
            " state=" + state +
            " force=" + force +
            " already=" + already +
            " played=" + played +
            " fade=" + duration.ToString("F3"));
    }

    static readonly string[] UserMeleeVariants = { "jatk01_", "jatk02_", "jatk03_" };

    static void PlayUserMeleeAttack(UserEntity user)
    {
        if (user == null || user.Identity == null)
            return;

        NetworkAnimationController nacCheck = user.GetAnimatorController();
        Animator animator = nacCheck != null ? nacCheck.GetAnimator() : null;
        bool visLock = user.IsAttackVisualPlaying() && user.AttackVisualLeftSec() > 0.35f;
        bool clipLock = PlayerBasicAttackAnim.IsSwingPlaying(animator, 0.85f);
        if (visLock || clipLock)
        {
            // EntityActionCombatLog.LogCiPawn(user,
                // "Melee SKIP_RETRIGGER nick=" + EntityActionCombatLog.NameOf(user) +
                // " visLock=" + visLock +
                // " clipLock=" + clipLock +
                // " visLeft=" + user.AttackVisualLeftSec().ToString("F3") +
                // " " + EntityActionCombatLog.AnimDump(user));
            return;
        }

        string prefix = UserMeleeVariants[UnityEngine.Random.Range(0, UserMeleeVariants.Length)];
        string state = prefix + user.WeaponAnim;
        float cycleMs = AttackTimingHelper.ResolveAttackCycleMs(user, state);
        user.BeginAttackVisual(cycleMs / 1000f);
        NetworkAnimationController nacSpeed = user.GetAnimatorController();
        if (nacSpeed != null)
            nacSpeed.SetAnimatorSpeed(1f);

        if (IncomingPacketActions.Animations != null)
        {
            IncomingPacketActions.Animations.PlayAnimationTrigger(user.Identity.Id, prefix);
            // Debug.Log(user.LogTag + " Visual.MeleeAttack state=" + state + " cycleMs=" + cycleMs);
            return;
        }

        NetworkAnimationController nac = user.GetAnimatorController();
        if (nac == null)
            return;

        PlayerBasicAttackCrossFade.TryPlay(nac, state);
        // Debug.Log(user.LogTag + " Visual.MeleeAttack fallback state=" + state + " cycleMs=" + cycleMs);
    }

    static void PlayUserPhysicalSkill(UserEntity user, MagicSkillUseDto magic)
    {
        if (user == null || user.Identity == null)
            return;

        string trigger = ResolveUserPhysicalSkillTrigger(magic);
        bool basicJatk = !string.IsNullOrEmpty(trigger) &&
            trigger.StartsWith("jatk", System.StringComparison.OrdinalIgnoreCase);
        NetworkAnimationController nac = user.GetAnimatorController();
        Animator animator = nac != null ? nac.GetAnimator() : null;
        if (basicJatk &&
            (user.IsAttackVisualPlaying() || PlayerBasicAttackAnim.IsSwingPlaying(animator, 0.85f)))
        {
            // EntityActionCombatLog.LogCiPawn(user,
                // "Skill SKIP_JATK nick=" + EntityActionCombatLog.NameOf(user) +
                // " skill=" + (magic != null ? magic.SkillId.ToString() : "-") +
                // " lvl=" + (magic != null ? magic.SkillLvl.ToString() : "-") +
                // " trigger=" + trigger +
                // " visLeft=" + user.AttackVisualLeftSec().ToString("F3") +
                // " " + EntityActionCombatLog.AnimDump(user));
            return;
        }

        int objectId = user.Identity.Id;
        if (IncomingPacketActions.Animations != null && magic != null && magic.HitTime > 0)
            IncomingPacketActions.Animations.SetSpTimeAtk(objectId, magic.HitTime);

        string state = trigger + user.WeaponAnim;

        float cycleMs = magic != null && magic.HitTime > 0
            ? magic.HitTime
            : AttackTimingHelper.ResolveAttackCycleMs(user, state);
        user.BeginAttackVisual(cycleMs / 1000f);

        if (IncomingPacketActions.Animations != null)
        {
            IncomingPacketActions.Animations.PlayAnimationTrigger(objectId, trigger);
            // Debug.Log(user.LogTag + " Visual.PhysicalSkill trigger=" + trigger +
                // " state=" + state + " cycleMs=" + cycleMs);
            return;
        }

        PlayUserMeleeAttack(user);
    }

    static string ResolveUserPhysicalSkillTrigger(MagicSkillUseDto magic)
    {
        if (magic == null || SkillgrpTable.Instance == null)
            return UserMeleeVariants[UnityEngine.Random.Range(0, UserMeleeVariants.Length)];

        AnimationCombo combo = SkillgrpTable.Instance.GetAnimComboBySkillId(magic.SkillId, magic.SkillLvl);
        if (combo == null)
            return UserMeleeVariants[UnityEngine.Random.Range(0, UserMeleeVariants.Length)];

        string[] cycle = combo.GetAnimCycle();
        if (cycle == null)
            return UserMeleeVariants[UnityEngine.Random.Range(0, UserMeleeVariants.Length)];

        for (int i = 0; i < cycle.Length; i++)
        {
            string name = cycle[i];
            if (string.IsNullOrWhiteSpace(name))
                continue;

            string trimmed = name.Trim();
            if (string.Equals(trimmed, "none", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (trimmed.StartsWith("Cast", System.StringComparison.Ordinal) ||
                trimmed.StartsWith("MagicShot", System.StringComparison.Ordinal))
                break;
            if (trimmed.StartsWith("SpAtk", System.StringComparison.OrdinalIgnoreCase))
                return trimmed.EndsWith("_") ? trimmed : trimmed + "_";
        }

        return UserMeleeVariants[UnityEngine.Random.Range(0, UserMeleeVariants.Length)];
    }

    static void PlayUserDeath(UserEntity user, bool alreadyCorpse)
    {
        if (user == null || user.Identity == null || IncomingPacketActions.Animations == null)
            return;

        user.ClearAttackVisual();
        IncomingPacketActions.Animations.PlayExactAnimatorState(
            user.Identity.Id, PlayerDeathAnim.Death, alreadyCorpse);
        // Debug.Log(user.LogTag + " Visual.Death alreadyCorpse=" + alreadyCorpse);
    }

    public static void PlayRevive(Entity entity)
    {
        UserEntity user = entity as UserEntity;
        if (user == null || user.Identity == null || IncomingPacketActions.Animations == null)
            return;

        user.SetDead(false);
        user.ClearAttackVisual();
        float duration = IncomingPacketActions.Animations.PlayExactAnimatorState(
            user.Identity.Id, PlayerDeathAnim.Rebirth);
        user.BeginAttackVisual(duration);
        // Debug.Log(user.LogTag + " Visual.Revive duration=" + duration);
    }

    static bool CanPlayAnim(Entity entity)
    {
        if (entity != null && entity.HasAnimator())
            return true;

        return false;
    }

    static bool IsPlayingDeath(Entity entity)
    {
        if (entity == null)
            return false;

        NetworkAnimationController nac = entity.GetAnimatorController();
        Animator animator = nac != null ? nac.GetAnimator() : null;
        if (animator == null)
            return false;

        if (PlayerLocomotionCrossFade.IsAlreadyPlaying(animator, MonsterAnim.Death))
            return true;
        if (PlayerLocomotionCrossFade.IsAlreadyPlaying(animator, PlayerDeathAnim.Death))
            return true;

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
            return false;
        return clips[0].clip.name.IndexOf("death", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
