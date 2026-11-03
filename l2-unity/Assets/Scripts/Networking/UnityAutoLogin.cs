using System;
using System.Globalization;
using System.IO;
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
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "autologin.request")); }
    }

    public static string WorldPath
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "autologin.world")); }
    }

    string _account = "2222";
    string _password = "2222";
    int _serverId = 1;
    int _slot = 2;
    string _chat = string.Empty;
    float _chatDelay = 20f;
    float _deadline;
    bool _connected;
    bool _serverListSent;
    bool _serverLoginSent;
    bool _charSelectSent;
    bool _worldNoted;
    float _worldAt;
    bool _finished;

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
        _deadline = Time.realtimeSinceStartup + 180f + Mathf.Max(0f, _chatDelay);
        Debug.Log("[AutoLogin] request account=" + _account + " serverId=" + _serverId + " slot=" + _slot);
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

        IncomingPacketActions.Game.Send(new RequestSay2Command(ChatTypes.GetById(0), _chat, ""));
        Debug.Log("[AutoLogin] sent chat " + _chat);
        WriteFile(ChatPath, "OK");
        _finished = true;
        enabled = false;
    }

    public static string ChatPath
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "autologin.chat")); }
    }

    void TryEnterCharacter()
    {
        CharacterSelector selector = CharacterSelector.Instance;
        CharSelectWindow window = CharSelectWindow.Instance;
        if (selector == null || window == null || !window.UiReady)
            return;
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
