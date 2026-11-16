using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Explicit test requests use the same chat and invitation response paths as the UI.
public sealed class LocalDemoPartyProbe : MonoBehaviour
{
    public string InviteName;
    public string AcceptFrom;
    public float LeaveAfter;
    public bool DeclineInvite;
    static LocalDemoPartyProbe _instance;
    PlayerParty _party;
    string _lastState;

    public static bool TryRespond(PartyInvitationWindow window, AskJoinPartyDto request)
    {
        if (_instance == null || string.IsNullOrEmpty(_instance.AcceptFrom) ||
            !string.Equals(_instance.AcceptFrom, request.RequestorName, System.StringComparison.OrdinalIgnoreCase))
            return false;
        bool accepted = !_instance.DeclineInvite;
        window.Respond(accepted);
        Debug.Log("[LocalDemoParty] response=" + accepted + " inviter=" + request.RequestorName);
        return true;
    }

    IEnumerator Start()
    {
        _instance = this;
        _party = IncomingPacketActions.Game.Party;
        _party.Changed += TraceState;
        TraceState();
        float started = Time.realtimeSinceStartup;
        if (!string.IsNullOrEmpty(InviteName))
        {
            yield return new WaitForSecondsRealtime(12f);
            bool sent = ChatWindow.Instance.SubmitText("/invite " + InviteName);
            Debug.Log("[LocalDemoParty] invite=" + InviteName + " sent=" + sent);
        }
        if (LeaveAfter > 0)
        {
            while (Time.realtimeSinceStartup < started + LeaveAfter)
                yield return null;
            bool sent = ChatWindow.Instance.SubmitText("/leave");
            Debug.Log("[LocalDemoParty] leave sent=" + sent);
        }
    }

    void TraceState()
    {
        var members = new List<string>();
        foreach (PartyMemberInfo member in _party.Members)
            members.Add(member.Name + "#" + member.ObjectId + " hp=" + member.Hp + "/" + member.MaxHp + " mp=" + member.Mp + "/" + member.MaxMp);
        string state = "leader=" + _party.LeaderId + " peers=" + string.Join(";", members);
        if (state == _lastState)
            return;
        _lastState = state;
        Debug.Log("[LocalDemoParty] " + state);
        File.AppendAllText(Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.party"), state + "\n");
    }

    void OnDestroy()
    {
        if (_party != null)
            _party.Changed -= TraceState;
        if (_instance == this)
            _instance = null;
    }
}
