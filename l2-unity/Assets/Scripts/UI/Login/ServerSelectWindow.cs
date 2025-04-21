using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static ServerListPacket;

public class ServerSelectWindow : L2Window
{
    private VisualTreeAsset _serverElementTemplate;
    private VisualElement _listContentContainer;
    private List<VisualElement> _serverElements;
    private List<ServerData> _serverData;
    private int _selectedServerId = -1;
    private ServerData _selectServerData;

    private static ServerSelectWindow _instance;
    public static ServerSelectWindow Instance { get { return _instance; } }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else {
            Destroy(this);
        }
    }

    private void OnDestroy() {
        _instance = null;
    }

    private void Update() {
        if (!_isWindowHidden) {
            if (Input.GetKeyDown(KeyCode.Escape)) {
                AudioManager.Instance.PlayUISound("click_01");
                CancelButtonPressed();
            } else if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return)) {
                AudioManager.Instance.PlayUISound("click_01");
                ConfirmButtonPressed();
            }
        }
    }

    protected override void LoadAssets() {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Login/ServerList/ServerListWindow");
        _serverElementTemplate = LoadAsset("Data/UI/_Elements/Login/ServerList/ServerElement");
    }

    protected override IEnumerator BuildWindow(VisualElement root) {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        Button confirmButton = _windowEle.Q<Button>("ConfirmButton");
        confirmButton.AddManipulator(new ButtonClickSoundManipulator(confirmButton));
        confirmButton.RegisterCallback<ClickEvent>(evt => ConfirmButtonPressed());

        Button cancelButton = _windowEle.Q<Button>("CancelButton");
        cancelButton.AddManipulator(new ButtonClickSoundManipulator(cancelButton));
        cancelButton.RegisterCallback<ClickEvent>(evt => CancelButtonPressed());

        Button nameButton = _windowEle.Q<Button>("Name");
        nameButton.AddManipulator(new ButtonClickSoundManipulator(nameButton));
        Button trafficButton = _windowEle.Q<Button>("Traffic");
        trafficButton.AddManipulator(new ButtonClickSoundManipulator(trafficButton));
        Button characterButton = _windowEle.Q<Button>("Character");
        characterButton.AddManipulator(new ButtonClickSoundManipulator(characterButton));

        _listContentContainer = _windowEle.Q<VisualElement>("ServerContainer");

        _serverElements = new List<VisualElement>();

        root.Add(_windowEle);

        yield return new WaitForEndOfFrame();
    }

    public void UpdateServerList(int lastServer, List<ServerData> serverData, Dictionary<int, int> charsOnServers) {
        ResetWindow();

        _serverData = serverData;

        for (int i = 0; i < serverData.Count; i++) {
            charsOnServers.TryGetValue(serverData[i].serverId, out int charCount);

            AddServerRow(i, ParseServerName(serverData[i].serverId), ParseServerPing(serverData[i].status), ParseServerStatus(serverData[i].status), charCount);

            if (serverData[i].serverId == lastServer) {
                SelectServer(i);
            }
        }

        for(int i = serverData.Count; i < 20; i++) {
            //AddServerRow(i, "", "", "" ,  -1);
        }

        if(lastServer == 0 && _serverData != null && _serverData.Count > 0) {
            SelectServer(0);
        }
    }

    public void SelectServer(int rowId) {
        for (int i = 0; i < _serverElements.Count; i++) {
            _serverElements[i].RemoveFromClassList("selected");
        }

        if(_serverElements.Count - 1 < rowId) {
            return;
        }

        _serverElements[rowId].AddToClassList("selected");

        if(_serverData == null || _serverData.Count == 0 
            || _serverData.Count - 1 < rowId || _serverData[rowId].ip == null) {
            return;
        }

        _selectServerData = _serverData[rowId];
        SetServerId(_serverData[rowId].serverId);

        //Debug.Log("Server selected: " + _selectedServerId);

        GameClient.Instance.ServerIp = StringUtils.ByteArrayToIpAddress(_serverData[rowId].ip);
        GameClient.Instance.ServerPort = _serverData[rowId].port;
    }

    private string ParseServerName(int serverId) {
        return ServerNameDAO.GetServer(serverId);
    }

    private string ParseServerPing(int status) {
        switch (status) {
            case 0: return "Light";
            case 1: return "Good";
            case 2: return "Heavy";
            case 3: return "Full";
            case 4: return "Down";
            case 5: return "GM Only";
            default: return "Unknown";
        }
    }

    private string ParseServerStatus(int status)
    {
        switch (status)
        {
            case 0: return "Down";
            case 1: return "Normal";
            default: return "Unknown";
        }
    }

    private string GetPingClass(string status) {
        switch (status) {
            case "Light": return "light";
            case "Good": return "light";
            case "Heavy": return "heavy";
            case "Full":
            case "Down":
            case "GM Only": return "full";
            default: return "normal";
        }
    }

    private string GetStatusClass(string status)
    {
        switch (status)
        {
            case "Down": return "heavy";
            case "Normal": return "normal";
            default: return "normal";
        }
    }

    private void AddServerRow(int id, string serverName, string ping , string status  , int charCount) {
        VisualElement row = _serverElementTemplate.Instantiate()[0];
        Label serverNameLabel = row.Q<Label>("ServerName");
        Label serverStatusLabel = row.Q<Label>("ServerStatus");
        Label charCountLabel = row.Q<Label>("CharacterCount");
        Label serverStatusJ = row.Q<Label>("ServerStatusJ");

        serverStatusLabel.AddToClassList(GetPingClass(ping));
        serverStatusJ.AddToClassList(GetStatusClass(status));

        serverNameLabel.text = serverName;
        serverStatusLabel.text = ping;

        serverStatusJ.text = status;




        if (charCount >= 0) {
            charCountLabel.text = charCount.ToString();
        } else {
            charCountLabel.text = "";
        }

        if (id % 2 == 1) {
            row.AddToClassList("odd");
        }

        if(charCount >= 0) {
            int rowId = id;
            row.RegisterCallback<ClickEvent>((evt) => {
                SelectServer(rowId);
            });
            row.AddManipulator(new SlotClickSoundManipulator(row));
        }

        _listContentContainer.Add(row);

        _serverElements.Add(row);
    }

    private void ResetWindow() {
        _serverElements.ForEach((x) => {
            x.RemoveFromHierarchy();
        });
        _serverElements.Clear();
        if(_serverData != null) {
            _serverData.Clear();
            _serverData = null;
        }

        SetServerId(-1);
    }

    private void SetServerId(int id) {
        _selectedServerId = id;
        GameClient.Instance.ServerId = id;
    }

    private void ConfirmButtonPressed() {
        if(_selectServerData != null & _selectServerData.status != 0){
            LoginClient.Instance.OnServerSelected(_selectedServerId);
        }
        else
        {
            LoginWindow.Instance.ShowErrorTextOtherThread("Your login attempt failed due to high server load. Please try again later");
            LoginClient.Instance.Disconnect();
        }
    }

    private void CancelButtonPressed() {
        LoginClient.Instance.Disconnect();
    }
}