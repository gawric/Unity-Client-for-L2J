using UnityEngine;
using System.Collections.Generic;
using L2_login;

public class LoginClient : DefaultClient {
    // Crypt
    public static byte[] STATIC_BLOWFISH_KEY = {
        (byte) 0x6b,
        (byte) 0x60,
        (byte) 0xcb,
        (byte) 0x5b,
        (byte) 0x82,
        (byte) 0xce,
        (byte) 0x90,
        (byte) 0xb1,
        (byte) 0xcc,
        (byte) 0x2b,
        (byte) 0x6c,
        (byte) 0x55,
        (byte) 0x6c,
        (byte) 0x6c,
        (byte) 0x6c,
        (byte) 0x6c
    };


    [Header("Account")]
    [SerializeField] protected string _account;
    [SerializeField] protected string _password;

    private RSACrypt _rsa;
    private byte[] _blowfishKey;
    private BlowfishEngine _decryptBlowfish;
    private BlowfishEngine _encryptBlowfish;
    private int _sessionId;

    public RSACrypt RSACrypt { get { return _rsa; } }
    public BlowfishEngine DecryptBlowFish { get { return _decryptBlowfish; } }
    public BlowfishEngine EncryptBlowFish { get { return _encryptBlowfish; } }
    public byte[] BlowfishKey { get { return _blowfishKey; } }

    public string Account { get { return _account; } set { _account = value; } }
    public string Password { get { return _password; } set { _password = value; } }

    private LoginClientPacketHandler clientPacketHandler;
    private LoginServerPacketHandler serverPacketHandler;

    public LoginClientPacketHandler ClientPacketHandler { get { return clientPacketHandler; } }
    public LoginServerPacketHandler ServerPacketHandler { get { return serverPacketHandler; } }


    private static LoginClient _instance;
    public static LoginClient Instance { get { return _instance; } }

    private void Reset() {
        _serverIp = "127.0.0.1";
        _serverPort = 2106;
    }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else if (_instance != this) {
            Destroy(this);
            if (Network != null)
            {
                Network.IncomingLogin.Stop();
                Network.SendLogin.Stop();
            }
        }
    }

    public void SetBlowFishKey(byte[] blowfishKey) {
        _client.CryptEnabled = true;

        _blowfishKey = blowfishKey;

        _decryptBlowfish = new BlowfishEngine();
        _decryptBlowfish.init(false, blowfishKey);

        _encryptBlowfish = new BlowfishEngine();
        _encryptBlowfish.init(true, blowfishKey);

        if (Network != null && Network.Protocol != null)
            Network.Protocol.SetLoginBlowfishKey(blowfishKey);

        Debug.Log("Blowfish key set.");
    }

    public void SetRSAKey(byte[] rsaKey) {
        _rsa = new RSACrypt(rsaKey, true);
    }

    public void SetSessionId(int sessionId)
    {
        this._sessionId = sessionId;
    }

    public void CompleteInitPacket()
    {
        if (_client != null)
            _client.InitPacket = false;
    }

    public int GetGessionId()
    {
        return _sessionId;
    }
    protected override void CreateAsyncClient() {
        if (_client == null)
        {
            clientPacketHandler = new LoginClientPacketHandler();
            serverPacketHandler = new LoginServerPacketHandler(Network.Protocol, Network.Dispatcher);

            _client = new AsynchronousClient(_serverIp, _serverPort, this, clientPacketHandler, serverPacketHandler, true, Network);
        }
          
    }

    protected override void WhileConnecting() {
        base.WhileConnecting();
        SetBlowFishKey(STATIC_BLOWFISH_KEY);
    }

    protected override void OnConnectionSuccess() {
        base.OnConnectionSuccess();

        Debug.Log("Connected to LoginServer");

        IncomingPacketActions.Manager.OnLoginServerConnected();
    }

    public override void OnConnectionFailed() {
        base.OnConnectionFailed();
    }

    public override void OnAuthAllowed() {
        Debug.Log("Authed to LoginServer");
        IncomingPacketActions.Manager.OnLoginServerAuthAllowed();
    }

    public void OnServerListReceived(byte lastServer, List<ServerData> serverData, Dictionary<int, int> charsOnServers) {

        IncomingPacketActions.Manager.OnReceivedServerList(lastServer, serverData, charsOnServers);
    }

    public void Send(INetworkCommand command)
    {
        if (command == null || Network == null)
            return;

        Network.SendLogin.AddItem(command);
    }

    public void OnServerSelected(int serverId) {
        Send(new RequestServerLoginCommand(serverId, SessionKey1, SessionKey2));
    }

    public override void OnDisconnect() {
        base.OnDisconnect();
        if (Network != null)
        {
            Network.IncomingLogin.Stop();
            Network.SendLogin.Stop();
        }
        Debug.Log("Disconnected from LoginServer.");
    }
}
