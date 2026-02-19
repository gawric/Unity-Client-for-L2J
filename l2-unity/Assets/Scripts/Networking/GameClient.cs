using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using L2_login;


public class GameClient : DefaultClient {
    [SerializeField] protected PlayerInfoInterlude _playerInfo;
    [SerializeField] protected int _serverId;
    [SerializeField] private int _playKey1;
    [SerializeField] private int _playKey2;
    private GameCrypt _gameCrypt;
    private readonly object syncLock = new object();
    private bool _isLoadComplete { get; set; }

    public PlayerInfoInterlude PlayerInfo { get { return _playerInfo; } set { _playerInfo = value; } }
    public string CurrentPlayer { get { return _playerInfo.Identity.Name; } }
    public int ServerId { get { return _serverId; } set { _serverId = value; } }
    public int PlayKey1 { get { return _playKey1; } set { _playKey1 = value; } }
    public int PlayKey2 { get { return _playKey2; } set { _playKey2 = value; } }
    public GameCrypt GameCrypt { get { return _gameCrypt; } }   

    private GameClientInterludePacketHandler clientPacketHandler;
    private GameServerInterludePacketHandler serverPacketHandler;

    public GameClientInterludePacketHandler ClientPacketHandler { get { return clientPacketHandler; } }
    public GameServerInterludePacketHandler ServerPacketHandler { get { return serverPacketHandler; } }

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
        clientPacketHandler = new GameClientInterludePacketHandler();
        serverPacketHandler = new GameServerInterludePacketHandler();

        _client = new AsynchronousClient(_serverIp, _serverPort, this, clientPacketHandler, serverPacketHandler, false);
    }

    public void EnableCrypt(byte[] key) {
        _gameCrypt = new GameCrypt();
        _gameCrypt.SetKey(key);
        _client.CryptEnabled = true;
    }

    public bool IsCryptEnabled()
    {
        return _client.CryptEnabled;
    }


    protected override void WhileConnecting() {
        base.WhileConnecting();

        GameManager.Instance.OnConnectingToGameServer();
    }

    protected override void OnConnectionSuccess() {
        base.OnConnectionSuccess();

        Debug.Log("Connected to GameServer");
        //746 interlude protocol
        SendGameDataQueue.Instance().AddItem(CreatorPacketsGameLobby.CreateProtocolVersion(GameManager.Instance.ProtocolVersion) , false , false);
        //clientPacketHandler.SendProtocolVersion();
    }

    public override void OnConnectionFailed() {
        base.OnConnectionFailed();
    }

    public override void OnAuthAllowed() {
        //Debug.Log("Authed to GameServer");

        GameManager.Instance.OnAuthAllowed();
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
