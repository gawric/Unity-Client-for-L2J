using System.Collections.Generic;
using UnityEngine;
using static ServerListPacket;

public class GameManager : MonoBehaviour {
    //[SerializeField] private int _protocolVersion = 1;
    //interlude client
    private int _protocolVersion = 746;
    [SerializeField] private GameState _gameState = GameState.LOGIN_SCREEN;
    private bool _gameReady = false;
    [SerializeField] private Camera _loadingCamera;

    public GameState GameState {
        get { return _gameState; }
        set {
            _gameState = value;
            //Debug.Log($"Game state is now {_gameState}.");
        }
    }
    public bool GameReady { get { return _gameReady; } set { _gameReady = value; } }
    public int ProtocolVersion { get { return _protocolVersion; } }

    private static GameManager _instance;
    public static GameManager Instance { get { return _instance; } }

    void Awake() {
        if (_instance == null) {
            _instance = this;
        } else if (_instance != this) {
            Destroy(this);
        }
    }

    private void Start() {
        LoadTables();
        SceneLoader.Instance.LoadMenu(); 
    }

    private void LoadTables() {
        ItemTable.Instance.Initialize();
        ItemNameTable.Instance.Initialize();
        ItemStatDataTable.Instance.Initialize();
        ArmorgrpTable.Instance.Initialize();
        EtcItemgrpTable.Instance.Initialize();
        WeapongrpTable.Instance.Initialize();
        ItemTable.Instance.CacheItems();
        NpcgrpTable.Instance.Initialize();
        QuestNameTable.Instance.Initialize();
        RecipeTable.Instance.Initialize();
        NpcNameTable.Instance.Initialize();
        ActionNameTable.Instance.Initialize();
        AnimLeghtTable.Instance.Initialize();
        SysStringTable.Instance.Initialize();
        SkillNameTable.Instance.Initialize();
        SkillgrpTable.Instance.Initialize();
        ModelTable.Instance.Initialize();
        LogongrpTable.Instance.Initialize();
        SystemMessageTable.Instance.Initialize();
        IconManager.Instance.Initialize();
        IconManager.Instance.CacheIcons();
        IconManager.Instance.CacheOtherIcons();
        IconManager.Instance.CacheInterfaceIcons();
        //KeyImageTable.Instance.Initialize();
    }

    public void LogIn() {
    }

    public void LogOut() {
        LoginClient.Instance.Disconnect();
    }

    public void OnWorldSceneLoaded() {

        GameObject.Destroy(L2LoginUI.Instance.gameObject);
        PlayerInfoInterlude playerInfo = GameClient.Instance.PlayerInfo;
        World.Instance.SpawnPlayerInterlude(playerInfo.Identity, playerInfo.Status, playerInfo.Stats, playerInfo.Appearance);
        L2GameUI.Instance.StopLoading();
        PlayerStateMachine.Instance.enabled = true;
        GameClient.Instance.ClientPacketHandler.EndLoadWorld();

        PlayerStateMachine.Instance.ChangeState(PlayerState.IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.ENTER_WORLD);
    }


    public void OnLoginServerConnected() {
        GameState = GameState.LOGIN_CONNECTED;
    }

    public void OnLoginServerAuthAllowed() {
        GameState = GameState.READING_LICENSE;

        L2LoginUI.Instance.ShowLicenseWindow();
    }

    public void OnLoginServerPlayOk() {
        GameState = GameState.READY_TO_CONNECT;
    }

    public void OnConnectingToGameServer() {
        GameState = GameState.CONNECTING_TO_GAMESERVER;
    }

    public void OnReceivedServerList(byte lastServer, List<ServerData> serverData, Dictionary<int, int> charsOnServers) {
        GameState = GameState.SERVER_LIST;

        L2LoginUI.Instance.ShowServerSelectWindow();

        ServerSelectWindow.Instance.UpdateServerList(lastServer, serverData, charsOnServers);
    }

    public void OnAuthAllowed() {
        GameState = GameState.CHAR_SELECT;
       // Debug.Log("Event Allowed Char Select");
        LoginCameraManager.Instance.SwitchCamera("CharSelect");

        L2LoginUI.Instance.ShowCharSelectWindow();
    }

    public void OnCharacterSelect() {
        GameState = GameState.IN_GAME;

        L2LoginUI.Instance.StartLoading();
        SceneLoader.Instance.LoadGame();
    }

   

    public void OnCreateUser(List<PlayerTemplates> playerTemplates) {
        GameState = GameState.CHAR_CREATION;

        LoginCameraManager.Instance.SwitchCamera("Login");

        L2LoginUI.Instance.SetCharTemplations(playerTemplates);
        L2LoginUI.Instance.ShowCharCreationWindow();
    }

    public void OnCreateUserFail(string text)
    {
        L2LoginUI.Instance.ShowCharCreationError(text);
    }

    public void OnWorldLoading() {
        MusicManager.Instance.Clear();
        L2GameUI.Instance.StartLoading();
    }

    public void OnRelogin() {
        GameState = GameState.LOGIN_SCREEN;

        LoginCameraManager.Instance.SwitchCamera("Login");

        L2LoginUI.Instance.ShowLoginWindow();
    }

    public void OnDisconnect() {
        if (GameState > GameState.CHAR_CREATION) {
            MusicManager.Instance.Clear();
            SceneLoader.Instance.LoadMenu();
        } else if(GameState > GameState.LOGIN_SCREEN && !GameClient.Instance.IsConnected && !LoginClient.Instance.IsConnected) {
            OnRelogin();
        }
    }

    public void OnGameserverSelected() {
        Debug.Log("Gameserver selected, connecting...");

        //GameClient.Instance.Connect();
    }

    public void OnStartingGame() {
        Debug.Log("On Starting game");
        //L2LoginUI.Instance.StartLoading();
    }

    public void OnGameLaunched() {
        if(GameState.IN_GAME == GameState)
        {
            //Debug.Log("On game launched");
            if (L2LoginUI.Instance != null)
            {
                L2LoginUI.Instance.StopLoading();
                L2LoginUI.Instance.SetLoading(true);
                //L2LoginUI.Instance.OnManualDestroy();
                //Debug.Log("GameManager: OnGameLaunched Success Loading ");
            }
            else
            {
                //Debug.Log("GameManager: ����������� ������ !!!! OnGameLaunched L2LoginUI NULLL");
            }

            CharacterCreator.Instance.SpawnAllPawns();
        }
        
    }

    public void StartLoading()
    {
        _loadingCamera.enabled = true;
        if (L2GameUI.Instance != null)
        {
            L2GameUI.Instance.StartLoading();
        }
        if (L2LoginUI.Instance != null)
        {
            L2LoginUI.Instance.StartLoading();
        }
    }
}
