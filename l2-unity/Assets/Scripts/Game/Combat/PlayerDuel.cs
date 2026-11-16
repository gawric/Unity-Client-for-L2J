using System;
using System.Collections.Generic;

public enum DuelPhase { None, Invitation, AwaitingStart, Ready, Active }

public sealed class DuelFighterInfo
{
    public string Name;
    public int ObjectId;
    public int ClassId;
    public int Level;
    public int Hp;
    public int MaxHp;
    public int Mp;
    public int MaxMp;
    public int Cp;
    public int MaxCp;
}

public sealed class PlayerDuel
{
    readonly Dictionary<int, DuelFighterInfo> _fighters = new Dictionary<int, DuelFighterInfo>();
    public DuelPhase Phase { get; private set; }
    public bool IsParty { get; private set; }
    public string OpponentName { get; private set; }
    public IReadOnlyCollection<DuelFighterInfo> Fighters => _fighters.Values;
    public event Action Changed;

    public void Challenge(string name, bool party)
    {
        OpponentName = name;
        IsParty = party;
    }

    public void Invite(string name, bool party)
    {
        OpponentName = name;
        IsParty = party;
        Phase = DuelPhase.Invitation;
        _fighters.Clear();
        Changed?.Invoke();
    }

    public void AwaitStart()
    {
        Phase = DuelPhase.AwaitingStart;
        Changed?.Invoke();
    }

    public void Ready(bool party)
    {
        IsParty = party;
        Phase = DuelPhase.Ready;
        _fighters.Clear();
        Changed?.Invoke();
    }

    public void Start(bool party)
    {
        IsParty = party;
        Phase = DuelPhase.Active;
        Changed?.Invoke();
    }

    public void Update(DuelFighterInfo fighter)
    {
        if (Phase != DuelPhase.Active && Phase != DuelPhase.Ready)
            return;
        _fighters[fighter.ObjectId] = fighter;
        if (string.IsNullOrEmpty(OpponentName))
            OpponentName = fighter.Name;
        Changed?.Invoke();
    }

    public void Clear()
    {
        Phase = DuelPhase.None;
        OpponentName = null;
        IsParty = false;
        _fighters.Clear();
        Changed?.Invoke();
    }
}
