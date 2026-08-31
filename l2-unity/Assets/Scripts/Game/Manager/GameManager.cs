using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class GameManager : MonoBehaviour {
    //[SerializeField] private int _protocolVersion = 1;
    //interlude client
    private int _protocolVersion = 746;
    [SerializeField] private GameState _gameState = GameState.LOGIN_SCREEN;
    private bool _gameReady = false;
    [SerializeField] private Camera _loadingCamera;

    [Inject] GameClient _gameClient;
    [Inject] LoginClient _loginClient;
    [Inject] SceneLoader _sceneLoader;
    [Inject] ItemTable _items;
    [Inject] ItemNameTable _itemNames;
    [Inject] ItemStatDataTable _itemStats;
    [Inject] ArmorgrpTable _armorGrps;
    [Inject] EtcItemgrpTable _etcItemGrps;
    [Inject] WeapongrpTable _weaponGrps;
    [Inject] NpcDecoEffectsTable _npcDecoEffects;
    [Inject] NpcgrpTable _npcGrps;
    [Inject] NpcNameTable _npcNames;
    [Inject] QuestNameTable _questNames;
    [Inject] RecipeTable _recipes;
    [Inject] ActionNameTable _actionNames;
    [Inject] AnimLeghtTable _animLengths;
    [Inject] SysStringTable _sysStrings;
    [Inject] SkillNameTable _skillNames;
    [Inject] SkillgrpTable _skillGrps;
    [Inject] ModelTable _models;
    [Inject] LogongrpTable _logonGrps;
    [Inject] SystemMessageTable _systemMessages;

    private SceneLoader Scenes
    {
        get { return _sceneLoader != null ? _sceneLoader : SceneLoader.Instance; }
    }

    public bool IsSwitchingServer = false;
    public bool WorldSpawnReady { get; private set; }

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
            return;
        }

        if (FindFirstObjectByType<AppLifetimeScope>() == null)
            gameObject.AddComponent<AppLifetimeScope>();
    }

    private void Start() {
        LoadTables();
        Scenes.LoadMenu(); 
    }

    private static T Table<T>(T injected, T fallback) where T : class
    {
        return injected != null ? injected : fallback;
    }

    private void LoadTables() {
        ItemTable items = Table(_items, ItemTable.Instance);
        items.Initialize();
        Table(_itemNames, ItemNameTable.Instance).Initialize();
        Table(_itemStats, ItemStatDataTable.Instance).Initialize();
        Table(_armorGrps, ArmorgrpTable.Instance).Initialize();
        Table(_etcItemGrps, EtcItemgrpTable.Instance).Initialize();
        Table(_weaponGrps, WeapongrpTable.Instance).Initialize();
        items.CacheItems();
        NpcDecoEffectsTable decoEffects = Table(_npcDecoEffects, NpcDecoEffectsTable.Instance);
        decoEffects.Initialize();
        Table(_npcGrps, NpcgrpTable.Instance).Initialize(decoEffects);
        Table(_questNames, QuestNameTable.Instance).Initialize();
        Table(_recipes, RecipeTable.Instance).Initialize();
        Table(_npcNames, NpcNameTable.Instance).Initialize();
        Table(_actionNames, ActionNameTable.Instance).Initialize();
        Table(_animLengths, AnimLeghtTable.Instance).Initialize();
        Table(_sysStrings, SysStringTable.Instance).Initialize();
        Table(_skillNames, SkillNameTable.Instance).Initialize();
        Table(_skillGrps, SkillgrpTable.Instance).Initialize();
        Table(_models, ModelTable.Instance).Initialize();
        Table(_logonGrps, LogongrpTable.Instance).Initialize();
        Table(_systemMessages, SystemMessageTable.Instance).Initialize();
        IconManager.Instance.Initialize();
        IconManager.Instance.CacheIcons();
        IconManager.Instance.CacheOtherIcons();
        IconManager.Instance.CacheInterfaceIcons();
    }

    public void LogIn() {
    }

    public void LogOut() {
        _loginClient.Disconnect();
    }

    public void OnWorldSceneLoaded() {

        GameObject.Destroy(IncomingPacketActions.LoginUi.gameObject);
        PlayerInfoInterlude playerInfo = _gameClient.PlayerInfo;
        IncomingPacketActions.GameWorld.SpawnPlayer(playerInfo.Identity, playerInfo.Status, playerInfo.Stats, playerInfo.Appearance);
        IncomingPacketActions.Ui.StopLoading();
        PlayerStateMachine.Instance.enabled = true;
        WorldSpawnReady = true;
        _gameClient.Send(new EnterWorldCommand());
        IncomingPacketActions.ApplyWorld(apply => apply.FlushKnownlist());
        _gameClient.EndLoadWorld();

        PlayerStateMachine.Instance.ChangeState(PlayerState.IDLE);
        PlayerStateMachine.Instance.NotifyEvent(Event.ENTER_WORLD);
    }


    public void OnLoginServerConnected() {
        GameState = GameState.LOGIN_CONNECTED;
    }

    public void OnLoginServerAuthAllowed() {
        GameState = GameState.READING_LICENSE;
        LobbyFlowLog.Info("state=READING_LICENSE show license");

        IncomingPacketActions.LoginUi.ShowLicenseWindow();
    }

    public void OnLoginServerPlayOk() {
        GameState = GameState.READY_TO_CONNECT;
        LobbyFlowLog.Info("state=READY_TO_CONNECT");
    }

    public void OnConnectingToGameServer() {
        GameState = GameState.CONNECTING_TO_GAMESERVER;
        LobbyFlowLog.Info("state=CONNECTING_TO_GAMESERVER");
    }

    public void OnReceivedServerList(byte lastServer, List<ServerData> serverData, Dictionary<int, int> charsOnServers) {
        GameState = GameState.SERVER_LIST;
        int count = serverData != null ? serverData.Count : 0;
        LobbyFlowLog.Info("state=SERVER_LIST lastServer=" + lastServer + " servers=" + count);

        IncomingPacketActions.LoginUi.ShowServerSelectWindow();

        IncomingPacketActions.ServerSelect.UpdateServerList(lastServer, serverData, charsOnServers);
    }

    public void OnAuthAllowed()
    {
        GameState = GameState.CHAR_SELECT;
        LobbyFlowLog.Info("state=CHAR_SELECT switch camera + show window");
        IncomingPacketActions.LoginCamera.SwitchCamera("CharSelect");

        IncomingPacketActions.LoginUi.ShowCharSelectWindow();
    }

    public void OnCharacterSelect() {
        GameState = GameState.IN_GAME;
        WorldSpawnReady = false;

        IncomingPacketActions.LoginUi.StartLoading();
        Scenes.LoadSWMap();
    }

   

    public void OnCreateUser(List<PlayerTemplates> playerTemplates) {
        GameState = GameState.CHAR_CREATION;

        IncomingPacketActions.LoginCamera.SwitchCamera("Login");

        IncomingPacketActions.LoginUi.SetCharTemplations(playerTemplates);
        IncomingPacketActions.LoginUi.ShowCharCreationWindow();
    }

    public void OnCreateUserFail(string text)
    {
        IncomingPacketActions.LoginUi.ShowCharCreationError(text);
    }

    public void OnWorldLoading() {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.Clear();
        }

        if (IncomingPacketActions.Ui != null)
        {
            IncomingPacketActions.Ui.StartLoading();
        }
        else if (IncomingPacketActions.LoginUi != null)
        {
            IncomingPacketActions.LoginUi.StartLoading();
        }
    }

    public void OnRelogin() {
        IsSwitchingServer = false;
        WorldSpawnReady = false;
        GameState = GameState.LOGIN_SCREEN;

        IncomingPacketActions.LoginCamera.SwitchCamera("Login");

        IncomingPacketActions.LoginUi.ShowLoginWindow();
    }

    public void OnDisconnect() {
        // PlayOk closes the login socket on purpose while GameClient is still connecting.
        // That disconnect must not reopen the login window.
        if (IsSwitchingServer)
            return;

        if (GameState > GameState.CHAR_CREATION) {
            MusicManager.Instance.Clear();
            Scenes.LoadMenu();
        } else if(GameState > GameState.LOGIN_SCREEN && !_gameClient.IsConnected && !_loginClient.IsConnected) {
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
            if (IncomingPacketActions.LoginUi != null)
            {
                IncomingPacketActions.LoginUi.StopLoading();
                IncomingPacketActions.LoginUi.SetLoading(true);
                //L2LoginUI.Instance.OnManualDestroy();
                //Debug.Log("GameManager: OnGameLaunched Success Loading ");
            }
            else
            {
                //Debug.Log("GameManager: пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ !!!! OnGameLaunched L2LoginUI NULLL");
            }

            IncomingPacketActions.Creator.SpawnAllPawns();
        }
        
    }

    public void StartLoading()
    {
        if (_loadingCamera != null)
        {
            _loadingCamera.enabled = true;
        }

        if (IncomingPacketActions.Ui != null)
        {
            IncomingPacketActions.Ui.StartLoading();
        }
        if (IncomingPacketActions.LoginUi != null)
        {
            IncomingPacketActions.LoginUi.StartLoading();
        }
    }
}
