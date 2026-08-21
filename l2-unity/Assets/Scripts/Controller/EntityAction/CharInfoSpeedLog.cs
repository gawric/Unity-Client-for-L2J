using System;
using UnityEngine;

/// <summary>
/// Speed probe for CharInfo (UserEntity) and Elder Keltir (MonsterEntity).
/// Same shape as official-client [SPD] PawnSpeed.log.
/// Compare packet GroundSpeed vs actual 2D displacement.
/// </summary>
public static class CharInfoSpeedLog
{
    const string Tag = "[SPD]";
    const float IntervalSec = 0.20f;
    const float L2UuToM = 1f / 52.5f;

    public static void LogPacket(UserEntity user, string where)
    {
        if (user == null)
            return;

        Stats s = user.Stats;
        float runReal = s != null ? s.RunRealSpeed : 0f;
        float walkReal = s != null ? s.WalkRealSpeed : 0f;
        int runBase = s != null ? s.BaseRunSpeed : 0;
        int walkBase = s != null ? s.BaseWalkingSpeed : 0;
        float moveMul = runBase > 0 ? runReal / runBase : 0f;
        float unityRun = s != null ? s.UnitySpeedRun : 0f;
        float unityWalk = s != null ? s.UnitySpeedWalking : 0f;
        bool identRun = user.Identity != null && user.Identity.IsRunning;
        bool appearRun = user.Appearance is PlayerAppearance pa && pa.Running;

        Debug.Log(Tag + " PKT " + where +
            " nick=" + user.Nick +
            " id=" + (user.Identity != null ? user.Identity.Id : 0) +
            " runBase=" + runBase +
            " walkBase=" + walkBase +
            " moveMul=" + moveMul.ToString("F5") +
            " runReal=" + runReal.ToString("F3") +
            " walkReal=" + walkReal.ToString("F3") +
            " groundFromMul=" + (runBase * moveMul).ToString("F3") +
            " unityRun=" + unityRun.ToString("F4") +
            " unityWalk=" + unityWalk.ToString("F4") +
            " l2RunMs=" + (runReal * L2UuToM).ToString("F4") +
            " scale0189=" + NumberUtils.ScaleToUnity(runReal).ToString("F4") +
            " running=" + (user.Running ? 1 : 0) +
            " identRun=" + (identRun ? 1 : 0) +
            " appearRun=" + (appearRun ? 1 : 0) +
            " loc=" + Vec(user.transform.position) +
            " " + ScaleDump(user, unityRun) +
            " " + EntityActionCombatLog.AnimDump(user));
    }

    public static void LogSnap(UserEntity user, Vector3 packetPos, string where)
    {
        if (user == null)
            return;

        float snap2d = VectorUtils.Distance2D(user.transform.position, packetPos);
        bool moving = user.Identity != null &&
            MoveAllCharacters.Instance != null &&
            MoveAllCharacters.Instance.IsMoving(user.Identity.Id);
        if (snap2d < 0.02f && !moving)
            return;

        Debug.Log(Tag + " SNAP " + where +
            " nick=" + user.Nick +
            " snap2d=" + snap2d.ToString("F3") +
            " moving=" + (moving ? 1 : 0) +
            " running=" + (user.Running ? 1 : 0) +
            " identRun=" + (user.Identity != null && user.Identity.IsRunning ? 1 : 0) +
            " from=" + Vec(user.transform.position) +
            " packet=" + Vec(packetPos) +
            " " + EntityActionCombatLog.AnimDump(user));
    }

    public static void LogMoveStart(Entity entity, string kind, float dist2d, float toPawn, float stopDist)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return;

        Stats s = user.Stats;
        float used = user.Running
            ? (s != null ? s.UnitySpeedRun : 0f)
            : (s != null ? s.UnitySpeedWalking : 0f);

        Debug.Log(Tag + " MOVE START kind=" + kind +
            " nick=" + user.Nick +
            " dist2d=" + dist2d.ToString("F2") +
            " toPawn=" + toPawn.ToString("F2") +
            " stopDist=" + stopDist.ToString("F2") +
            " usedMs=" + used.ToString("F4") +
            " running=" + (user.Running ? 1 : 0) +
            " identRun=" + (user.Identity != null && user.Identity.IsRunning ? 1 : 0) +
            " appearRun=" + (user.Appearance is PlayerAppearance pa && pa.Running ? 1 : 0) +
            " walkAnim=" + (!user.Running ? 1 : 0) +
            " loc=" + Vec(user.transform.position) +
            " " + EntityActionCombatLog.AnimDump(user));
    }

    /// <summary>
    /// L2 Dist is 2D Location→Location in UU. Unity stop is the same ring in meters.
    /// </summary>
    public static void LogStop(Entity entity, Entity pawn, string reason)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return;

        float toPawn = pawn != null
            ? VectorUtils.Distance2D(user.transform.position, pawn.transform.position)
            : -1f;
        float stopDist = user.ActionSlot.PawnDist;
        CharacterController cc = user.GetCharacterController();
        CharacterController pawnCc = pawn != null ? pawn.GetComponent<CharacterController>() : null;
        float ccR = cc != null ? cc.radius : 0f;
        float pawnR = pawnCc != null ? pawnCc.radius : 0f;
        float pktCh = user.Appearance != null ? user.Appearance.CollisionHeight : 0f;
        float pktCr = user.Appearance != null ? user.Appearance.CollisionRadius : 0f;

        Debug.Log("[SPD] STOP " + reason +
            " nick=" + user.Nick +
            " toPawn=" + toPawn.ToString("F3") +
            " toPawnUu=" + (toPawn * 52.5f).ToString("F2") +
            " stopDist=" + stopDist.ToString("F3") +
            " stopDistUu=" + (stopDist * 52.5f).ToString("F2") +
            " slack=0.00" +
            " ccR=" + ccR.ToString("F3") +
            " ccH=" + (cc != null ? cc.height.ToString("F2") : "-") +
            " pktCR=" + pktCr.ToString("F2") +
            " pktCH=" + pktCh.ToString("F2") +
            " pawnCcR=" + pawnR.ToString("F3") +
            " loc=" + Vec(user.transform.position) +
            " pawnLoc=" + (pawn != null ? Vec(pawn.transform.position) : "-") +
            " " + EntityActionCombatLog.AnimDump(user));
    }

    public static void LogArrive(Entity entity, string reason)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return;

        Entity pawn = EntityActionCombatLog.PawnOf(user);
        PacketLatencyLog.MarkDist();
        LogStop(user, pawn, reason);
        Debug.Log(Tag + " ARRIVE " + reason +
            " nick=" + user.Nick +
            " toPawn=" + (pawn != null
                ? VectorUtils.Distance2D(user.transform.position, pawn.transform.position).ToString("F2")
                : "-") +
            " running=" + (user.Running ? 1 : 0) +
            " loc=" + Vec(user.transform.position) +
            " " + EntityActionCombatLog.AnimDump(user) +
            EntityActionCombatLog.ChaseDump(user, pawn));
    }

    public static void LogAttack(Entity entity, Entity target)
    {
        UserEntity user = entity as UserEntity;
        if (user == null)
            return;

        LogStop(user, target, "Atk");
        Debug.Log(Tag + " ATK nick=" + user.Nick +
            " sinceDist=" + PacketLatencyLog.DistAgeMs() + "ms" +
            " toPawn=" + (target != null
                ? VectorUtils.Distance2D(user.transform.position, target.transform.position).ToString("F2")
                : "-") +
            " running=" + (user.Running ? 1 : 0) +
            " loc=" + Vec(user.transform.position) +
            " " + EntityActionCombatLog.AnimDump(user) +
            EntityActionCombatLog.ChaseDump(user, target));
    }

   

    public static void LogTripEnd(MovementData data, string reason)
    {
        if (data == null)
            return;
        UserEntity user = data.GetEntity() as UserEntity;
        MonsterEntity monster = data.GetEntity() as MonsterEntity;
        if (user == null && !IsKeltir(monster))
            return;

        Entity entity = user != null ? (Entity)user : monster;
        string nick = user != null ? user.Nick : NpcNick(monster);

        float now = Time.time;
        float tripSec = data.SpeedTripSec(now);
        float tripDist = data.SpeedTripDist2d();
        float avg = tripSec > 0.05f ? tripDist / tripSec : 0f;
        Stats s = entity.Stats;
        float used = entity.Running
            ? (s != null ? s.UnitySpeedRun : 0f)
            : (s != null ? s.UnitySpeedWalking : 0f);
        float runReal = s != null ? s.RunRealSpeed : 0f;

        Debug.Log(Tag + " TRIP " + reason +
            " nick=" + nick +
            " tripSec=" + tripSec.ToString("F2") +
            " tripDist=" + tripDist.ToString("F2") +
            " tripAvgMs=" + avg.ToString("F4") +
            " usedMs=" + used.ToString("F4") +
            " l2RunMs=" + (runReal * L2UuToM).ToString("F4") +
            " running=" + (entity.Running ? 1 : 0) +
            " loc=" + Vec(entity.transform.position) +
            " " + ScaleDump(entity, avg) +
            " " + EntityActionCombatLog.AnimDump(entity));
    }

    static string ScaleDump(Entity entity, float metersPerSec)
    {
        CharacterController cc = entity != null ? entity.GetComponent<CharacterController>() : null;
        float ccH = cc != null ? cc.height : 0f;
        float ccR = cc != null ? cc.radius : 0f;
        float pktCh = 0f;
        if (entity != null && entity.Appearance != null)
            pktCh = entity.Appearance.CollisionHeight;
        float chM = L2NameplateAnchor.CollisionHeightToUnityMeters(pktCh);
        float l2CapsuleH = chM * 2f;
        float bodyPerSec = ccH > 0.05f ? metersPerSec / ccH : 0f;
        float l2BodyPerSec = l2CapsuleH > 0.05f ? metersPerSec / l2CapsuleH : 0f;
        return "ccH=" + ccH.ToString("F2") +
            " ccR=" + ccR.ToString("F2") +
            " pktCH=" + pktCh.ToString("F2") +
            " l2CapsuleH=" + l2CapsuleH.ToString("F2") +
            " bodyPerSec=" + bodyPerSec.ToString("F2") +
            " vsL2Capsule=" + l2BodyPerSec.ToString("F2");
    }

    static string GaitOf(string anim, bool running)
    {
        string n = anim != null ? anim.ToLowerInvariant() : "";
        if (n.IndexOf("walk") >= 0)
            return "walk";
        if (n.IndexOf("run") >= 0)
            return "run";
        if (n.IndexOf("wait") >= 0 || n.IndexOf("atk") >= 0)
            return n.IndexOf("atk") >= 0 ? "atk" : "wait";
        return running ? "run?" : "walk?";
    }

    static string AnimName(Entity entity)
    {
        if (entity == null)
            return "-";
        NetworkAnimationController nac = entity.GetAnimatorController();
        Animator animator = nac != null ? nac.GetAnimator() : null;
        if (animator == null)
            return "-";
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips != null && clips.Length > 0 && clips[0].clip != null)
            return clips[0].clip.name;
        return "?";
    }

    static bool IsKeltir(Entity entity)
    {
        if (entity == null)
            return false;
        if (entity.Identity != null && entity.Identity.NpcId == 20544)
            return true;
        string n = NpcNick(entity);
        if (n.IndexOf("Keltir", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        string cls = entity.Identity != null ? entity.Identity.NpcClass : null;
        return !string.IsNullOrEmpty(cls) &&
            cls.IndexOf("keltir", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static string NpcNick(Entity entity)
    {
        if (entity == null)
            return "-";
        if (entity.Identity != null && !string.IsNullOrEmpty(entity.Identity.Name))
            return entity.Identity.Name;
        return entity.name;
    }

    public static void LogNpcPacket(Entity npc, string where)
    {
        if (!IsKeltir(npc))
            return;

        Stats s = npc.Stats;
        float runReal = s != null ? s.RunRealSpeed : 0f;
        float walkReal = s != null ? s.WalkRealSpeed : 0f;
        int runBase = s != null ? s.BaseRunSpeed : 0;
        int walkBase = s != null ? s.BaseWalkingSpeed : 0;
        float moveMul = runBase > 0 ? runReal / runBase : 0f;
        Debug.Log(Tag + " PKT " + where +
            " nick=" + NpcNick(npc) +
            " id=" + (npc.Identity != null ? npc.Identity.Id : 0) +
            " npcId=" + (npc.Identity != null ? npc.Identity.NpcId : 0) +
            " npcClass=" + (npc.Identity != null ? npc.Identity.NpcClass : "-") +
            " runBase=" + runBase +
            " walkBase=" + walkBase +
            " moveMul=" + moveMul.ToString("F5") +
            " runReal=" + runReal.ToString("F3") +
            " walkReal=" + walkReal.ToString("F3") +
            " groundFromMul=" + (runBase * moveMul).ToString("F3") +
            " unityRun=" + (s != null ? s.UnitySpeedRun.ToString("F4") : "-") +
            " unityWalk=" + (s != null ? s.UnitySpeedWalking.ToString("F4") : "-") +
            " l2RunMs=" + (runReal * L2UuToM).ToString("F4") +
            " scale0189=" + NumberUtils.ScaleToUnity(runReal).ToString("F4") +
            " running=" + (npc.Running ? 1 : 0) +
            " identRun=" + (npc.Identity != null && npc.Identity.IsRunning ? 1 : 0) +
            " loc=" + Vec(npc.transform.position) +
            " " + EntityActionCombatLog.AnimDump(npc));
    }

    static string SpeedMismatch(
        bool running, bool identRun, bool appearRun, bool tgtRun, string gait,
        float impliedMs, float expectMs)
    {
        string extra = "";
        if (running != identRun)
            extra += " FLAG_IDENT";
        if (running != appearRun)
            extra += " FLAG_APPEAR";
        if (running != tgtRun)
            extra += " FLAG_TGT";
        if (running && gait == "walk")
            extra += " ANIM_WALK_WHILE_RUNSPD";
        if (!running && gait == "run")
            extra += " ANIM_RUN_WHILE_WALKSPD";
        if (expectMs > 0.05f && impliedMs > expectMs * 1.15f)
            extra += " FAST";
        if (expectMs > 0.05f && impliedMs < expectMs * 0.50f && impliedMs < 0.20f)
            extra += " STILL";
        return extra;
    }

    static string Vec(Vector3 v)
    {
        return "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";
    }
}
