using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

public class HtmlWindow : L2PopupWindow
{
    private Label _titleLabel;
    private static HtmlWindow _instance;
    public static HtmlWindow Instance => _instance;

    private VisualElement _content;

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else
            Destroy(this);
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

        VisualElement dragArea = GetElementByClass("drag-area");
        _content = GetElementByClass("content");

        if (_content == null)
        {
            Debug.LogError("HtmlWindow: content element not found.");
            return;
        }

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

        OnCenterScreen(root);
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();
    }

    public void InjectToWindow(string html)
    {
        if (_content == null)
            return;

        _content.Clear();

        if (string.IsNullOrWhiteSpace(html))
            return;

        string title = ExtractTitle(html);

        if (!string.IsNullOrEmpty(title))
        {
            SetWindowTitle(title);
        }
        else
            SetWindowTitle("");


        L2HtmlRenderer renderer = new L2HtmlRenderer(_content, OnHtmlAction);
        renderer.Render(html);
    }

    private string ExtractTitle(string html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        var match = Regex.Match(html, @"<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
        StartCoroutine(ShowCoroutine());
    }

    private IEnumerator ShowCoroutine()
    {
        yield return null;

        _windowEle.style.display = DisplayStyle.Flex;
        _mouseOverDetection.Enable();
        BringToFront();
    }

    private void SetWindowTitle(string title)
    {
        if (_titleLabel != null)
        {
            _titleLabel.text = title;
        }
    }
}
