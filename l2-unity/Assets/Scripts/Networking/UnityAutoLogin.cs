using System;
using System.Globalization;
using System.IO;
using System.Collections;
using UnityEngine;

/// <summary>
/// Same steps as AutoLoginInterlude.dll: RequestAuthLogin, accept the license
/// (RequestServerList), RequestServerLogin, RequestGameStart on the character slot,
/// then wait in the world. Runs only when Temp/autologin.request exists.
/// </summary>
public sealed class UnityAutoLogin : MonoBehaviour
{
    public static string RequestPath
    {
        get
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-local-demo-request")
                    return Path.GetFullPath(args[i + 1]);
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "autologin.request"));
        }
    }

    public static string ResultDirectory { get { return Path.GetDirectoryName(RequestPath); } }
    public static bool IsActive { get; private set; }

    public static string WorldPath
    {
        get { return Path.Combine(ResultDirectory, "autologin.world"); }
    }

    string _account = "2222";
    string _password = "2222";
    int _serverId = 1;
    int _slot = 2;
    string _chat = string.Empty;
    float _chatDelay = 20f;
    string _attack = string.Empty;
    float _attackDelay = 5f;
    float _attackTimeout = 60f;
    bool _quitAfter;
    float _deadline;
    bool _connected;
    bool _serverListSent;
    bool _serverLoginSent;
    bool _charSelectSent;
    bool _worldNoted;
    float _worldAt;
    bool _finished;
    string _createName = string.Empty;
    string _createClass = "Fighter";
    bool _creationRequested;
    bool _creationSent;
    float _staySeconds = 30f;
    int _attackSkill;
    string _partyInvite = string.Empty;
    string _partyAcceptFrom = string.Empty;
    float _partyLeaveAfter;
    bool _partyDecline;
    string _duelName = string.Empty;
    string _duelAcceptFrom = string.Empty;
    bool _duelDecline;
    bool _duelAttack;
    float _duelSurrenderAfter;
    string _merchant = string.Empty;
    int _merchantNpc;
    int _questNpc;
    int _questItem;
    bool _questStart;
    bool _questUnavailable;
    int _merchantExpectedPotions = 2;
    string _expectedTile = string.Empty;
    int _travelX;
    int _travelY;
    int _travelAfter = 12;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!File.Exists(RequestPath))
            return;

        GameObject go = new GameObject("UnityAutoLogin");
        DontDestroyOnLoad(go);
        go.AddComponent<UnityAutoLogin>();
    }

    void Awake()
    {
        ReadRequest();
        IsActive = true;
        gameObject.AddComponent<LocalDemoUIProbe>();
        _deadline = Time.realtimeSinceStartup + 180f + Mathf.Max(0f, _chatDelay);
        Debug.Log("[AutoLogin] request account=" + _account + " serverId=" + _serverId + " slot=" + _slot);
    }

    void OnDestroy()
    {
        IsActive = false;
    }

    void Update()
    {
        if (_finished)
            return;
        if (Time.realtimeSinceStartup > _deadline)
        {
            Finish("FAIL timeout");
            return;
        }

        GameManager manager = GameManager.Instance;
        LoginClient login = LoginClient.Instance;
        if (manager == null || login == null)
            return;

        if (!_connected && manager.GameState == GameState.LOGIN_SCREEN)
        {
            login.Account = _account;
            login.Password = _password;
            _connected = true;
            Debug.Log("[AutoLogin] Sending RequestAuthLogin...");
            login.Connect();
        }

        if (!_serverListSent && manager.GameState == GameState.READING_LICENSE)
        {
            _serverListSent = true;
            Debug.Log("[AutoLogin] EULA accepted. Sending RequestServerList...");
            login.Send(new RequestServerListCommand(login.SessionKey1, login.SessionKey2));
        }

        if (!_serverLoginSent && manager.GameState == GameState.SERVER_LIST)
        {
            _serverLoginSent = true;
            Debug.Log("[AutoLogin] Sending RequestServerLogin serverId=" + _serverId + "...");
            login.OnServerSelected(_serverId);
        }

        if (!_charSelectSent && manager.GameState == GameState.CHAR_SELECT)
            TryEnterCharacter();

        if (_creationRequested && !_creationSent && manager.GameState == GameState.CHAR_CREATION)
        {
            CharCreationWindow window = CharCreationWindow.Instance;
            if (window != null && window.TryCreateHuman(_createName, _createClass))
            {
                _creationSent = true;
                Debug.Log("[AutoLogin] character creation sent name=" + _createName + " class=" + _createClass);
            }
        }

        if (manager.WorldSpawnReady)
            OnWorldReady();
    }

    void OnWorldReady()
    {
        if (!_worldNoted)
        {
            _worldNoted = true;
            _worldAt = Time.realtimeSinceStartup;
            WriteFile(WorldPath, "OK");
            Debug.Log("[AutoLogin] WORLD ENTER OK");
            StartCoroutine(TracePresence());
            if (!string.IsNullOrEmpty(_expectedTile) || _travelX != 0 || _travelY != 0)
            {
                LocalDemoTravelProbe travel = gameObject.AddComponent<LocalDemoTravelProbe>();
                travel.ExpectedTile = _expectedTile;
                travel.OffsetX = _travelX;
                travel.OffsetY = _travelY;
                travel.Delay = _travelAfter;
            }
            if (_questNpc > 0)
            {
                var quest = gameObject.AddComponent<LocalDemoQuestProbe>();
                quest.NpcId = _questNpc;
                quest.ExpectedItem = _questItem;
                quest.StartQuest = _questStart;
                quest.ExpectUnavailable = _questUnavailable;
            }
            if (!string.IsNullOrEmpty(_merchant))
            {
                var merchant = gameObject.AddComponent<LocalDemoMerchantProbe>();
                merchant.Mode = _merchant;
                merchant.NpcId = _merchantNpc;
                merchant.ExpectedPotions = _merchantExpectedPotions;
            }
            bool partyProbe = !string.IsNullOrEmpty(_partyInvite) || !string.IsNullOrEmpty(_partyAcceptFrom);
            bool duelProbe = !string.IsNullOrEmpty(_duelName) || !string.IsNullOrEmpty(_duelAcceptFrom);
            if (duelProbe)
            {
                LocalDemoDuelProbe probe = gameObject.AddComponent<LocalDemoDuelProbe>();
                probe.ChallengeName = _duelName;
                probe.AcceptFrom = _duelAcceptFrom;
                probe.Decline = _duelDecline;
                probe.Attack = _duelAttack;
                probe.SurrenderAfter = _duelSurrenderAfter;
            }
            if (partyProbe)
            {
                LocalDemoPartyProbe probe = gameObject.AddComponent<LocalDemoPartyProbe>();
                probe.InviteName = _partyInvite;
                probe.AcceptFrom = _partyAcceptFrom;
                probe.LeaveAfter = _partyLeaveAfter;
                probe.DeclineInvite = _partyDecline;
            }
            if (_quitAfter && (string.IsNullOrEmpty(_attack) || partyProbe))
                StartCoroutine(QuitAfterStay());
            if (_attack.Equals("nearest", StringComparison.OrdinalIgnoreCase))
            {
                UnityAutoCombat combat = gameObject.AddComponent<UnityAutoCombat>();
                combat.StartDelay = _attackDelay;
                combat.Timeout = _attackTimeout;
                combat.QuitAfter = _quitAfter && !partyProbe;
                combat.SkillId = _attackSkill;
            }
            if (string.IsNullOrEmpty(_chat))
            {
                _finished = true;
                enabled = false;
                return;
            }

            Debug.Log("[AutoLogin] chat '" + _chat + "' in " + _chatDelay.ToString("0") + "s");
        }

        if (Time.realtimeSinceStartup < _worldAt + _chatDelay)
            return;

        if (ChatWindow.Instance == null || !ChatWindow.Instance.SubmitText(_chat))
            return;
        Debug.Log("[AutoLogin] sent chat " + _chat);
        WriteFile(ChatPath, "OK");
        _finished = true;
        enabled = false;
    }

    public static string ChatPath
    {
        get { return Path.Combine(ResultDirectory, "autologin.chat"); }
    }

    void TryEnterCharacter()
    {
        CharacterSelector selector = CharacterSelector.Instance;
        CharSelectWindow window = CharSelectWindow.Instance;
        if (selector == null || window == null || !window.UiReady)
            return;
        if (!string.IsNullOrEmpty(_createName))
        {
            int existingSlot = selector.FindInterludeSlot(_createName);
            if (existingSlot >= 0)
                _slot = existingSlot;
            else
            {
                if (!_creationRequested)
                {
                    _creationRequested = true;
                    IncomingPacketActions.Game.Send(new NewCharacterCommand());
                    Debug.Log("[AutoLogin] requesting character templates for " + _createName);
                }
                return;
            }
        }
        if (selector.InterludeCount <= _slot)
            return;

        _charSelectSent = true;
        Debug.Log("[AutoLogin] Sending RequestGameStart slot=" + _slot + "...");
        selector.SelectInterludeCharacter(_slot);
        selector.ConfirmSelection();
    }

    void ReadRequest()
    {
        string[] lines;
        try
        {
            lines = File.ReadAllLines(RequestPath);
        }
        catch (Exception)
        {
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim();
            if (key.Equals("account", StringComparison.OrdinalIgnoreCase))
                _account = value.ToLowerInvariant();
            else if (key.Equals("password", StringComparison.OrdinalIgnoreCase))
                _password = value;
            else if (key.Equals("serverId", StringComparison.OrdinalIgnoreCase))
                _serverId = ParseInt(value, _serverId);
            else if (key.Equals("slot", StringComparison.OrdinalIgnoreCase))
                _slot = ParseInt(value, _slot);
            else if (key.Equals("chat", StringComparison.OrdinalIgnoreCase))
                _chat = value;
            else if (key.Equals("chatDelay", StringComparison.OrdinalIgnoreCase))
                _chatDelay = ParseInt(value, (int)_chatDelay);
            else if (key.Equals("attack", StringComparison.OrdinalIgnoreCase))
                _attack = value;
            else if (key.Equals("attackDelay", StringComparison.OrdinalIgnoreCase))
                _attackDelay = ParseInt(value, (int)_attackDelay);
            else if (key.Equals("attackTimeout", StringComparison.OrdinalIgnoreCase))
                _attackTimeout = ParseInt(value, (int)_attackTimeout);
            else if (key.Equals("quitAfter", StringComparison.OrdinalIgnoreCase))
                _quitAfter = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
            else if (key.Equals("createName", StringComparison.OrdinalIgnoreCase))
                _createName = value;
            else if (key.Equals("createClass", StringComparison.OrdinalIgnoreCase))
                _createClass = value;
            else if (key.Equals("staySeconds", StringComparison.OrdinalIgnoreCase))
                _staySeconds = ParseInt(value, (int)_staySeconds);
            else if (key.Equals("attackSkill", StringComparison.OrdinalIgnoreCase))
                _attackSkill = ParseInt(value, 0);
            else if (key.Equals("partyInvite", StringComparison.OrdinalIgnoreCase))
                _partyInvite = value;
            else if (key.Equals("partyAcceptFrom", StringComparison.OrdinalIgnoreCase))
                _partyAcceptFrom = value;
            else if (key.Equals("partyLeaveAfter", StringComparison.OrdinalIgnoreCase))
                _partyLeaveAfter = ParseInt(value, 0);
            else if (key.Equals("partyDecline", StringComparison.OrdinalIgnoreCase))
                _partyDecline = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("duelName", StringComparison.OrdinalIgnoreCase))
                _duelName = value;
            else if (key.Equals("duelAcceptFrom", StringComparison.OrdinalIgnoreCase))
                _duelAcceptFrom = value;
            else if (key.Equals("duelDecline", StringComparison.OrdinalIgnoreCase))
                _duelDecline = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("duelAttack", StringComparison.OrdinalIgnoreCase))
                _duelAttack = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("duelSurrenderAfter", StringComparison.OrdinalIgnoreCase))
                _duelSurrenderAfter = ParseInt(value, 0);
            else if (key.Equals("questNpc", StringComparison.OrdinalIgnoreCase))
                _questNpc = ParseInt(value, 0);
            else if (key.Equals("questItem", StringComparison.OrdinalIgnoreCase))
                _questItem = ParseInt(value, 0);
            else if (key.Equals("questStart", StringComparison.OrdinalIgnoreCase))
                _questStart = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("questUnavailable", StringComparison.OrdinalIgnoreCase))
                _questUnavailable = value.Equals("true", StringComparison.OrdinalIgnoreCase);
            else if (key.Equals("merchantNpc", StringComparison.OrdinalIgnoreCase))
                _merchantNpc = ParseInt(value, 0);
            else if (key.Equals("merchantExpectedPotions", StringComparison.OrdinalIgnoreCase))
                _merchantExpectedPotions = ParseInt(value, 2);
            else if (key.Equals("merchant", StringComparison.OrdinalIgnoreCase))
                _merchant = value;
            else if (key.Equals("expectedTile", StringComparison.OrdinalIgnoreCase))
                _expectedTile = value;
            else if (key.Equals("travelX", StringComparison.OrdinalIgnoreCase))
                _travelX = ParseInt(value, 0);
            else if (key.Equals("travelY", StringComparison.OrdinalIgnoreCase))
                _travelY = ParseInt(value, 0);
            else if (key.Equals("travelAfter", StringComparison.OrdinalIgnoreCase))
                _travelAfter = ParseInt(value, 12);
        }
    }

    IEnumerator QuitAfterStay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(_staySeconds, _chatDelay + 5f));
        Debug.Log("[AutoLogin] quitting after stay");
        Application.Quit();
    }

    IEnumerator TracePresence()
    {
        string previous = null;
        while (true)
        {
            yield return new WaitForSecondsRealtime(5f);
            if (World.Instance == null || World.Instance.UsersContainer == null)
                continue;
            var names = new System.Collections.Generic.List<string>();
            foreach (UserEntity user in World.Instance.UsersContainer.GetComponentsInChildren<UserEntity>(false))
                if (user.Identity != null)
                    names.Add(user.Identity.Name + "#" + user.Identity.Id);
            names.Sort(StringComparer.Ordinal);
            string present = string.Join(",", names);
            if (present != previous)
            {
                Debug.Log("[LocalDemoWorld] peers=" + present);
                previous = present;
            }
        }
    }

    static int ParseInt(string text, int fallback)
    {
        int value;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return value;
        return fallback;
    }

    void Finish(string result)
    {
        _finished = true;
        Debug.Log("[AutoLogin] WORLD ENTER " + result);
        WriteFile(WorldPath, result);
        enabled = false;
    }

    static void WriteFile(string path, string text)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text + "\n");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[AutoLogin] could not write " + path + ": " + e.Message);
        }
    }
}
