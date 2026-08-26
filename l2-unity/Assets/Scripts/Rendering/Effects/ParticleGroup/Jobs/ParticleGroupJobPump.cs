using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Completes ParticleGroup expire jobs after other FixedUpdates so slot math
/// overlaps gameplay scripts on worker threads.
/// </summary>
[DefaultExecutionOrder(10000)]
public sealed class ParticleGroupJobPump : MonoBehaviour
{
    struct QueuedGroup
    {
        public ParticleGroup group;
        public float now;
    }

    static ParticleGroupJobPump _instance;
    static readonly List<QueuedGroup> Queued = new List<QueuedGroup>(64);

    public static void Enqueue(ParticleGroup group, float now)
    {
        if (group == null)
            return;

        Ensure();
        for (int i = 0; i < Queued.Count; i++)
        {
            if (Queued[i].group != group)
                continue;
            Queued[i] = new QueuedGroup { group = group, now = now };
            return;
        }

        Queued.Add(new QueuedGroup { group = group, now = now });
    }

    public static void Remove(ParticleGroup group)
    {
        if (group == null || Queued.Count == 0)
            return;

        for (int i = Queued.Count - 1; i >= 0; i--)
        {
            if (Queued[i].group == group)
                Queued.RemoveAt(i);
        }
    }

    static void Ensure()
    {
        if (_instance != null)
            return;

        var go = new GameObject(nameof(ParticleGroupJobPump));
        DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        _instance = go.AddComponent<ParticleGroupJobPump>();
    }

    void FixedUpdate()
    {
        int count = Queued.Count;
        if (count == 0)
            return;

        JobHandle combined = default;
        bool any = false;
        for (int i = 0; i < count; i++)
        {
            ParticleGroup group = Queued[i].group;
            if (group == null)
                continue;

            JobHandle handle = group.ConsumeExpireHandle();
            combined = any ? JobHandle.CombineDependencies(combined, handle) : handle;
            any = true;
        }

        combined.Complete();

        for (int i = 0; i < count; i++)
        {
            ParticleGroup group = Queued[i].group;
            if (group == null)
                continue;

            group.ApplyExpireAndSpawn(Queued[i].now);
        }

        Queued.Clear();
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        Queued.Clear();
    }
}
