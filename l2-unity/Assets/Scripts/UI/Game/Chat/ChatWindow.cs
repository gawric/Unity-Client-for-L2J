using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ChatWindow : L2Window
{
    private VisualTreeAsset _tabTemplate;
    private VisualTreeAsset _tabHeaderTemplate;
    private VisualTreeAsset _messageLabelTemplate;
    private TextField _chatInput;
    private VisualElement _chatInputContainer;
    private VisualElement _chatTabView;
    private ChatTab _activeTab;

    [SerializeField] private float _chatWindowMinWidth = 225.0f;
    [SerializeField] private float _chatWindowMaxWidth = 500.0f;
    [SerializeField] private float _chatWindowMinHeight = 175.0f;
    [SerializeField] private float _chatWindowMaxHeight = 600.0f;
    [SerializeField] public List<ChatTab> _tabs;
    [SerializeField] private bool _chatOpened = false;
    [SerializeField] private int _chatInputCharacterLimit = 64;



    public bool ChatOpened { get { return _chatOpened; } }

    private static ChatWindow _instance;
    public static ChatWindow Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Chat/ChatWindow");
        _tabTemplate = LoadAsset("Data/UI/_Elements/Game/Chat/ChatTab");
        _tabHeaderTemplate = LoadAsset("Data/UI/_Elements/Game/Chat/ChatTabHeader");
        _messageLabelTemplate = LoadAsset("Data/UI/_Elements/Game/Chat/MessageLabelTemplate");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        var diagonalResizeHandle = GetElementByClass("resize-diag");

        DiagonalResizeManipulator diagonalResizeManipulator = new DiagonalResizeManipulator(
            diagonalResizeHandle,
            _windowEle,
            _chatWindowMinWidth,
            _chatWindowMaxWidth,
            _chatWindowMinHeight,
            _chatWindowMaxHeight,
            14.5f,
            2f);

        diagonalResizeHandle.AddManipulator(diagonalResizeManipulator);

        _chatInput = (TextField)GetElementById("ChatInputField");
        _chatInput.RegisterCallback<FocusEvent>(OnChatInputFocus);
        _chatInput.RegisterCallback<BlurEvent>(OnChatInputBlur);
        _chatInput.maxLength = _chatInputCharacterLimit;

        var enlargeTextBtn = (Button)GetElementById("EnlargeTextBtn");
        enlargeTextBtn.AddManipulator(new ButtonClickSoundManipulator(enlargeTextBtn));

        var chatOptionsBtn = (Button)GetElementById("ChatOptionsBtn");
        chatOptionsBtn.AddManipulator(new ButtonClickSoundManipulator(chatOptionsBtn));

        _chatInput.AddManipulator(new BlinkingCursorManipulator(_chatInput));

        _chatInputContainer = GetElementById("InnerBar");

        CreateTabs();

        yield return new WaitForEndOfFrame();
        diagonalResizeManipulator.SnapSize();

    }


    private void CreateTabs()
    {
        _chatTabView = GetElementById("ChatTabView");

        VisualElement tabHeaderContainer = _chatTabView.Q<VisualElement>("tab-header-container");
        if (tabHeaderContainer == null)
        {
            Debug.LogError("tab-header-container is null");
        }
        VisualElement tabContainer = _chatTabView.Q<VisualElement>("tab-content-container");

        if (tabContainer == null)
        {
            Debug.LogError("tab-content-container");
        }

        for (int i = 0; i < _tabs.Count; i++)
        {
            VisualElement tabElement = _tabTemplate.CloneTree()[0];
            // tabElement.name = _tabs[i].TabName;
            tabElement.name = _tabs[i].TabName;
            tabElement.AddToClassList("unselected-tab");

            VisualElement tabHeaderElement = _tabHeaderTemplate.CloneTree()[0];
            tabHeaderElement.name = _tabs[i].TabName;
            tabHeaderElement.Q<Label>().text = _tabs[i].TabName;

            tabHeaderContainer.Add(tabHeaderElement);
            tabContainer.Add(tabElement);

            _tabs[i].SetMessageTemplate(_messageLabelTemplate);
            _tabs[i].Initialize(_windowEle, tabElement, tabHeaderElement);

        }

        if (_tabs.Count > 0)
        {
            SwitchTab(_tabs[0]);
        }
    }

    public bool SwitchTab(ChatTab switchTo)
    {
        if (_activeTab != switchTo)
        {
            if (_activeTab != null)
            {
                _activeTab.TabContainer.AddToClassList("unselected-tab");
                _activeTab.TabHeader.RemoveFromClassList("active");
            }

            switchTo.TabContainer.RemoveFromClassList("unselected-tab");
            switchTo.TabHeader.AddToClassList("active");
            ScrollDown(switchTo.Scroller);

            _activeTab = switchTo;

            Debug.Log("switching to: " + _activeTab.TabName);
            return true;
        }

        return false;
    }

    void Update()
    {
        if (InputManager.Instance.Validate)
        {
            if (_chatOpened)
            {
                CloseChat(true);
            }
            else
            {
                StartCoroutine(OpenChat());
            }
        }
    }

    IEnumerator OpenChat()
    {
        _chatOpened = true;
        L2GameUI.Instance.BlurFocus();
        yield return new WaitForEndOfFrame();
        _chatInput.Focus();
    }

    public void CloseChat(bool sendMessage)
    {
        _chatOpened = false;

        L2GameUI.Instance.BlurFocus();

        if (sendMessage)
        {
            string text = _chatInput.text;
            if (text.Length > 0)
            {
                if (CanSend(1100))
                {
                    if (IsGmCommand(text))
                    {
                        String prefix = "admin_";
                        string gmCommand = text.Substring(2);
                        string command = prefix + gmCommand;
                        bool enable = GameClient.Instance.IsCryptEnabled();
                        SendGameDataQueue.Instance().AddItem(CreatorPacketsUser.CreateByPassPacket(command), enable, enable);
                        Debug.Log("ChatWindow: requested admin command :" + command);
                    }
                    else
                    {
                        var commands = PlayerCommands.FindByText(text);
                        if (commands != null)
                        {
                            bool enable = GameClient.Instance.IsCryptEnabled();
                            SendGameDataQueue.Instance().AddItem(CreatorPacketsUser.CreateRequestUserCommand(commands.Id), enable, enable);
                            Debug.Log("ChatWindow: requested player command :" + commands  + " id: " + commands.Id);
                        }
                        else
                        {
                            SendChatMessage(_activeTab, text);
                        }
                    }

                    _chatInput.value = "";
                }
                else
                    _activeTab.ConcatMessage("Server:Please dont spam the chat.");
            }
        }
    }

    public static bool IsGmCommand(string text)
    {
        return text != null
            && text.Length > 1
            && text[0] == '/'
            && text[1] == '/';
    }

    private long _flood = 0;

    bool CanSend(long cooldownMs)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (now < _flood)
            return false;

        _flood = now + cooldownMs;
        return true;
    }

    private void OnChatInputFocus(FocusEvent evt)
    {
        if (!_chatInputContainer.ClassListContains("highlighted"))
        {
            _chatInputContainer.AddToClassList("highlighted");
        }

        if (!_chatOpened)
        {
            _chatOpened = true;
        }
    }

    private void OnChatInputBlur(BlurEvent evt)
    {
        if (_chatInputContainer.ClassListContains("highlighted"))
        {
            _chatInputContainer.RemoveFromClassList("highlighted");
        }

        if (_chatOpened)
        {
            _chatOpened = false;
        }
    }

    public void SendChatMessage(ChatTab tab, string text)
    {
        ChatTypeData data = ChatTypes.GetById(tab.TabId);
        if (data != null)
        {
            bool enable = GameClient.Instance.IsCryptEnabled();
            bool whisper = text[0] == '"';

            if (whisper)
            {
                text = text.Substring(1);

                int spaceIndex = text.IndexOf(' ');
                if (spaceIndex <= 0)
                    return;

                string targetName = text.Substring(0, spaceIndex);
                string message = text.Substring(spaceIndex + 1);

                SendGameDataQueue.Instance().AddItem(CreatorPacketsUser.CreateSendWhisperMessage(ChatTypes.GetById(2), message, targetName), enable, enable);
                Debug.Log("whisper chat: sending to :" + targetName + " message: " + message);
            }
            else
            {
                SendGameDataQueue.Instance().AddItem(CreatorPacketsUser.CreateSendMessage(data, text), enable, enable);
            }
        }
        else
        {
            Debug.Log("SendChatMessage : Incorrect data for tab :" + tab.TabId);
        }
    }


    public void ReceiveChatMessage(CreatureMessage data)
    {
        ChatType messageType = (ChatType)data._data.Type;

        Debug.Log("Creature Say: Received:" + data.ToString() + " Type: " + messageType + " ChatTypeId: " + data._data.Type);

        bool isAnnounce = messageType == ChatType.ANNOUNCEMENT || messageType == ChatType.CRITICAL_ANNOUNCE;
        for (int i = 0; i < _tabs.Count; i++)
        {
            ChatTab tab = _tabs[i];

            if (tab.ChatType == messageType || tab.ChatType == ChatType.GENERAL && isAnnounce || tab.ChatType == ChatType.GENERAL && messageType == ChatType.WHISPER)
            {
                tab.ConcatMessage(data.ToString());
            }
        }
    }

    public void ReceiveSystemMessage(SystemMessage message)
    {
        if (message == null)
        {
            return;
        }

        for (int i = 0; i < _tabs.Count; i++)
        {
            //if(_tabs[i].FilteredMessages.Count > 0)
            // {
            // if (_tabs[i].FilteredMessages.Contains(message.MessageType))
            // {
            // ConcatMessage(_tabs[i].Content, message.ToString());
            // }
            // }
            _tabs[i].ConcatMessage(message.ToString());
        }
    }



    internal void ScrollDown(Scroller scroller)
    {
        StartCoroutine(ScrollDownWithDelay(scroller));
    }

    IEnumerator ScrollDownWithDelay(Scroller scroller)
    {
        yield return new WaitForEndOfFrame();
        scroller.value = scroller.highValue > 0 ? scroller.highValue : 0;
    }
}