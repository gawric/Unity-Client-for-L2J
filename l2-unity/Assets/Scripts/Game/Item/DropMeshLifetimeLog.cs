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
        // if (!DropMeshLifetimeLog.Enabled || _nextHeartbeatIndex >= HeartbeatSeconds.Length)
            return;

        float age = Time.time - _spawnTime;
        float target = HeartbeatSeconds[_nextHeartbeatIndex];
        if (age < target)
            return;

        // DropMeshLifetimeLog.Event(
            // "HEARTBEAT",
            // $"itemObj={_itemObjectId} itemId={_itemId} prefab='{_prefabName}' coinVisual={_coinVisual} " +
            // $"age={age:F3}s milestone={target:F0}s " +
            // $"activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy} " +
            // $"worldPos={transform.position:F3} renderers={CountEnabledRenderers()} " +
            // "note=DropMesh_still_alive_after_FX_milestones");

        _nextHeartbeatIndex++;
    }

    void OnDestroy()
    {
        // if (!DropMeshLifetimeLog.Enabled)
            return;

        float age = _spawnTime > 0f ? Time.time - _spawnTime : -1f;
        // string reason = DropMeshLifetimeLog.ConsumeRemoveReason(_itemObjectId);
        // DropMeshLifetimeLog.Event(
            // "DESTROY",
            // $"itemObj={_itemObjectId} itemId={_itemId} prefab='{_prefabName}' coinVisual={_coinVisual} " +
            // $"wallAge={age:F3}s reason={reason} meshId={GetInstanceID()}",
            // includeStack: reason == "unknown");
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
