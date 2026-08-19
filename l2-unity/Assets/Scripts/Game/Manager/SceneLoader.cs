using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string _menuScene = "Menu";
    [SerializeField] private string _lobbyScene = "l2_lobby";
    [SerializeField] private string _gameScene = "Game";
    [SerializeField] private float _tilePreloadDistance = 80f;

    private readonly HashSet<string> _loadedTiles = new HashSet<string>();
    private bool _worldStreaming;
    private bool _streamingBusy;

    private GameManager Manager
    {
        get { return IncomingPacketActions.Manager; }
    }

    public string GameScene { get { return _gameScene; } }

    public static SceneLoader _instance;
    public static SceneLoader Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this);
        }
    }

    public void LoadMenu()
    {
        _worldStreaming = false;
        _streamingBusy = false;
        _loadedTiles.Clear();
        Manager.OnStartingGame();
        if (SceneManager.GetActiveScene().name != _menuScene)
        {
            SwitchScene(_menuScene, (AsyncOperation o) =>
            {
                StartCoroutine(LoadLobbyAndCacheGame());
            });
            return;
        }

        StartCoroutine(LoadLobbyAndCacheGame());
    }

    private IEnumerator LoadLobbyAndCacheGame()
    {
        Manager.StartLoading();
        yield return LoadSceneAdditiveIfNeeded(_lobbyScene);
        Manager.OnGameLaunched();
        yield return PreloadGameScene();
    }

    private IEnumerator PreloadGameScene()
    {
        yield return LoadSceneAdditiveIfNeeded(_gameScene);
        yield return WaitGameUiBuilt();
        SetGameHudVisible(false);
        SetSceneRootsActive(_gameScene, false, true);
        Debug.Log("Game scene cached (inactive).");
    }

    public void LoadSWMap()
    {
        if (IncomingPacketActions.Game == null || IncomingPacketActions.Game.PlayerInfo.Identity == null)
        {
            Debug.LogError("LoadSWMap: no player xyz");
            return;
        }

        Vector3 l2Pos = IncomingPacketActions.Game.PlayerInfo.Identity.GetL2jPos();
        StartCoroutine(LoadSWMapRoutine(l2Pos));
    }

    private IEnumerator LoadSWMapRoutine(Vector3 l2Pos)
    {
        yield return LoadSceneAdditiveIfNeeded(_gameScene);
        SetSceneRootsActive(_gameScene, true, true);

        Scene game = SceneManager.GetSceneByName(_gameScene);
        if (game.IsValid() && game.isLoaded)
        {
            SceneManager.SetActiveScene(game);
        }

        Manager.OnWorldLoading();
        SetGameHudVisible(true);
        SetSceneRootsActive(_lobbyScene, false);

        string tile = VectorUtils.GetSwMapName(l2Pos);
        Debug.Log("LoadSWMap tile='" + tile + "' xyz=(" + l2Pos.x + ", " + l2Pos.y + ", " + l2Pos.z + ")");

        if (Geodata.Instance != null)
        {
            Geodata.Instance.LoadMaps(new List<string> { tile });
        }

        yield return LoadSceneAdditiveIfNeeded(tile);
        _loadedTiles.Add(tile);

        FinishLoadSWMap();
        UnloadLobbyScenes();
        _worldStreaming = true;
    }

    private void FinishLoadSWMap()
    {
        if (IncomingPacketActions.GameWorld != null)
        {
            Manager.OnWorldSceneLoaded();
        }
    }

    private void Update()
    {
        if (!_worldStreaming || _streamingBusy || PlayerController.Instance == null)
        {
            return;
        }

        Vector3 l2Pos = VectorUtils.ConvertPosUnityToL2j(PlayerController.Instance.transform.position);
        List<string> wanted = ResolveSeamlessTiles(l2Pos);
        if (wanted.Count == 0)
        {
            return;
        }

        for (int i = 0; i < wanted.Count; i++)
        {
            string tile = wanted[i];
            if (_loadedTiles.Contains(tile) || !Application.CanStreamedLevelBeLoaded(tile))
            {
                continue;
            }

            StartCoroutine(EventLoadSWMap(tile));
            return;
        }

        foreach (string loaded in _loadedTiles)
        {
            if (wanted.Contains(loaded))
            {
                continue;
            }

            StartCoroutine(UnloadSWMap(loaded));
            return;
        }
    }

    private List<string> ResolveSeamlessTiles(Vector3 l2Pos)
    {
        int tileX;
        int tileY;
        VectorUtils.GetSwMapTile(l2Pos, out tileX, out tileY);

        List<string> wanted = new List<string>();
        AddTile(wanted, tileX, tileY);

        float mapSize = Geodata.Instance != null ? Geodata.Instance.MapSize : 624.1524f;
        float edge = mapSize > 0f ? _tilePreloadDistance / mapSize : 0.12f;
        float fx = l2Pos.x / VectorUtils.SwTileSizeUu - Mathf.Floor(l2Pos.x / VectorUtils.SwTileSizeUu);
        float fy = l2Pos.y / VectorUtils.SwTileSizeUu - Mathf.Floor(l2Pos.y / VectorUtils.SwTileSizeUu);

        int dx = 0;
        int dy = 0;
        if (fx < edge)
        {
            dx = -1;
        }
        else if (fx > 1f - edge)
        {
            dx = 1;
        }

        if (fy < edge)
        {
            dy = -1;
        }
        else if (fy > 1f - edge)
        {
            dy = 1;
        }

        if (dx != 0)
        {
            AddTile(wanted, tileX + dx, tileY);
        }

        if (dy != 0)
        {
            AddTile(wanted, tileX, tileY + dy);
        }

        if (dx != 0 && dy != 0)
        {
            AddTile(wanted, tileX + dx, tileY + dy);
        }

        return wanted;
    }

    private static void AddTile(List<string> tiles, int tileX, int tileY)
    {
        string name = VectorUtils.FormatSwMapName(tileX, tileY);
        if (!tiles.Contains(name))
        {
            tiles.Add(name);
        }
    }

    private IEnumerator EventLoadSWMap(string tile)
    {
        _streamingBusy = true;
        Debug.Log("EventLoadSWMap tile='" + tile + "'");
        if (Geodata.Instance != null)
        {
            Geodata.Instance.LoadMaps(new List<string> { tile });
        }

        yield return LoadSceneAdditiveIfNeeded(tile);
        _loadedTiles.Add(tile);
        _streamingBusy = false;
    }

    private IEnumerator UnloadSWMap(string tile)
    {
        _streamingBusy = true;
        Debug.Log("UnloadSWMap tile='" + tile + "'");
        SetSceneRootsActive(tile, false);
        UnloadScene(tile);
        _loadedTiles.Remove(tile);
        _streamingBusy = false;
        yield break;
    }

    private IEnumerator LoadSceneAdditiveIfNeeded(string sceneName)
    {
        Scene existing = SceneManager.GetSceneByName(sceneName);
        if (existing.IsValid() && existing.isLoaded)
        {
            Debug.Log("Skipping scene load " + sceneName);
            yield break;
        }

        Debug.Log("Loading scene " + sceneName);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (asyncLoad == null)
        {
            Debug.LogError("Failed to load scene " + sceneName);
            yield break;
        }

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator WaitGameUiBuilt()
    {
        float timeout = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < timeout)
        {
            L2GameUI ui = GetGameUi();
            if (ui != null && ui.AreWindowsReady())
            {
                Debug.Log("Game UI windows ready.");
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning("Game UI windows were not ready before timeout.");
    }

    private void SetGameHudVisible(bool visible)
    {
        L2GameUI ui = GetGameUi();
        if (ui != null)
        {
            ui.SetHudVisible(visible);
        }
    }

    private static L2GameUI GetGameUi()
    {
        if (L2GameUI.Instance != null)
        {
            return L2GameUI.Instance;
        }

        return FindFirstObjectByType<L2GameUI>(FindObjectsInactive.Include);
    }

    private static bool IsGameUiRoot(GameObject root)
    {
        return root != null && root.GetComponentInChildren<L2GameUI>(true) != null;
    }

    private void SetSceneRootsActive(string sceneName, bool active, bool keepGameUi = false)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
            {
                continue;
            }

            if (keepGameUi && IsGameUiRoot(roots[i]))
            {
                continue;
            }

            roots[i].SetActive(active);
        }
    }

    private void UnloadLobbyScenes()
    {
        SetSceneRootsActive(_lobbyScene, false);
        UnloadScene(_lobbyScene);

        Scene loaderScene = gameObject.scene;
        if (loaderScene.IsValid() && loaderScene.name != _menuScene)
        {
            SetSceneRootsActive(_menuScene, false);
            UnloadScene(_menuScene);
        }
    }

    public void SwitchScene(string sceneName, Action<AsyncOperation> p)
    {
        if (SceneManager.GetActiveScene().name != sceneName)
        {
            Debug.Log("Switching to scene " + sceneName);
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.completed += p;
        }
        else
        {
            Debug.Log("Skipping scene switch " + sceneName);
        }
    }

    private void UnloadScene(string sceneName)
    {
        Debug.Log("Unoading scene " + sceneName);

        if (!SceneManager.GetSceneByName(sceneName).IsValid())
        {
            return;
        }

        SceneManager.UnloadSceneAsync(sceneName);
    }
}
