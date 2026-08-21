using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

public sealed class EntityActionMachine : ITickable
{
    public static EntityActionMachine Instance { get; private set; }

    private readonly Dictionary<EntityActionKind, IEntityActionProcess> _processes;
    private readonly List<Entity> _active = new List<Entity>();
    private readonly HashSet<int> _activeIds = new HashSet<int>();

    public EntityActionMachine(L2PawnRange pawnRange)
    {
        Instance = this;
        _processes = new Dictionary<EntityActionKind, IEntityActionProcess>
        {
            { EntityActionKind.Idle, new EntityActionIdle() },
            { EntityActionKind.Move, new EntityActionMove(pawnRange) },
            { EntityActionKind.Stop, new EntityActionStop(pawnRange) },
            { EntityActionKind.Attack, new EntityActionAttack() },
            { EntityActionKind.Skill, new EntityActionSkill() }
        };
    }

    public static bool IsAllowed(Entity entity)
    {
        return entity != null && !(entity is PlayerEntity);
    }

    public static bool ShouldHoldCombatIdle(Entity entity)
    {
        if (entity == null || !entity.InCombat)
            return false;
        if (entity.ActionSlot.Action == EntityActionKind.Move)
            return false;
        return entity.ActionSlot.Target == null && entity.AttackTarget == null;
    }

    public void Set(Entity entity, EntityActionKind action, object payload)
    {
        if (!IsAllowed(entity))
            return;

        EntityActionSlot slot = entity.ActionSlot;
        EntityActionCombatLog.RememberBeforeSet(slot.Action);
        slot.Write(action, payload, slot.Target, slot.Destination);

        IEntityActionProcess process;
        if (_processes.TryGetValue(action, out process))
            process.Enter(entity, payload);

        Track(entity, action);
    }

    public void Die(Entity entity, bool alreadyCorpse = false)
    {
        if (entity == null)
            return;

        EntityActionCombatLog.LogIfWatch(entity,
            "Die name=" + EntityActionCombatLog.NameOf(entity) +
            " allowed=" + IsAllowed(entity) +
            " alreadyCorpse=" + alreadyCorpse +
            " inCombat=" + entity.InCombat +
            " action=" + entity.ActionSlot.Action);

        if (IsAllowed(entity))
        {
            entity.InCombat = false;
            EntityActionVisual.CancelMove(entity);
            EntityActionVisual.PlayDeath(entity, alreadyCorpse);
            Set(entity, EntityActionKind.Stop, null);
        }

        StopAttackersOf(entity);
    }

    public void CancelAttackKeepStance(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        if (HitManager.Instance != null)
            HitManager.Instance.FlushRemoteMeleeHit(entity);

        EntityActionKind action = entity.ActionSlot.Action;
        entity.InCombat = true;
        entity.AttackTarget = null;
        entity.ActionSlot.Target = null;
        L2PawnRange.ClearIgnoredPawn(entity);
        EntityActionVisual.CancelMove(entity);

        EntityActionCombatLog.Watch(entity);
        EntityActionCombatLog.LogIfWatch(entity,
            "ReleaseDeadTarget keepSwing=" + IsFinishingSwing(entity) +
            " " + EntityActionCombatLog.Describe(entity) +
            " fromAction=" + action);
        EntityActionCombatLog.LogGap(entity, "ReleaseDeadTarget", null,
            " fromAction=" + action +
            " keepSwing=" + IsFinishingSwing(entity));

        if (IsFinishingSwing(entity) || action == EntityActionKind.Idle)
            return;

        Set(entity, EntityActionKind.Idle, null);
    }

    public void ApplyStop(Entity entity, StopMoveDto dto)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        EntityActionKind action = entity.ActionSlot.Action;
        if (IsSkillOrAttack(action) || entity.InCombat && action != EntityActionKind.Move)
        {
            EntityActionCombatLog.LogIfWatch(entity,
                "StopMove ignore name=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + action +
                " inCombat=" + entity.InCombat);
            EntityActionCombatLog.LogCiPawn(entity,
                "ApplyStop IGNORE nick=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + action +
                " inCombat=" + entity.InCombat);
            return;
        }

        EntityActionCombatLog.LogIfWatch(entity,
            "StopMove→Stop name=" + EntityActionCombatLog.NameOf(entity) +
            " action=" + action +
            " inCombat=" + entity.InCombat);
        EntityActionCombatLog.LogCiPawn(entity,
            "ApplyStop SET Stop nick=" + EntityActionCombatLog.NameOf(entity) +
            " action=" + action +
            " inCombat=" + entity.InCombat);
        Set(entity, EntityActionKind.Stop, dto);
    }

    void StopAttackersOf(Entity dead)
    {
        if (dead == null)
            return;

        List<Entity> attackers = new List<Entity>();
        CollectAttackersOf(dead, _active, attackers);

        World world = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld
            : World.Instance;
        if (world != null)
            world.ForEachEntity(entity => CollectAttacker(dead, entity, attackers));

        if (EntityActionCombatLog.IsWatch(dead) || EntityActionCombatLog.ContainsWatch(attackers))
        {
            string names = "";
            for (int i = 0; i < attackers.Count; i++)
            {
                if (i > 0)
                    names += ", ";
                names += EntityActionCombatLog.Describe(attackers[i]) +
                    "(action=" + attackers[i].ActionSlot.Action +
                    " inCombat=" + attackers[i].InCombat + ")";
            }
            EntityActionCombatLog.Log(
                "StopAttackersOf dead=" + EntityActionCombatLog.NameOf(dead) +
                " count=" + attackers.Count +
                " active=" + _active.Count +
                " attackers=[" + names + "]");
        }

        for (int i = 0; i < attackers.Count; i++)
            CancelAttackKeepStance(attackers[i]);
    }

    static void CollectAttackersOf(Entity dead, List<Entity> source, List<Entity> attackers)
    {
        if (source == null)
            return;
        for (int i = 0; i < source.Count; i++)
            CollectAttacker(dead, source[i], attackers);
    }

    static void CollectAttacker(Entity dead, Entity attacker, List<Entity> attackers)
    {
        if (attacker == null || attacker == dead || attacker.IsDead())
            return;
        if (!IsTargeting(attacker, dead))
            return;
        if (!attackers.Contains(attacker))
            attackers.Add(attacker);
    }

    static bool IsTargeting(Entity attacker, Entity dead)
    {
        if (attacker.ActionSlot.Target == dead)
            return true;
        if (attacker.AttackTarget != null && dead != null && dead.transform != null)
            return attacker.AttackTarget == dead.transform;
        return false;
    }

    public void Revive(Entity entity)
    {
        if (!IsAllowed(entity))
            return;

        entity.SetDead(false);
        if (entity.Status != null && entity.Status.GetHp() <= 0)
        {
            float maxHp = entity.Stats != null && entity.Stats.MaxHp > 0 ? entity.Stats.MaxHp : 100f;
            entity.Status.SetHp(maxHp);
        }
        EntityActionVisual.CancelMove(entity);
        Set(entity, EntityActionKind.Attack, new ReviveDto());
    }

    public void StopAttack(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        entity.InCombat = false;
        RefreshStandWaitIfIdle(entity, "AutoAttackStop");
    }

    public void StartAttackStance(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;

        entity.InCombat = true;
        RefreshStandWaitIfIdle(entity, "AutoAttackStart");
    }

    void RefreshStandWaitIfIdle(Entity entity, string reason)
    {
        EntityActionKind action = entity.ActionSlot.Action;
        bool busy = action == EntityActionKind.Move ||
            IsSkillOrAttack(action) ||
            IsFinishingSwing(entity);
        if (busy)
        {
            EntityActionCombatLog.LogIfWatch(entity,
                reason + " FLAG_ONLY name=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + action);
            EntityActionCombatLog.LogCiPawn(entity,
                reason + " FLAG_ONLY nick=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + action +
                " inCombat=" + entity.InCombat +
                " pawn=" + EntityActionCombatLog.Describe(EntityActionCombatLog.PawnOf(entity)));
            return;
        }

        EntityActionCombatLog.LogIfWatch(entity,
            reason + " REFRESH name=" + EntityActionCombatLog.NameOf(entity) +
            " inCombat=" + entity.InCombat);
        EntityActionCombatLog.LogCiPawn(entity,
            reason + " REFRESH nick=" + EntityActionCombatLog.NameOf(entity) +
            " action=" + action +
            " inCombat=" + entity.InCombat);
        EntityActionVisual.PlayStandWait(entity);
    }

    public void Social(Entity entity, SocialActionDto dto)
    {
    }

    public void ChangeWaitType(Entity entity, ChangeWaitTypeDto dto)
    {
        if (!IsAllowed(entity) || dto == null)
            return;
        if (entity.IsDead() && dto.WaitType != WaitType.WT_STOP_FAKEDEATH)
            return;

        EntityActionVisual.CancelMove(entity);
        EntityActionVisual.PlayWaitType(entity, dto.WaitType);
        if (dto.WaitType == WaitType.WT_STANDING || dto.WaitType == WaitType.WT_STOP_FAKEDEATH)
            Set(entity, EntityActionKind.Idle, null);
        else
        {
            entity.ActionSlot.Write(EntityActionKind.Stop, dto, entity.ActionSlot.Target, entity.ActionSlot.Destination);
            Track(entity, EntityActionKind.Stop);
        }
    }

    public void NotifyArrived(Entity entity)
    {
        if (!IsAllowed(entity) || entity.IsDead())
            return;
        if (IsSkillOrAttack(entity.ActionSlot.Action) || ShouldHoldCombatIdle(entity))
        {
            EntityActionCombatLog.LogIfWatch(entity,
                "NotifyArrived ignore name=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + entity.ActionSlot.Action +
                " inCombat=" + entity.InCombat);
            EntityActionCombatLog.LogCiPawn(entity,
                "Arrived IGNORE nick=" + EntityActionCombatLog.NameOf(entity) +
                " action=" + entity.ActionSlot.Action +
                " inCombat=" + entity.InCombat);
            return;
        }

        Entity pawn = EntityActionCombatLog.PawnOf(entity);
        if (entity is UserEntity && pawn != null && entity.ActionSlot.PawnDist > 0.01f)
        {
            EntityActionVisual.CancelMove(entity);
            string pose = "Arrived KEEP_RUN";
            EntityActionCombatLog.MarkArrivedPose(pose);
            EntityActionCombatLog.LogCiPawn(entity,
                pose +
                " nick=" + EntityActionCombatLog.NameOf(entity) +
                " nowToPawn=" + VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2") +
                " pawnDist=" + entity.ActionSlot.PawnDist.ToString("F2") +
                " inCombat=" + entity.InCombat +
                " pawn=" + EntityActionCombatLog.Describe(pawn) +
                EntityActionCombatLog.ChaseDump(entity, pawn));
            float arrivedToPawn = VectorUtils.Distance2D(entity.transform.position, pawn.transform.position);
            if (arrivedToPawn >= 2f)
                EntityActionCombatLog.LogGap(entity, "Arrived KEEP_RUN_FAR", pawn);
            CharInfoSpeedLog.LogArrive(entity, pose);
            return;
        }

        EntityActionCombatLog.LogCiPawn(entity,
            "Arrived→Idle nick=" + EntityActionCombatLog.NameOf(entity) +
            " now=" + EntityActionCombatLog.Vec(entity.transform.position) +
            " dest=" + EntityActionCombatLog.Vec(entity.ActionSlot.Destination) +
            " pawn=" + EntityActionCombatLog.Describe(pawn) +
            " nowToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position).ToString("F2")
                : "-") +
            " destToPawn=" + (pawn != null
                ? VectorUtils.Distance2D(entity.ActionSlot.Destination, pawn.transform.position).ToString("F2")
                : "-") +
            " " + EntityActionCombatLog.ClassifyDest(entity.transform.position, pawn));
        Set(entity, EntityActionKind.Idle, null);
    }

    public void Remove(Entity entity)
    {
        if (entity == null || entity.Identity == null)
            return;

        L2PawnRange.ClearIgnoredPawn(entity);
        _activeIds.Remove(entity.Identity.Id);
        _active.Remove(entity);
        entity.ActionSlot.Clear();
    }

    public void Tick()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            Entity entity = _active[i];
            if (entity == null)
            {
                _active.RemoveAt(i);
                continue;
            }

            IEntityActionProcess process;
            if (_processes.TryGetValue(entity.ActionSlot.Action, out process))
                process.Tick(entity);
        }
    }

    public static bool IsFinishingSwing(Entity entity)
    {
        UserEntity user = entity as UserEntity;
        return user != null && user.IsAttackVisualPlaying();
    }

    public static bool IsSkillOrAttack(EntityActionKind action)
    {
        return action == EntityActionKind.Attack || action == EntityActionKind.Skill;
    }

    void Track(Entity entity, EntityActionKind action)
    {
        if (entity.Identity == null)
            return;

        int id = entity.Identity.Id;
        if (action == EntityActionKind.Idle || action == EntityActionKind.Stop)
        {
            if (_activeIds.Remove(id))
                _active.Remove(entity);
            return;
        }

        if (_activeIds.Add(id))
            _active.Add(entity);
    }
}

public static class EntityActionCombatLog
{
    const string Tag = "[EntityAction:Combat]";
    static readonly HashSet<int> WatchIds = new HashSet<int>();
    static readonly L2PawnRange DistRing = new L2PawnRange();
    static int _chasePawnId;
    static Vector3 _chasePawnStart;
    static Vector3 _chaseDestHint;
    static float _chaseStartTime;
    static float _arrivedTime;
    static string _arrivedPose;
    static float _attackEnterTime;
    static int _attackSeq;
    static EntityActionKind _prevAction;

    public static void RememberBeforeSet(EntityActionKind action)
    {
        _prevAction = action;
    }

    public static void Watch(Entity entity)
    {
        int id = IdOf(entity);
        if (id != 0)
            WatchIds.Add(id);
    }

    public static int IdOf(Entity entity)
    {
        return entity != null && entity.Identity != null ? entity.Identity.Id : 0;
    }

    public static string Describe(Entity entity)
    {
        return "name=" + NameOf(entity) + " id=" + IdOf(entity);
    }

    public static bool IsWatch(Entity entity)
    {
        if (entity == null)
            return false;
        int id = IdOf(entity);
        if (id != 0 && WatchIds.Contains(id))
            return true;
        if (!Matches(NameOf(entity)))
            return false;
        Watch(entity);
        return true;
    }

    public static bool ContainsWatch(List<Entity> entities)
    {
        if (entities == null)
            return false;
        for (int i = 0; i < entities.Count; i++)
        {
            if (IsWatch(entities[i]))
                return true;
        }
        return false;
    }

    public static bool Matches(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        string n = name.ToLowerInvariant();
        return n.IndexOf("keltir") >= 0 || n.IndexOf("mmagic1") >= 0 || n.IndexOf("beard") >= 0;
    }

    public static string NameOf(Entity entity)
    {
        if (entity == null)
            return "null";
        UserEntity user = entity as UserEntity;
        if (user != null)
            return user.Nick;
        if (entity.Identity != null && !string.IsNullOrEmpty(entity.Identity.Name))
            return entity.Identity.Name;
        return entity.name;
    }

    public static void Log(string message)
    {
        Debug.Log(Tag + " " + message);
    }

    public static void LogIfWatch(Entity entity, string message)
    {
        if (IsWatch(entity))
            Log(message);
    }

    public static void LogIfWatch(Entity a, Entity b, string message)
    {
        if (IsWatch(a) || IsWatch(b))
            Log(message);
    }

    public static bool IsCharInfo(Entity entity)
    {
        return entity is UserEntity;
    }

    public static Entity PawnOf(Entity entity)
    {
        if (entity == null)
            return null;
        Entity target = entity.ActionSlot.Target;
        if (target != null && target != entity)
            return target;
        if (entity.AttackTarget == null)
            return null;
        Entity fromTransform = entity.AttackTarget.GetComponent<Entity>();
        if (fromTransform == null || fromTransform == entity)
            return null;
        return fromTransform;
    }

    public static Entity ResolvePawn(Entity mover, Vector3 dest)
    {
        Entity pawn = PawnOf(mover);
        if (pawn != null)
            return pawn;
        return FindNearDest(dest, mover, 0.45f);
    }

    public static Entity FindNearDest(Vector3 dest, Entity skip, float radius)
    {
        World world = IncomingPacketActions.GameWorld != null
            ? IncomingPacketActions.GameWorld
            : World.Instance;
        if (world == null)
            return null;
        Entity found = null;
        float best = radius;
        world.ForEachEntity(e =>
        {
            if (e == null || e == skip || e is UserEntity || e is PlayerEntity)
                return;
            if (e.transform == null)
                return;
            float d = VectorUtils.Distance2D(dest, e.transform.position);
            if (d < best)
            {
                best = d;
                found = e;
            }
        });
        return found;
    }

    public static string Vec(Vector3 v)
    {
        return "(" + v.x.ToString("F2") + ", " + v.y.ToString("F2") + ", " + v.z.ToString("F2") + ")";
    }

    public static string ClassifyDest(Vector3 dest, Entity pawn)
    {
        if (pawn == null)
            return "POINT";
        float destToPawn = VectorUtils.Distance2D(dest, pawn.transform.position);
        if (destToPawn < 0.40f)
            return "TO_CENTER destToPawn=" + destToPawn.ToString("F2");
        return "TO_OFFSET destToPawn=" + destToPawn.ToString("F2");
    }

    public static void LogCiPawn(Entity entity, string message)
    {
        if (!IsCharInfo(entity))
            return;
        Debug.Log("[CI_PAWN] " + message);
    }

    public static void LogGap(Entity entity, string reason, Entity pawn, string extra = "")
    {
        if (!IsCharInfo(entity))
            return;
        float nowToPawn = pawn != null
            ? VectorUtils.Distance2D(entity.transform.position, pawn.transform.position)
            : -1f;
        bool moving = entity.Identity != null && MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(entity.Identity.Id);
        Debug.Log("[CI_GAP] " + reason +
            " nick=" + NameOf(entity) +
            " action=" + entity.ActionSlot.Action +
            " inCombat=" + entity.InCombat +
            " moving=" + moving +
            " nowToPawn=" + nowToPawn.ToString("F2") +
            " pawnDist=" + entity.ActionSlot.PawnDist.ToString("F2") +
            " dest=" + Vec(entity.ActionSlot.Destination) +
            " now=" + Vec(entity.transform.position) +
            " pawn=" + Describe(pawn) +
            (pawn != null ? " pawnPos=" + Vec(pawn.transform.position) : "") +
            extra);
    }

    public static bool IsPawnMoving(Entity pawn)
    {
        return pawn != null && pawn.Identity != null &&
            MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(pawn.Identity.Id);
    }

    public static string AnimDump(Entity entity)
    {
        if (entity == null)
            return "anim=-";
        NetworkAnimationController nac = entity.GetAnimatorController();
        Animator animator = nac != null ? nac.GetAnimator() : null;
        if (animator == null)
            return "anim=-";
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        string clip = clips != null && clips.Length > 0 && clips[0].clip != null
            ? clips[0].clip.name
            : "?";
        AnimatorStateInfo st = animator.GetCurrentAnimatorStateInfo(0);
        return "anim=" + clip +
            " n=" + st.normalizedTime.ToString("F2") +
            " spd=" + animator.speed.ToString("F2");
    }

    public static string VisualDump(Entity entity)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return "vis=-";
        return "visPlay=" + user.IsAttackVisualPlaying() +
            " visLeft=" + user.AttackVisualLeftSec().ToString("F3");
    }

    public static void MarkChaseStart(Entity user, Entity pawn, Vector3 destHint)
    {
        _chasePawnId = IdOf(pawn);
        _chasePawnStart = pawn != null ? pawn.transform.position : Vector3.zero;
        _chaseDestHint = destHint;
        _chaseStartTime = Time.time;
        _arrivedTime = 0f;
        _arrivedPose = "";
        _attackEnterTime = 0f;
        _attackSeq = 0;
    }

    public static void MarkArrivedPose(string pose)
    {
        _arrivedTime = Time.time;
        _arrivedPose = pose;
    }

    public static string ChaseDump(Entity user, Entity pawn)
    {
        if (user == null)
            return "";
        float pawnShift = pawn != null && IdOf(pawn) == _chasePawnId
            ? VectorUtils.Distance2D(_chasePawnStart, pawn.transform.position)
            : -1f;
        float toHint = VectorUtils.Distance2D(user.transform.position, user.ActionSlot.Destination);
        float toLive = -1f;
        if (pawn != null && user.ActionSlot.PawnDist > 0.01f)
        {
            Vector3 live = DistRing.StopPointOnDistRing(
                user.transform.position, pawn.transform.position, user.ActionSlot.PawnDist);
            toLive = VectorUtils.Distance2D(user.transform.position, live);
        }
        bool pawnMoving = IsPawnMoving(pawn);
        string mismatch = pawnShift > 0.40f && toHint <= 0.50f ? " SNAP_ON_MOVE" : "";
        return " pawnShift=" + pawnShift.ToString("F2") +
            " pawnMoving=" + pawnMoving +
            " chaseSec=" + (_chaseStartTime > 0f ? (Time.time - _chaseStartTime).ToString("F2") : "-") +
            " toHint=" + toHint.ToString("F2") +
            " toLive=" + toLive.ToString("F2") +
            " destHint=" + Vec(_chaseDestHint) +
            " pawnStart=" + Vec(_chasePawnStart) +
            " pawnNow=" + (pawn != null ? Vec(pawn.transform.position) : "-") +
            " " + AnimDump(user) +
            mismatch;
    }

    public static string AttackDump(Entity user, Entity target)
    {
        _attackSeq++;
        float sinceArrived = _arrivedTime > 0f ? Time.time - _arrivedTime : -1f;
        float sinceLast = _attackEnterTime > 0f ? Time.time - _attackEnterTime : -1f;
        _attackEnterTime = Time.time;
        return " atkSeq=" + _attackSeq +
            " sinceArrived=" + sinceArrived.ToString("F2") +
            " sinceLastAtk=" + sinceLast.ToString("F2") +
            " arrivedPose=" + (string.IsNullOrEmpty(_arrivedPose) ? "-" : _arrivedPose) +
            " prevAction=" + _prevAction +
            " " + VisualDump(user) +
            ChaseDump(user, target);
    }

    public static string IdleFromSwingDump(Entity user)
    {
        float sinceAtk = _attackEnterTime > 0f ? Time.time - _attackEnterTime : -1f;
        return " sinceAtk=" + sinceAtk.ToString("F2") +
            " atkSeq=" + _attackSeq +
            " arrivedPose=" + (string.IsNullOrEmpty(_arrivedPose) ? "-" : _arrivedPose) +
            " " + VisualDump(user) +
            " " + AnimDump(user);
    }
}
