#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Tracks persistent ground DropMesh (L2 AL2Pickup drop_mesh), not e_u056_a FX.
/// Off by default — set Enabled to debug leftover meshes.
/// Console filter: [DropMeshLifetime]
/// </summary>
public static class DropMeshLifetimeLog
{
    public const string UnityLogPath =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\Debug\Unity_DropMesh.log";

    public static bool Enabled = false;

    static readonly object WriteLock = new object();
    static readonly Dictionary<int, string> PendingRemoveReason = new Dictionary<int, string>();
    static bool _fileStarted;

    public static void NotifyRemove(int itemObjectId, string reason)
    {
        if (!Enabled || itemObjectId == 0)
            return;

        PendingRemoveReason[itemObjectId] = reason ?? "unknown";
    }

    public static void OnAttached(ItemEntity entity, GameObject dropMesh, GameObject prefab, bool coinVisual)
    {
        if (!Enabled || entity == null || dropMesh == null)
            return;

        int objectId = entity.Identity != null ? entity.Identity.Id : 0;
        PendingRemoveReason.Remove(objectId);

        var tracker = dropMesh.GetComponent<DropMeshLifetimeTracker>();
        if (tracker == null)
            tracker = dropMesh.AddComponent<DropMeshLifetimeTracker>();

        tracker.Configure(objectId, entity.ItemId, prefab != null ? prefab.name : "null", coinVisual);

        Event(
            "ATTACH",
            $"itemObj={objectId} itemId={entity.ItemId} entity='{entity.name}' " +
            $"prefab='{(prefab != null ? prefab.name : "null")}' coinVisual={coinVisual} " +
            $"worldPos={dropMesh.transform.position:F3} activeSelf={dropMesh.activeSelf} " +
            $"meshId={dropMesh.GetInstanceID()}");
    }

    public static void OnNoVisual(int itemId, ItemEntity entity, string dropModel, string equipModel)
    {
        if (!Enabled || entity == null)
            return;

        int objectId = entity.Identity != null ? entity.Identity.Id : 0;
        Event(
            "NO_VISUAL",
            $"itemObj={objectId} itemId={itemId} entity='{entity.name}' " +
            $"dropModel='{dropModel ?? "null"}' equipModel='{equipModel ?? "null"}' " +
            "branch=DropMesh_skipped");
    }

    internal static void Event(string action, string detail, bool includeStack = false)
    {
        if (!Enabled)
            return;

        string stack = includeStack ? "\nstack=" + Environment.StackTrace : string.Empty;
        string line =
            $"[DropMeshLifetime] {action} now={Time.time:F3}s frame={Time.frameCount} {detail}{stack}";

        Debug.Log(line);
        Append(line);
    }

    internal static string ConsumeRemoveReason(int itemObjectId)
    {
        if (itemObjectId == 0)
            return "unknown";

        if (PendingRemoveReason.TryGetValue(itemObjectId, out string reason))
        {
            PendingRemoveReason.Remove(itemObjectId);
            return reason;
        }

        return "unknown";
    }

    static void Append(string line)
    {
        lock (WriteLock)
        {
            try
            {
                string dir = Path.GetDirectoryName(UnityLogPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                if (!_fileStarted && File.Exists(UnityLogPath))
                    File.Delete(UnityLogPath);

                _fileStarted = true;
                File.AppendAllText(UnityLogPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DropMeshLifetime] file write failed: " + ex.Message);
            }
        }
    }
}

/// <summary>Heartbeat on DropMesh until GameObject is destroyed.</summary>
public sealed class DropMeshLifetimeTracker : MonoBehaviour
{
    static readonly float[] HeartbeatSeconds = { 1f, 8f, 18f };

    int _itemObjectId;
    int _itemId;
    string _prefabName = "null";
    bool _coinVisual;
    float _spawnTime;
    int _nextHeartbeatIndex;

    public void Configure(int itemObjectId, int itemId, string prefabName, bool coinVisual)
    {
        _itemObjectId = itemObjectId;
        _itemId = itemId;
        _prefabName = prefabName ?? "null";
        _coinVisual = coinVisual;
        _spawnTime = Time.time;
        _nextHeartbeatIndex = 0;
    }

    void Update()
    {
        if (!DropMeshLifetimeLog.Enabled || _nextHeartbeatIndex >= HeartbeatSeconds.Length)
            return;

        float age = Time.time - _spawnTime;
        float target = HeartbeatSeconds[_nextHeartbeatIndex];
        if (age < target)
            return;

        DropMeshLifetimeLog.Event(
            "HEARTBEAT",
            $"itemObj={_itemObjectId} itemId={_itemId} prefab='{_prefabName}' coinVisual={_coinVisual} " +
            $"age={age:F3}s milestone={target:F0}s " +
            $"activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy} " +
            $"worldPos={transform.position:F3} renderers={CountEnabledRenderers()} " +
            "note=DropMesh_still_alive_after_FX_milestones");

        _nextHeartbeatIndex++;
    }

    void OnDestroy()
    {
        if (!DropMeshLifetimeLog.Enabled)
            return;

        float age = _spawnTime > 0f ? Time.time - _spawnTime : -1f;
        string reason = DropMeshLifetimeLog.ConsumeRemoveReason(_itemObjectId);
        DropMeshLifetimeLog.Event(
            "DESTROY",
            $"itemObj={_itemObjectId} itemId={_itemId} prefab='{_prefabName}' coinVisual={_coinVisual} " +
            $"wallAge={age:F3}s reason={reason} meshId={GetInstanceID()}",
            includeStack: reason == "unknown");
    }

    int CountEnabledRenderers()
    {
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].enabled && renderers[i].gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }
}
#endif
