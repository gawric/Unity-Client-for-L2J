using System.Collections.Generic;
using UnityEngine;

public static class MagicShotCoordinator
{
    private struct ShotSession
    {
        public int CastStartMs;
        public float LastUpdatedTime;
    }

    private static readonly object Sync = new object();
    private static readonly Dictionary<int, ShotSession> SessionsByObjectId = new Dictionary<int, ShotSession>();
    private const float StaleSessionLifetimeSeconds = 30f;

    public static bool TryStartShot(int objectId, MagicCastData castData, string source, out string message)
    {
        if (objectId <= 0)
        {
            message = $"[MagicShotCoordinator] ALLOW source={source} objectId={objectId} reason=invalidObjectId";
            return true;
        }

        if (castData == null)
        {
            message = $"[MagicShotCoordinator] ALLOW source={source} objectId={objectId} reason=castDataNull";
            return true;
        }

        int castStartMs = Mathf.RoundToInt(castData.StartTime * 1000f);
        float now = Time.time;

        lock (Sync)
        {
            CleanupStaleSessions(now);

            if (SessionsByObjectId.TryGetValue(objectId, out ShotSession existingSession))
            {
                if (existingSession.CastStartMs == castStartMs)
                {
                    message =
                        $"[MagicShotCoordinator] BLOCK source={source} objectId={objectId} castStartMs={castStartMs} reason=alreadyStarted";
                    return false;
                }
            }

            SessionsByObjectId[objectId] = new ShotSession
            {
                CastStartMs = castStartMs,
                LastUpdatedTime = now
            };
        }

        message = $"[MagicShotCoordinator] ALLOW source={source} objectId={objectId} castStartMs={castStartMs}";
        return true;
    }

    public static void CompleteCast(int objectId, MagicCastData castData)
    {
        if (objectId <= 0 || castData == null)
        {
            return;
        }

        int castStartMs = Mathf.RoundToInt(castData.StartTime * 1000f);
        lock (Sync)
        {
            if (SessionsByObjectId.TryGetValue(objectId, out ShotSession existingSession) &&
                existingSession.CastStartMs == castStartMs)
            {
                SessionsByObjectId.Remove(objectId);
            }
        }
    }

    private static void CleanupStaleSessions(float now)
    {
        if (SessionsByObjectId.Count == 0)
        {
            return;
        }

        List<int> staleObjectIds = null;
        foreach (KeyValuePair<int, ShotSession> pair in SessionsByObjectId)
        {
            if (now - pair.Value.LastUpdatedTime > StaleSessionLifetimeSeconds)
            {
                if (staleObjectIds == null)
                {
                    staleObjectIds = new List<int>();
                }

                staleObjectIds.Add(pair.Key);
            }
        }

        if (staleObjectIds == null)
        {
            return;
        }

        for (int i = 0; i < staleObjectIds.Count; i++)
        {
            SessionsByObjectId.Remove(staleObjectIds[i]);
        }
    }
}
