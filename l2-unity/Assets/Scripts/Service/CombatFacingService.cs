using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// Continuous yaw-follow toward a live target during bow draw / magic cast.
/// Does not move actors — approach stays server <c>MoveToPawnDto</c> only.
/// Entries keyed by objectId (list-ready for other players later; today only local player).
/// Local player pauses while <see cref="PlayerController.RunningToDestination"/>.
/// </summary>
public sealed class CombatFacingService : MonoBehaviour
{
    private const string LogPrefix = "[CombatFacing]";
    private const float TurnLogIntervalSeconds = 0.35f;

    private struct FacingEntry
    {
        public Transform Actor;
        public Transform Target;
        public float LastTurnLogTime;
    }

    public static CombatFacingService Instance { get; private set; }

    [Inject] WeapongrpTable _weaponGrps;

    private static WeapongrpTable Weapons
    {
        get
        {
            if (Instance != null && Instance._weaponGrps != null)
                return Instance._weaponGrps;
            return WeapongrpTable.Instance;
        }
    }


    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _angleThreshold = 3f;

    private readonly Dictionary<int, FacingEntry> _entries = new Dictionary<int, FacingEntry>(8);
    private readonly List<int> _iterateIds = new List<int>(8);
    private readonly List<int> _removeIds = new List<int>(8);

    public int ActiveCount => _entries.Count;
    public bool IsActive => _entries.Count > 0;

    public static CombatFacingService Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        if (App.GameContainer != null)
        {
            try
            {
                CombatFacingService resolved = App.GameContainer.Resolve<CombatFacingService>();
                if (resolved != null)
                    return resolved;
            }
            catch
            {
            }
        }

        GameObject go = new GameObject(nameof(CombatFacingService));
        CombatFacingService created = go.AddComponent<CombatFacingService>();
        App.InjectGameObject(go);
        return created;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool HasFollow(int objectId) => _entries.ContainsKey(objectId);

    /// <summary>
    /// Start / replace tracking for <paramref name="objectId"/> until <see cref="EndFollow(int)"/>.
    /// </summary>
    public void BeginFollow(int objectId, Transform actor, Transform target)
    {
        if (objectId == 0 || actor == null || target == null || actor == target)
        {
            Debug.Log(
                $"{LogPrefix} Begin REJECTED objectId={objectId} " +
                $"actor={(actor != null ? actor.name : "null")} " +
                $"target={(target != null ? target.name : "null")} count={_entries.Count}");
            EndFollow(objectId, "begin-rejected");
            return;
        }

        bool replaced = _entries.ContainsKey(objectId);
        _entries[objectId] = new FacingEntry
        {
            Actor = actor,
            Target = target,
            LastTurnLogTime = 0f
        };

        Debug.Log(
            $"{LogPrefix} Begin ACCEPTED objectId={objectId} " +
            $"actor={actor.name} target={target.name} " +
            $"replaced={replaced} count={_entries.Count}");
    }

    public void EndFollow(int objectId)
    {
        EndFollow(objectId, "explicit");
    }

    public void EndFollow(int objectId, string reason)
    {
        if (objectId == 0)
        {
            return;
        }

        if (!_entries.Remove(objectId))
        {
            return;
        }

        Debug.Log($"{LogPrefix} End REMOVED objectId={objectId} reason={reason} count={_entries.Count}");
    }

    /// <summary>End follow for local player (convenience for current call sites).</summary>
    public void EndFollowLocal(string reason = "local")
    {
        int id = ResolveLocalObjectId();
        if (id != 0)
        {
            EndFollow(id, reason);
        }
    }

    public void EndFollowAll()
    {
        int before = _entries.Count;
        _entries.Clear();
        Debug.Log($"{LogPrefix} End ALL cleared before={before} count=0");
    }

    private void Update()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        int localId = ResolveLocalObjectId();
        PlayerController player = IncomingPacketActions.Player;
        bool localMoving = player != null && player.RunningToDestination;

        // Snapshot keys — ApplyYaw may write entry back; shoot/Exit may EndFollow mid-frame.
        _iterateIds.Clear();
        foreach (int id in _entries.Keys)
        {
            _iterateIds.Add(id);
        }

        _removeIds.Clear();

        for (int i = 0; i < _iterateIds.Count; i++)
        {
            int objectId = _iterateIds[i];
            if (!_entries.TryGetValue(objectId, out FacingEntry entry))
            {
                continue;
            }

            if (entry.Actor == null || entry.Target == null)
            {
                _removeIds.Add(objectId);
                continue;
            }

            // Only local player's move-path owns rotation while running.
            if (objectId == localId && localMoving)
            {
                continue;
            }

            ApplyYaw(objectId, ref entry);
            if (_entries.ContainsKey(objectId))
            {
                _entries[objectId] = entry;
            }
        }

        for (int i = 0; i < _removeIds.Count; i++)
        {
            int rid = _removeIds[i];
            if (_entries.Remove(rid))
            {
                Debug.Log(
                    $"{LogPrefix} End REMOVED objectId={rid} reason=null-actor-or-target count={_entries.Count}");
            }
        }
    }

    private void ApplyYaw(int objectId, ref FacingEntry entry)
    {
        Transform actor = entry.Actor;
        Transform target = entry.Target;

        Vector3 dir = target.position - actor.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion look = Quaternion.LookRotation(dir.normalized);
        float angle = Quaternion.Angle(actor.rotation, look);
        if (angle <= _angleThreshold)
        {
            actor.rotation = look;
            return;
        }

        actor.rotation = Quaternion.Slerp(actor.rotation, look, Time.deltaTime * _speed);

        float now = Time.unscaledTime;
        if (now - entry.LastTurnLogTime >= TurnLogIntervalSeconds)
        {
            entry.LastTurnLogTime = now;
            Debug.Log(
                $"{LogPrefix} Turn objectId={objectId} " +
                $"actor={actor.name} target={target.name} angle={angle:F1} count={_entries.Count}");
        }
    }

    private static int ResolveLocalObjectId()
    {
        if (PlayerEntity.Instance == null ||
            PlayerEntity.Instance.Identity == null)
        {
            return 0;
        }

        return PlayerEntity.Instance.Identity.Id;
    }

    /// <summary>
    /// Equipped bow via appearance hands, fallback anim name contains "bow".
    /// </summary>
    public static bool IsUsingBow(Entity entity)
    {
        if (entity is PlayerEntity player)
            return IsPlayerUsingBow(player);

        if (entity != null && entity.Appearance != null && Weapons != null)
        {
            if (IsBowItemId(entity.Appearance.RHand) || IsBowItemId(entity.Appearance.LHand))
                return true;
        }

        string anim = null;
        UserEntity user = entity as UserEntity;
        if (user != null)
            anim = user.WeaponAnim;
        else if (entity != null && entity.Gear != null)
            anim = entity.Gear.WeaponAnim;

        return !string.IsNullOrEmpty(anim) &&
               anim.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsPlayerUsingBow(PlayerEntity player)
    {
        if (player == null)
        {
            return false;
        }

        if (player.Appearance != null && Weapons != null)
        {
            if (IsBowItemId(player.Appearance.RHand) || IsBowItemId(player.Appearance.LHand))
            {
                return true;
            }
        }

        string anim = player.GetCurrentAnimName();
        return !string.IsNullOrEmpty(anim) &&
               anim.IndexOf("bow", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsBowItemId(int itemId)
    {
        if (itemId == 0)
        {
            return false;
        }

        Weapongrp weapon = Weapons.GetWeapon(itemId);
        return weapon != null && weapon.WeaponType == WeaponType.bow;
    }
}
