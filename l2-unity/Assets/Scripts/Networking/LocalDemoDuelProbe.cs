using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class LocalDemoDuelProbe : MonoBehaviour
{
    public string ChallengeName;
    public string AcceptFrom;
    public bool Decline;
    public bool Attack;
    public float SurrenderAfter;
    static LocalDemoDuelProbe _instance;
    PlayerDuel _duel;
    bool _started;
    string _last;

    public static void TryRespond(string name)
    {
        if (_instance == null || string.IsNullOrEmpty(_instance.AcceptFrom) ||
            !string.Equals(name, _instance.AcceptFrom, System.StringComparison.OrdinalIgnoreCase))
            return;
        DuelActions.Respond(!_instance.Decline);
        Debug.Log("[LocalDemoDuel] response=" + !_instance.Decline + " inviter=" + name);
    }

    IEnumerator Start()
    {
        _instance = this;
        _duel = IncomingPacketActions.Game.Duel;
        _duel.Changed += Trace;
        Trace();
        if (!string.IsNullOrEmpty(ChallengeName))
        {
            yield return new WaitForSecondsRealtime(12f);
            bool sent = ChatWindow.Instance.SubmitText("/duel " + ChallengeName);
            Debug.Log("[LocalDemoDuel] challenge=" + ChallengeName + " sent=" + sent);
        }
    }

    void Trace()
    {
        var fighters = new List<string>();
        foreach (DuelFighterInfo info in _duel.Fighters)
            fighters.Add(info.Name + " cp=" + info.Cp + "/" + info.MaxCp + " hp=" + info.Hp + "/" + info.MaxHp + " mp=" + info.Mp + "/" + info.MaxMp);
        string line = "phase=" + _duel.Phase + " peer=" + _duel.OpponentName + " stats=" + string.Join(";", fighters);
        if (line != _last)
        {
            _last = line;
            Debug.Log("[LocalDemoDuel] " + line);
            File.AppendAllText(Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.duel"), line + "\n");
        }
        if (_duel.Phase == DuelPhase.Active && !_started)
        {
            _started = true;
            if (Attack)
                StartCoroutine(AttackOpponent());
            if (SurrenderAfter > 0)
                StartCoroutine(SurrenderLater());
        }
    }

    IEnumerator AttackOpponent()
    {
        yield return new WaitForSecondsRealtime(1f);
        UserEntity target = null;
        foreach (UserEntity user in World.Instance.UsersContainer.GetComponentsInChildren<UserEntity>(false))
            if (user.Identity != null && user.Identity.Name == _duel.OpponentName)
                target = user;
        if (target == null || _duel.Phase != DuelPhase.Active)
        {
            Debug.Log("[LocalDemoDuel] FAIL no duel target");
            yield break;
        }
        PlayerActions.Instance.ForceAttack(target);
        yield return new WaitForSecondsRealtime(1.5f);
        if (_duel.Phase == DuelPhase.Active)
        {
            bool sent = PlayerActions.Instance.ForceAttack(target);
            Debug.Log("[LocalDemoDuel] forced attack sent=" + sent);
        }
    }

    IEnumerator SurrenderLater()
    {
        yield return new WaitForSecondsRealtime(SurrenderAfter);
        if (_duel.Phase == DuelPhase.Active)
        {
            ChatWindow.Instance.SubmitText("/withdraw");
            Debug.Log("[LocalDemoDuel] surrender sent");
        }
    }

    void OnDestroy()
    {
        if (_duel != null)
            _duel.Changed -= Trace;
        if (_instance == this)
            _instance = null;
    }
}
