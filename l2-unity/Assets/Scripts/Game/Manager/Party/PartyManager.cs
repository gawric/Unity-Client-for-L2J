using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Client-side party roster state, fed by the PartySmallWindow* / PartySpelled packets (there is
/// no local simulation - every field here is a direct mirror of what the server last sent).
/// </summary>
public class PartyManager : UnityEngine.MonoBehaviour
{
    private static PartyManager _instance;
    public static PartyManager Instance => _instance;

    private int _leaderObjectId;
    private PartyDistributionType _distributionType = PartyDistributionType.FindersKeepers;
    private readonly Dictionary<int, PartyMemberData> _members = new Dictionary<int, PartyMemberData>();

    public bool IsInParty => _members.Count > 0;
    public int LeaderObjectId => _leaderObjectId;
    public PartyDistributionType DistributionType => _distributionType;
    public IReadOnlyCollection<PartyMemberData> Members => _members.Values;

    public bool IsLeader => IsInParty && PlayerEntity.Instance != null
        && _leaderObjectId == PlayerEntity.Instance.Identity.Id;

    /// <summary>Roster changed shape (member added/removed, or full reset) - rebuild the member list UI.</summary>
    public event Action OnPartyChanged;
    /// <summary>HP/MP/CP/level/class changed for this member id - refresh just that row.</summary>
    public event Action<int> OnMemberUpdated;
    /// <summary>Buff/debuff list changed for this member id - refresh just that row's buff icons.</summary>
    public event Action<int> OnMemberBuffsUpdated;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            Debug.Log("[PartyDebug] PartyManager.Awake: instance set.");
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    public PartyMemberData GetMember(int objectId)
    {
        return _members.TryGetValue(objectId, out PartyMemberData member) ? member : null;
    }

    public void ApplyAll(PartySmallWindowAllDto packet)
    {
        _leaderObjectId = packet.LeaderObjectId;
        _distributionType = packet.DistributionType;
        _members.Clear();

        foreach (PartyMemberSnapshot snapshot in packet.Members)
        {
            PartyMemberData member = new PartyMemberData();
            member.ApplySnapshot(snapshot);
            _members[member.ObjectId] = member;
        }

        Debug.Log($"[PartyDebug] ApplyAll: leader={_leaderObjectId}, memberCount={_members.Count}, names=[{string.Join(", ", _members.Values.Select(m => m.Name))}], hasSubscribers={OnPartyChanged != null}");
        OnPartyChanged?.Invoke();
    }

    public void ApplyAdd(PartySmallWindowAddDto packet)
    {
        _leaderObjectId = packet.LeaderObjectId;
        _distributionType = packet.DistributionType;

        PartyMemberData member = new PartyMemberData();
        member.ApplySnapshot(packet.Member);
        _members[member.ObjectId] = member;

        Debug.Log($"[PartyDebug] ApplyAdd: leader={_leaderObjectId}, added={member.Name} ({member.ObjectId}), memberCount={_members.Count}, hasSubscribers={OnPartyChanged != null}");
        OnPartyChanged?.Invoke();
    }

    public void ApplyUpdate(PartySmallWindowUpdateDto packet)
    {
        if (!_members.TryGetValue(packet.Member.ObjectId, out PartyMemberData member))
        {
            // An update implies the member should already be known - keep the roster consistent
            // instead of silently dropping the data if it somehow arrives out of order.
            member = new PartyMemberData();
            _members[packet.Member.ObjectId] = member;
        }

        member.ApplySnapshot(packet.Member);
        OnMemberUpdated?.Invoke(member.ObjectId);
    }

    public void ApplyDelete(PartySmallWindowDeleteDto packet)
    {
        if (_members.Remove(packet.ObjectId))
        {
            OnPartyChanged?.Invoke();
        }
    }

    public void ApplyDeleteAll()
    {
        _members.Clear();
        _leaderObjectId = 0;
        OnPartyChanged?.Invoke();
    }

    public void ApplySpelled(PartySpelledDto packet)
    {
        // Only player members have a roster row today - pets/servitors (CreatureType 1/2) aren't
        // tracked here yet.
        if (packet.CreatureType != 0)
        {
            return;
        }

        if (_members.TryGetValue(packet.ObjectId, out PartyMemberData member))
        {
            member.Buffs.Clear();
            member.Buffs.AddRange(packet.Effects);
            OnMemberBuffsUpdated?.Invoke(member.ObjectId);
        }
    }
}
