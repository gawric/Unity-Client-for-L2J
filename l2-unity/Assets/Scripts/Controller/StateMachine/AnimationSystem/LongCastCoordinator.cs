using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Связывает фазу loop (CastEndLong) между SkillAnimationRunner и PlayerOverriddenLongAtk.
/// </summary>
public static class LongCastCoordinator
{
    private static readonly object Sync = new object();
    private static readonly Dictionary<int, TaskCompletionSource<bool>> LoopPhaseByObjectId = new Dictionary<int, TaskCompletionSource<bool>>();

    public static TaskCompletionSource<bool> RegisterLoopPhase(int objectId)
    {
        var tcs = new TaskCompletionSource<bool>();
        lock (Sync)
        {
            if (LoopPhaseByObjectId.TryGetValue(objectId, out TaskCompletionSource<bool> oldTcs))
            {
                oldTcs.TrySetResult(false);
            }

            LoopPhaseByObjectId[objectId] = tcs;
        }

        return tcs;
    }

    public static void CompleteLoopPhase(int objectId)
    {
        lock (Sync)
        {
            if (!LoopPhaseByObjectId.TryGetValue(objectId, out TaskCompletionSource<bool> tcs))
            {
                return;
            }

            LoopPhaseByObjectId.Remove(objectId);
            tcs.TrySetResult(true);
        }
    }

    public static void CancelLoopPhase(int objectId)
    {
        lock (Sync)
        {
            if (!LoopPhaseByObjectId.TryGetValue(objectId, out TaskCompletionSource<bool> tcs))
            {
                return;
            }

            LoopPhaseByObjectId.Remove(objectId);
            tcs.TrySetResult(false);
        }
    }
}
