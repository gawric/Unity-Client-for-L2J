using System;
using System.Collections.Generic;

public sealed class PlayerParty
{
    readonly Dictionary<int, PartyMemberInfo> _members = new Dictionary<int, PartyMemberInfo>();

    public int LeaderId { get; private set; }
    public int DistributionType { get; private set; }
    public bool IsInParty => LeaderId > 0;
    public IReadOnlyCollection<PartyMemberInfo> Members => _members.Values;
    public event Action Changed;

    public void Replace(int leaderId, int distributionType, IReadOnlyList<PartyMemberInfo> members)
    {
        if (members.Count == 0)
        {
            Clear();
            return;
        }
        _members.Clear();
        LeaderId = leaderId;
        DistributionType = distributionType;
        foreach (PartyMemberInfo member in members)
            _members[member.ObjectId] = member;
        Changed?.Invoke();
    }

    public void Add(int leaderId, int distributionType, PartyMemberInfo member)
    {
        LeaderId = leaderId;
        DistributionType = distributionType;
        _members[member.ObjectId] = member;
        Changed?.Invoke();
    }

    public void Update(PartyMemberInfo member)
    {
        if (!_members.ContainsKey(member.ObjectId))
            return;
        _members[member.ObjectId] = member;
        Changed?.Invoke();
    }

    public void Remove(int objectId)
    {
        if (!_members.Remove(objectId))
            return;
        if (_members.Count == 0)
            Clear();
        else
            Changed?.Invoke();
    }

    public void Clear()
    {
        LeaderId = 0;
        DistributionType = 0;
        _members.Clear();
        Changed?.Invoke();
    }
}
