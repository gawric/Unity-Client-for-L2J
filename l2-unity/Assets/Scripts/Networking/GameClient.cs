using UnityEngine;

public class GameClient : DefaultClient {
    protected PlayerInfoInterlude _playerInfo;
    protected int _serverId;
    private int _playKey1;
    private int _playKey2;
    private readonly object syncLock = new object();
    private bool _isLoadComplete { get; set; }

    public PlayerInfoInterlude PlayerInfo { get { return _playerInfo; } set { _playerInfo = value; } }
    public string CurrentPlayer { get { return _playerInfo.Identity.Name; } }
    public int ServerId { get { return _serverId; } set { _serverId = value; } }
    public int PlayKey1 { get { return _playKey1; } set { _playKey1 = value; } }
    public int PlayKey2 { get { return _playKey2; } set { _playKey2 = value; } } 

    private GameClientPacketHandler clientPacketHandler;
    private GameServerPacketHandler serverPacketHandler;

    public GameClientPacketHandler ClientPacketHandler { get { return clientPacketHandler; } }
    public GameServerPacketHandler ServerPacketHandler { get { return serverPacketHandler; } }

    private static GameClient _instance;
    public static GameClient Instance { get { return _instance; } }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else if (_instance != this) {
            Destroy(this);
        }
    }

    protected override void CreateAsyncClient() {
        clientPacketHandler = new GameClientPacketHandler();
        serverPacketHandler = new GameServerPacketHandler(Network.Protocol, Network.Dispatcher);

        _client = new AsynchronousClient(_serverIp, _serverPort, this, clientPacketHandler, serverPacketHandler, false, Network);
    }

    public void EnableCrypt(byte[] key) {
        if (Network != null && Network.Protocol != null)
            Network.Protocol.SetGameCryptKey(key);
        _client.CryptEnabled = true;
    }

    public bool IsCryptEnabled()
    {
        return _client != null && _client.CryptEnabled;
    }


    protected override void WhileConnecting() {
        base.WhileConnecting();

        IncomingPacketActions.Manager.OnConnectingToGameServer();
    }

    protected override void OnConnectionSuccess() {
        base.OnConnectionSuccess();
        LobbyFlowLog.Info("GameClient.OnConnectionSuccess — send ProtocolVersion");

        GameManager manager = IncomingPacketActions.Manager;
        if (manager != null)
            manager.IsSwitchingServer = false;
        else
            LobbyFlowLog.Error("GameClient.OnConnectionSuccess Manager is null");

        int protocol = manager != null ? manager.ProtocolVersion : 746;
        LobbyFlowLog.Info("TX ProtocolVersion=" + protocol + " crypt=" + IsCryptEnabled());
        SendPlain(new ProtocolVersionCommand(protocol));
    }

    public void Send(INetworkCommand command)
    {
        EnqueueGame(command, IsCryptEnabled());
    }

    public void SendPlain(INetworkCommand command)
    {
        EnqueueGame(command, false);
    }

    private void EnqueueGame(INetworkCommand command, bool crypt)
    {
        if (command == null || Network == null)
            return;

        Network.SendGame.AddItem(command, crypt);
    }

    public void EndLoadWorld()
    {
        Send(new RequestSkillCoolTimeCommand());
    }

    public override void OnConnectionFailed() {
        base.OnConnectionFailed();
        IncomingPacketActions.Manager.IsSwitchingServer = false;
        IncomingPacketActions.Manager.OnRelogin();
    }

    public override void OnAuthAllowed() {
        //Debug.Log("Authed to GameServer");

        IncomingPacketActions.Manager.OnAuthAllowed();
    }

    public override void OnDisconnect() {
        base.OnDisconnect();
    }

   public bool DataPreparationCompleted()
    {
        lock (syncLock) {
           return _isLoadComplete;
        }
    }

    public bool SetDataPreparationCompleted(bool isComplete)
    {
        lock (syncLock)
        {
            return _isLoadComplete = isComplete;
        }
    }
}
