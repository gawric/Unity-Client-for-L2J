using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compare Unity CharInfo (UserEntity) travel vs aCis PlayerMove budget.
/// aCis: scheduleAtFixedRate(100,100), passed = getRealMoveSpeed(stamp&lt;=5)*dt/1000,
/// arrive when 2D radius to pawn &lt;= Dist. First 5 ticks use walk-start speed.
/// </summary>
public static class CharInfoMoveBudgetLog
{
    const string Tag = "[CI_MOVE]";
    const float L2UuToM = 1f / 52.5f;

    class Trip
    {
        public int Id;
        public string Nick;
        public string Kind;
        public Vector3 Start;
        public Vector3 PktOrigin;
        public float StartTime;
        public float StartToPawn;
        public float Dist;
        public float RunUu;
        public float WalkUu;
        public float UnityRun;
        public float UnityWalk;
        public float Path2d;
        public Vector3 LastPos;
    }

    static readonly Dictionary<int, Trip> Trips = new Dictionary<int, Trip>();

    public static void StartPawn(UserEntity user, Entity pawn, MoveToPawnDto dto, float stopDist)
    {
        if (user == null || user.Identity == null)
            return;

        Stats s = user.Stats;
        float runUu = s != null ? s.RunRealSpeed : 0f;
        float walkUu = s != null ? s.WalkRealSpeed : 0f;
        Trip trip = new Trip
        {
            Id = user.Identity.Id,
            Nick = user.Nick,
            Kind = "Pawn",
            Start = user.transform.position,
            PktOrigin = dto != null ? dto.ObjPos : user.transform.position,
            StartTime = Time.time,
            StartToPawn = pawn != null
                ? VectorUtils.Distance2D(user.transform.position, pawn.transform.position)
                : 0f,
            Dist = stopDist,
            RunUu = runUu,
            WalkUu = walkUu,
            UnityRun = s != null ? s.UnitySpeedRun : 0f,
            UnityWalk = s != null ? s.UnitySpeedWalking : 0f,
            Path2d = 0f,
            LastPos = user.transform.position
        };
        Trips[trip.Id] = trip;

        float startUu = trip.StartToPawn / L2UuToM;
        float distUu = trip.Dist / L2UuToM;
        float needUu = Mathf.Max(0f, startUu - distUu);
        float budgetMs = AcisBudgetMs(needUu, walkUu, runUu, user.Running);

        Debug.Log(Tag + " START kind=Pawn nick=" + trip.Nick +
            " id=" + trip.Id +
            " pawn=" + EntityActionCombatLog.NameOf(pawn) +
            " start=" + Vec(trip.Start) +
            " pktOrigin=" + Vec(trip.PktOrigin) +
            " originDelta=" + VectorUtils.Distance2D(trip.Start, trip.PktOrigin).ToString("F3") +
            " startToPawn=" + trip.StartToPawn.ToString("F3") +
            " startToPawnUu=" + startUu.ToString("F1") +
            " Dist=" + trip.Dist.ToString("F3") +
            " DistUu=" + distUu.ToString("F1") +
            " need=" + (trip.StartToPawn - trip.Dist).ToString("F3") +
            " needUu=" + needUu.ToString("F1") +
            " runUu=" + runUu.ToString("F3") +
            " walkStartUu=" + walkUu.ToString("F3") +
            " unityRun=" + trip.UnityRun.ToString("F4") +
            " unityWalk=" + trip.UnityWalk.ToString("F4") +
            " running=" + (user.Running ? 1 : 0) +
            " acisBudgetMs=" + budgetMs.ToString("F0") +
            " formula=delay100ms + stamp1-5 walkStart then run; arrive=isIn2DRadius(pawn,Dist)");
    }

    public static void StartPoint(UserEntity user, Vector3 dest)
    {
        if (user == null || user.Identity == null)
            return;

        Stats s = user.Stats;
        float runUu = s != null ? s.RunRealSpeed : 0f;
        float walkUu = s != null ? s.WalkRealSpeed : 0f;
        float dist2d = VectorUtils.Distance2D(user.transform.position, dest);
        Trip trip = new Trip
        {
            Id = user.Identity.Id,
            Nick = user.Nick,
            Kind = "Point",
            Start = user.transform.position,
            PktOrigin = user.transform.position,
            StartTime = Time.time,
            StartToPawn = dist2d,
            Dist = 0.1f,
            RunUu = runUu,
            WalkUu = walkUu,
            UnityRun = s != null ? s.UnitySpeedRun : 0f,
            UnityWalk = s != null ? s.UnitySpeedWalking : 0f,
            Path2d = 0f,
            LastPos = user.transform.position
        };
        Trips[trip.Id] = trip;

        float needUu = dist2d / L2UuToM;
        float budgetMs = AcisBudgetMs(needUu, walkUu, runUu, user.Running);
        Debug.Log(Tag + " START kind=Point nick=" + trip.Nick +
            " id=" + trip.Id +
            " start=" + Vec(trip.Start) +
            " dest=" + Vec(dest) +
            " dist2d=" + dist2d.ToString("F3") +
            " distUu=" + needUu.ToString("F1") +
            " runUu=" + runUu.ToString("F3") +
            " walkStartUu=" + walkUu.ToString("F3") +
            " unityRun=" + trip.UnityRun.ToString("F4") +
            " running=" + (user.Running ? 1 : 0) +
            " acisBudgetMs=" + budgetMs.ToString("F0"));
    }

    public static void Accum(UserEntity user, float dist2d, Vector3 pos)
    {
        Trip trip;
        if (!TryTrip(user, out trip))
            return;
        trip.Path2d += dist2d;
        trip.LastPos = pos;
    }

    public static void Compare(Entity entity, string reason, Entity pawn, Vector3 packetPos, bool hasPacket)
    {
        UserEntity user = entity as UserEntity;
        if (user == null || user.Identity == null)
            return;

        Trip trip;
        if (!TryTrip(user, out trip))
        {
            if (hasPacket)
            {
                float snap = VectorUtils.Distance2D(user.transform.position, packetPos);
                if (snap >= 0.02f)
                    Debug.Log(Tag + " " + reason + " nick=" + user.Nick +
                        " id=" + user.Identity.Id +
                        " noTrip snap2d=" + snap.ToString("F3") +
                        " now=" + Vec(user.transform.position) +
                        " packet=" + Vec(packetPos));
            }
            return;
        }

        float elapsedMs = (Time.time - trip.StartTime) * 1000f;
        Vector3 now = user.transform.position;
        float disp = VectorUtils.Distance2D(trip.Start, now);
        float dispUu = disp / L2UuToM;
        float pathUu = trip.Path2d / L2UuToM;
        float nowToPawn = pawn != null
            ? VectorUtils.Distance2D(now, pawn.transform.position)
            : -1f;
        float pastDist = trip.Dist > 0.01f && nowToPawn >= 0f
            ? trip.Dist - nowToPawn
            : 0f;
        float needM = Mathf.Max(0f, trip.StartToPawn - trip.Dist);
        float needUu = needM / L2UuToM;
        float acisExpectUu = Mathf.Min(needUu + 1f, AcisExpectUu(elapsedMs, trip.WalkUu, trip.RunUu, user.Running));
        float acisExpectM = acisExpectUu * L2UuToM;
        float overshootM = disp - acisExpectM;
        float overshootNeed = disp - needM;
        float pktDelta = hasPacket ? VectorUtils.Distance2D(now, packetPos) : -1f;
        float pktToPawn = hasPacket && pawn != null
            ? VectorUtils.Distance2D(packetPos, pawn.transform.position)
            : -1f;

        Debug.Log(Tag + " " + reason +
            " nick=" + trip.Nick +
            " id=" + trip.Id +
            " kind=" + trip.Kind +
            " elapsedMs=" + elapsedMs.ToString("F0") +
            " unityDisp=" + disp.ToString("F3") +
            " unityDispUu=" + dispUu.ToString("F1") +
            " unityPath=" + trip.Path2d.ToString("F3") +
            " unityPathUu=" + pathUu.ToString("F1") +
            " nowToPawn=" + nowToPawn.ToString("F3") +
            " Dist=" + trip.Dist.ToString("F3") +
            " pastDist=" + pastDist.ToString("F3") +
            " need=" + needM.ToString("F3") +
            " acisExpect=" + acisExpectM.ToString("F3") +
            " acisExpectUu=" + acisExpectUu.ToString("F1") +
            " overshootVsAcis=" + overshootM.ToString("F3") +
            " overshootVsNeed=" + overshootNeed.ToString("F3") +
            " pktDelta=" + pktDelta.ToString("F3") +
            " pktToPawn=" + pktToPawn.ToString("F3") +
            " now=" + Vec(now) +
            (hasPacket ? " packet=" + Vec(packetPos) : "") +
            " start=" + Vec(trip.Start) +
            " runUu=" + trip.RunUu.ToString("F2") +
            " walkStartUu=" + trip.WalkUu.ToString("F2") +
            " unityRun=" + trip.UnityRun.ToString("F4"));
    }

    static bool TryTrip(UserEntity user, out Trip trip)
    {
        trip = null;
        if (user == null || user.Identity == null)
            return false;
        return Trips.TryGetValue(user.Identity.Id, out trip);
    }

    /// <summary>
    /// aCis PlayerMove: first tick after 100ms, stamps 1-5 use walk-start speed.
    /// </summary>
    public static float AcisExpectUu(float elapsedMs, float walkUuS, float runUuS, bool running)
    {
        float t = elapsedMs - 100f;
        if (t <= 0f)
            return 0f;
        float walkPhaseMs = Mathf.Min(500f, t);
        float runPhaseMs = Mathf.Max(0f, t - 500f);
        float walkSpd = walkUuS;
        float runSpd = running ? runUuS : walkUuS;
        return walkSpd * (walkPhaseMs / 1000f) + runSpd * (runPhaseMs / 1000f);
    }

    public static float AcisBudgetMs(float needUu, float walkUuS, float runUuS, bool running)
    {
        float walkSpd = walkUuS;
        float runSpd = running ? runUuS : walkUuS;
        float ms = 100f;
        float walkBudgetUu = walkSpd * 0.5f;
        if (needUu <= walkBudgetUu)
            return ms + (walkSpd > 0.01f ? needUu / walkSpd * 1000f : 0f);
        return ms + 500f + (runSpd > 0.01f ? (needUu - walkBudgetUu) / runSpd * 1000f : 0f);
    }

    static string Vec(Vector3 v)
    {
        return "(" + v.x.ToString("F2") + "," + v.y.ToString("F2") + "," + v.z.ToString("F2") + ")";
    }
}
