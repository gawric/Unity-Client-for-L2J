using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

public class HtmlWindow : L2PopupWindow
{
    private Label _titleLabel;
    private static HtmlWindow _instance;
    public static HtmlWindow Instance => _instance;

    private VisualElement _root;
    private VisualElement _content;
    private ScrollView _scrollView;
    private VisualElement _communityTopButtons;

    private bool _isCommunityBoardOpen = false;
    private bool _normalWindowPositionInitialized = false;
    private bool _communityBoardPositionInitialized = false;
    private long _flood = 0;

    private string _normalHtml = "";
    private string _communityHtml = "";

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            Destroy(this);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && CanSend(1100))
        {
            Debug.Log("Requesting : ShowBoard");
            RequestShowBoard packet = CreatorPacketsUser.CreateRequestShowBoard();
            bool crypt = GameClient.Instance.IsCryptEnabled();
            SendGameDataQueue.Instance().AddItem(packet, crypt, crypt);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/HtmlWindow");
    }

    protected override void InitWindow(VisualElement root)
    {
        base.InitWindow(root);

        _root = root;

        VisualElement dragArea = GetElementByClass("drag-area");

        _content = GetElementByClass("content");
        _scrollView = _windowEle.Q<ScrollView>();
        _communityTopButtons = _windowEle.Q<VisualElement>("CommunityTopButtons");

        if (_content == null)
        {
            Debug.LogError("HtmlWindow: content element not found.");
            return;
        }

        if (_communityTopButtons != null)
            _communityTopButtons.style.display = DisplayStyle.None;

        _titleLabel = _windowEle.Q<Label>("windows-name-label");

        _content.style.display = DisplayStyle.Flex;
        _content.style.flexDirection = FlexDirection.Column;
        _content.style.alignItems = Align.Stretch;
        _content.style.justifyContent = Justify.FlexStart;
        _content.style.width = Length.Percent(100);
        _content.style.flexGrow = 1;
        _content.style.flexShrink = 0;

        if (dragArea != null)
            dragArea.AddManipulator(new DragManipulator(dragArea, _windowEle));

        RegisterCloseWindowEvent("btn-close-frame");

        if (dragArea != null)
            RegisterClickWindowEvent(_windowEle, dragArea);

        RegisterCommunityButtons();

        if (!_normalWindowPositionInitialized && _root != null)
        {
            OnCenterScreen(_root);
            _normalWindowPositionInitialized = true;
        }
    }

    private void RegisterCommunityButtons()
    {
        Button btnHome = _windowEle.Q<Button>("CommunityBtnHome");
        Button btnRegion = _windowEle.Q<Button>("CommunityBtnRegion");
        Button btnClan = _windowEle.Q<Button>("CommunityBtnClan");
        Button btnMemo = _windowEle.Q<Button>("CommunityBtnMemo");
        Button btnMail = _windowEle.Q<Button>("CommunityBtnMail");

        if (btnHome != null)
            btnHome.clicked += () => UseActionCommand("_bbshome");

        if (btnRegion != null)
            btnRegion.clicked += () => UseActionCommand("_bbsregion");

        if (btnClan != null)
            btnClan.clicked += () => UseActionCommand("_bbsclan");

        if (btnMemo != null)
            btnMemo.clicked += () => UseActionCommand("_bbsmemo");

        if (btnMail != null)
            btnMail.clicked += () => UseActionCommand("_bbsmail");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();
    }

    public void InjectToWindow(string html)
    {
        _normalHtml = html ?? "";

        ApplyNormalWindowMode();
        RenderHtml(_normalHtml, "");
    }

    public void InjectToCommunityWindow(string html)
    {
        _communityHtml = html ?? "";

        ApplyCommunityBoardMode();
        RenderHtml(_communityHtml, "Community Board");
    }

    private void RenderHtml(string html, string defaultTitle)
    {
        if (_content == null)
            return;

        _content.Clear();

        string title = ExtractTitle(html);
        SetWindowTitle(!string.IsNullOrEmpty(title) ? title : defaultTitle);

        L2HtmlRenderer renderer = new L2HtmlRenderer(_content, OnHtmlAction);
        renderer.Render(html);
    }

    public void ToggleCommunityBoard(bool close)
    {
        if (_windowEle == null)
        {
            Debug.LogError("HtmlWindow: _windowEle is null.");
            return;
        }

        if (close && _isCommunityBoardOpen && _windowEle.style.display == DisplayStyle.Flex)
        {
            _windowEle.style.display = DisplayStyle.None;
            _isCommunityBoardOpen = false;

            if (_mouseOverDetection != null)
                _mouseOverDetection.Disable();

            Debug.Log("Community board closed with B");
            return;
        }

        ApplyCommunityBoardMode();
        RenderHtml(_communityHtml, "Community Board");

        _windowEle.style.display = DisplayStyle.Flex;

        if (_mouseOverDetection != null)
            _mouseOverDetection.Enable();

        BringToFront();

        Debug.Log("Community board opened with B");
    }

    private void ApplyNormalWindowMode()
    {
        _isCommunityBoardOpen = false;

        if (_communityTopButtons != null)
            _communityTopButtons.style.display = DisplayStyle.None;

        if (_scrollView != null)
        {
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _scrollView.style.top = Length.Percent(13);
            _scrollView.style.height = Length.Percent(84);
            _scrollView.style.width = Length.Percent(93);
            _scrollView.style.left = StyleKeyword.Auto;
        }

        if (_windowEle != null)
        {
            _windowEle.style.width = 340;
            _windowEle.style.height = 400;
            _windowEle.style.maxWidth = 340;
            _windowEle.style.maxHeight = StyleKeyword.None;

            _windowEle.style.bottom = StyleKeyword.Auto;
            _windowEle.style.translate = new Translate(0, 0);

            if (!_normalWindowPositionInitialized && _root != null)
            {
                OnCenterScreen(_root);
                _normalWindowPositionInitialized = true;
            }
        }
    }

    private void ApplyCommunityBoardMode()
    {
        _isCommunityBoardOpen = true;

        if (_communityTopButtons != null)
            _communityTopButtons.style.display = DisplayStyle.Flex;

        if (_scrollView != null)
        {
            _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            _scrollView.style.top = 85;
            _scrollView.style.height = Length.Percent(74);
            _scrollView.style.width = Length.Percent(93);
            _scrollView.style.left = Length.Percent(3.5f);
        }

        if (_windowEle != null)
        {
            _windowEle.style.width = 850;
            _windowEle.style.height = 650;
            _windowEle.style.maxWidth = 850;
            _windowEle.style.maxHeight = 650;

            _windowEle.style.bottom = StyleKeyword.Auto;
            _windowEle.style.translate = new Translate(0, 0);

            if (!_communityBoardPositionInitialized && _root != null)
            {
                OnCenterScreen(_root);
                _communityBoardPositionInitialized = true;
            }
        }

        SetWindowTitle("Community Board");
    }

    private string ExtractTitle(string html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        var match = Regex.Match(
            html,
            @"<title>(.*?)</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline
        );

        if (match.Success)
            return match.Groups[1].Value.Trim();

        return null;
    }

    private void OnHtmlAction(string action)
    {
        if (string.IsNullOrEmpty(action))
            return;

        RequestBypassToServer packet = CreatorPacketsUser.CreateByPassPacket(action);
        bool crypt = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(packet, crypt, crypt);
    }

    public void UseActionCommand(string command)
    {
        OnHtmlAction(command);
    }

    public void Show()
    {
        ApplyNormalWindowMode();
        RenderHtml(_normalHtml, "");
        StartCoroutine(ShowCoroutine());
    }

    private IEnumerator ShowCoroutine()
    {
        yield return null;

        _windowEle.style.display = DisplayStyle.Flex;

        if (_mouseOverDetection != null)
            _mouseOverDetection.Enable();

        BringToFront();
    }

    private void SetWindowTitle(string title)
    {
        if (_titleLabel != null)
            _titleLabel.text = title;
    }

    bool CanSend(long cooldownMs)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (now < _flood)
            return false;

        _flood = now + cooldownMs;
        return true;
    }
}
