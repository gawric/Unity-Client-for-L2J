using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

/**
 * @author AbsolutePower
 */
public sealed class L2HtmlRenderer
{
    private static readonly Color LinkColor = new Color32(70, 120, 230, 255);
    private static readonly Color LinkHoverColor = new Color32(110, 160, 255, 255);

    private static readonly Regex MultiSpaceRegex = new Regex(@"[ ]{3,}", RegexOptions.Compiled);
    private static readonly Regex VariableRegex = new Regex(@"\$(\w+)", RegexOptions.Compiled);
    private static readonly Regex PercentVariableRegex = new Regex(@"%(\w+)%", RegexOptions.Compiled);
    private static readonly Regex HexColorRegex = new Regex(@"^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$", RegexOptions.Compiled);

    private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
    private static readonly HashSet<string> MissingTextureCache = new HashSet<string>();

    private readonly VisualElement _root;
    private readonly Action<string> _onAction;

    private readonly Stack<VisualElement> _parentStack = new Stack<VisualElement>();
    private readonly Stack<string> _colorStack = new Stack<string>();
    private readonly Stack<float?> _fontSizeStack = new Stack<float?>();
    private readonly Stack<string> _linkActionStack = new Stack<string>();
    private readonly Stack<bool> _centerStack = new Stack<bool>();
    private readonly Stack<TextAnchor> _textAlignStack = new Stack<TextAnchor>();
    private readonly Stack<float> _cellPaddingStack = new Stack<float>();
    private readonly Stack<float> _cellSpacingStack = new Stack<float>();
    private readonly Stack<bool> _noWrapStack = new Stack<bool>();
    private readonly Stack<bool> _preStack = new Stack<bool>();

    private readonly Stack<bool> _boldStack = new Stack<bool>();
    private readonly Stack<bool> _italicStack = new Stack<bool>();
    private readonly Stack<bool> _underlineStack = new Stack<bool>();

    private readonly Dictionary<string, Func<string>> _valueProviders = new Dictionary<string, Func<string>>();
    private readonly Dictionary<string, RadioButton> _radioGroups = new Dictionary<string, RadioButton>();

    private readonly StringBuilder _textBuffer = new StringBuilder(256);
    private readonly Dictionary<string, string> _attrs = new Dictionary<string, string>(16);

    private string _activeColor;
    private float? _activeFontSize;

    private bool _boldMode;
    private bool _italicMode;
    private bool _underlineMode;
    private bool _noWrapMode;
    private bool _preMode;

    private bool _centerMode;
    private bool _insideHead;
    private bool _insideTitle;

    private TextAnchor _activeTextAlign = TextAnchor.MiddleLeft;

    private float _activeCellPadding = 1f;
    private float _activeCellSpacing = 0f;

    private DropdownField _activeDropdown;
    private string _activeDropdownVar;
    private int _activeDropdownSelectedIndex = -1;
    private readonly List<string> _activeDropdownLabels = new List<string>();
    private readonly List<string> _activeDropdownValues = new List<string>();

    private VisualElement CurrentParent => _parentStack.Count > 0 ? _parentStack.Peek() : _root;
    private string CurrentLinkAction => _linkActionStack.Count > 0 ? _linkActionStack.Peek() : null;

    public L2HtmlRenderer(VisualElement root, Action<string> onAction)
    {
        _root = root;
        _onAction = onAction;
    }

    public void Render(string html)
    {
        ClearState();
        _root.Clear();

        _root.style.overflow = Overflow.Hidden;
        _root.style.flexShrink = 1;
        _root.style.flexGrow = 1;
        _root.style.minWidth = 0;
        _root.style.maxWidth = Length.Percent(100);
        _root.style.width = Length.Percent(100);
        _root.style.alignItems = Align.Stretch;

        if (string.IsNullOrWhiteSpace(html))
            return;

        html = Decode(html);

        int i = 0;

        while (i < html.Length)
        {
            char c = html[i];

            if (c == '<')
            {
                int end = FindTagEnd(html, i + 1);

                if (end < 0)
                    break;

                string rawTag = html.Substring(i + 1, end - i - 1).Trim();

                string tagName = rawTag.StartsWith("/")
                    ? ReadTagName(rawTag.Substring(1).Trim()).ToLowerInvariant()
                    : ReadTagName(rawTag).ToLowerInvariant();

                bool inlineFormat = IsInlineFormatTag(tagName);

                if (!_insideHead && !_insideTitle)
                {
                    if (!inlineFormat)
                        FlushText();
                }
                else
                {
                    _textBuffer.Clear();
                }

                HandleTag(rawTag);

                i = end + 1;
            }
            else
            {
                if (!_insideHead && !_insideTitle)
                    _textBuffer.Append(c);

                i++;
            }
        }

        FlushText();
    }

    private void ClearState()
    {
        _parentStack.Clear();
        _colorStack.Clear();
        _fontSizeStack.Clear();
        _linkActionStack.Clear();
        _centerStack.Clear();
        _textAlignStack.Clear();
        _cellPaddingStack.Clear();
        _cellSpacingStack.Clear();
        _noWrapStack.Clear();
        _preStack.Clear();

        _boldStack.Clear();
        _italicStack.Clear();
        _underlineStack.Clear();

        _valueProviders.Clear();
        _radioGroups.Clear();

        _textBuffer.Clear();

        _activeColor = null;
        _activeFontSize = null;

        _boldMode = false;
        _italicMode = false;
        _underlineMode = false;
        _noWrapMode = false;
        _preMode = false;

        _centerMode = false;
        _insideHead = false;
        _insideTitle = false;

        _activeTextAlign = TextAnchor.MiddleLeft;

        _activeCellPadding = 1f;
        _activeCellSpacing = 0f;

        _activeDropdown = null;
        _activeDropdownVar = null;
        _activeDropdownSelectedIndex = -1;
        _activeDropdownLabels.Clear();
        _activeDropdownValues.Clear();
    }

    private bool IsInlineFormatTag(string tagName)
    {
        return tagName == "font" ||
               tagName == "b" ||
               tagName == "i" ||
               tagName == "u" ||
               tagName == "nobr";
    }

    private int FindTagEnd(string html, int start)
    {
        bool insideQuote = false;
        char quoteChar = '\0';

        for (int i = start; i < html.Length; i++)
        {
            char c = html[i];

            if (insideQuote)
            {
                if (c == quoteChar)
                {
                    insideQuote = false;
                    quoteChar = '\0';
                }

                continue;
            }

            if (c == '"' || c == '\'')
            {
                insideQuote = true;
                quoteChar = c;
                continue;
            }

            if (c == '>')
                return i;
        }

        return -1;
    }

    private void HandleTag(string rawTag)
    {
        if (string.IsNullOrWhiteSpace(rawTag))
            return;

        bool closing = rawTag[0] == '/';

        if (closing)
        {
            string closingName = rawTag.Substring(1).Trim().ToLowerInvariant();

            if (closingName == "head")
            {
                _insideHead = false;
                _textBuffer.Clear();
                return;
            }

            if (closingName == "title")
            {
                _insideTitle = false;
                _textBuffer.Clear();
                return;
            }

            if (_insideHead || _insideTitle)
            {
                _textBuffer.Clear();
                return;
            }

            switch (closingName)
            {
                case "div":
                case "p":
                case "tr":
                case "td":
                    PopParent();
                    PopTextAlign();
                    break;

                case "table":
                    PopParent();

                    if (_cellPaddingStack.Count > 0)
                        _activeCellPadding = _cellPaddingStack.Pop();

                    if (_cellSpacingStack.Count > 0)
                        _activeCellSpacing = _cellSpacingStack.Pop();

                    break;

                case "center":
                    PopParent();
                    _centerMode = _centerStack.Count > 0 && _centerStack.Pop();
                    PopTextAlign();
                    break;

                case "font":
                    if (_activeFontSize.HasValue)
                        _textBuffer.Append("</size>");

                    if (!string.IsNullOrEmpty(_activeColor))
                        _textBuffer.Append("</color>");

                    _activeColor = _colorStack.Count > 0 ? _colorStack.Pop() : null;
                    _activeFontSize = _fontSizeStack.Count > 0 ? _fontSizeStack.Pop() : null;
                    break;

                case "a":
                    if (_linkActionStack.Count > 0)
                        _linkActionStack.Pop();
                    break;

                case "b":
                    _textBuffer.Append("</b>");
                    _boldMode = _boldStack.Count > 0 && _boldStack.Pop();
                    break;

                case "i":
                    _textBuffer.Append("</i>");
                    _italicMode = _italicStack.Count > 0 && _italicStack.Pop();
                    break;

                case "u":
                    _textBuffer.Append("</u>");
                    _underlineMode = _underlineStack.Count > 0 && _underlineStack.Pop();
                    break;

                case "nobr":
                    _noWrapMode = _noWrapStack.Count > 0 && _noWrapStack.Pop();
                    break;

                case "pre":
                    _preMode = _preStack.Count > 0 && _preStack.Pop();
                    break;

                case "select":
                case "combobox":
                    FinishDropdown();
                    break;

                case "option":
                    FinishDropdownOptionText();
                    break;
            }

            return;
        }

        string tagName = ReadTagName(rawTag).ToLowerInvariant();

        if (tagName == "head")
        {
            _insideHead = true;
            _textBuffer.Clear();
            return;
        }

        if (tagName == "title")
        {
            _insideTitle = true;
            _textBuffer.Clear();
            return;
        }

        ReadAttributes(rawTag, _attrs);

        if (tagName == "body")
        {
            ApplyBackground(_root);
            ApplyTooltip(_root);
            return;
        }

        if (_insideHead || _insideTitle)
        {
            _textBuffer.Clear();
            return;
        }

        if (tagName == "html")
            return;

        switch (tagName)
        {
            case "br":
            case "br1":
                AddBreak();
                break;

            case "hr":
                AddHr();
                break;

            case "center":
                AddCenter();
                break;

            case "p":
                AddParagraph();
                break;

            case "pre":
                _preStack.Push(_preMode);
                _preMode = true;
                break;

            case "nobr":
                _noWrapStack.Push(_noWrapMode);
                _noWrapMode = true;
                break;

            case "font":
                {
                    _colorStack.Push(_activeColor);
                    _fontSizeStack.Push(_activeFontSize);

                    string newColor = NormalizeColor(GetAttr("color", GetAttr("value", _activeColor ?? "#DCD9DC")));
                    _activeColor = newColor;

                    _textBuffer.Append("<color=");
                    _textBuffer.Append(newColor);
                    _textBuffer.Append(">");

                    if (TryFloat(GetAttr("size", ""), out float size))
                    {
                        _activeFontSize = ConvertFontSize(size);

                        _textBuffer.Append("<size=");
                        _textBuffer.Append(Mathf.RoundToInt(_activeFontSize.Value));
                        _textBuffer.Append(">");
                    }

                    break;
                }

            case "b":
                _boldStack.Push(_boldMode);
                _boldMode = true;
                _textBuffer.Append("<b>");
                break;

            case "i":
                _italicStack.Push(_italicMode);
                _italicMode = true;
                _textBuffer.Append("<i>");
                break;

            case "u":
                _underlineStack.Push(_underlineMode);
                _underlineMode = true;
                _textBuffer.Append("<u>");
                break;

            case "a":
                _linkActionStack.Push(GetAttr("action", GetAttr("href", "")));
                break;

            case "div":
                AddDiv();
                break;

            case "table":
                AddTable();
                break;

            case "tr":
                AddRow();
                break;

            case "td":
                AddCell();
                break;

            case "multiedit":
                AddMultiEdit();
                break;

            case "edit":
            case "input":
                AddInput();
                break;

            case "button":
                AddButton();
                break;

            case "img":
                AddImage();
                break;

            case "select":
            case "combobox":
                StartDropdown();
                break;

            case "option":
                AddDropdownOption();
                break;

            case "checkbox":
                AddCheckbox();
                break;

            case "radio":
                AddRadio();
                break;
        }
    }

    private void FlushText()
    {
        if (_textBuffer.Length == 0)
            return;

        if (_activeDropdown != null)
            return;

        string text = _preMode
            ? _textBuffer.ToString()
            : NormalizeL2Text(_textBuffer.ToString());

        _textBuffer.Clear();

        if (string.IsNullOrWhiteSpace(text))
            return;

        bool isLink = !string.IsNullOrEmpty(CurrentLinkAction);
        bool hasRichColor = text.Contains("<color=");

        Label label = new Label();

        label.text = isLink ? "<u>" + text + "</u>" : text;
        label.enableRichText = true;

        label.AddToClassList(isLink ? "html_link" : "html_text");

        label.style.color = isLink && !hasRichColor ? LinkColor : Color.white;
        label.style.fontSize = 13;
        label.style.flexShrink = 1;
        label.style.minWidth = 0;
        label.style.maxWidth = Length.Percent(100);

        label.style.letterSpacing = 0;

        label.style.whiteSpace = (_preMode || _noWrapMode)
            ? WhiteSpace.NoWrap
            : WhiteSpace.Normal;

        label.style.unityTextAlign = _activeTextAlign;

        label.style.marginTop = 0;
        label.style.marginBottom = 0;
        label.style.paddingTop = 1;
        label.style.paddingBottom = 1;

        if (isLink)
        {
            string action = CurrentLinkAction;

            label.pickingMode = PickingMode.Position;
            label.focusable = true;

            label.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!hasRichColor)
                    label.style.color = LinkHoverColor;
            });

            label.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (!hasRichColor)
                    label.style.color = LinkColor;
            });

            label.RegisterCallback<ClickEvent>(evt =>
            {
                string finalAction = PrepareAction(action);

                if (!string.IsNullOrEmpty(finalAction))
                    _onAction?.Invoke(finalAction);

                evt.StopPropagation();
            });
        }

        CurrentParent.Add(label);
    }

    private void AddBreak()
    {
        VisualElement br = new VisualElement();

        br.AddToClassList("html_br");
        br.style.height = 6;
        br.style.flexShrink = 0;
        ResetBoxSpacing(br);

        CurrentParent.Add(br);
    }

    private void AddHr()
    {
        VisualElement hr = new VisualElement();

        hr.AddToClassList("html_hr");

        hr.style.height = TryFloat(GetAttr("height", ""), out float h) ? h : 1;
        hr.style.width = Length.Percent(100);
        hr.style.maxWidth = Length.Percent(100);
        hr.style.flexShrink = 1;

        Color color = new Color32(90, 90, 90, 255);

        string colorAttr = GetAttr("color", GetAttr("bordercolor", ""));

        if (!string.IsNullOrEmpty(colorAttr))
        {
            string normalized = NormalizeColor(colorAttr);

            if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
                color = parsed;
        }

        hr.style.backgroundColor = color;
        hr.style.marginTop = 3;
        hr.style.marginBottom = 3;
        hr.style.marginLeft = 0;
        hr.style.marginRight = 0;

        CurrentParent.Add(hr);
    }

    private void AddCenter()
    {
        _centerStack.Push(_centerMode);
        _centerMode = true;

        PushTextAlign(TextAnchor.MiddleCenter);

        VisualElement center = CreateContainer();

        center.AddToClassList("html_center");
        center.style.flexDirection = FlexDirection.Column;
        center.style.alignItems = Align.Center;
        center.style.justifyContent = Justify.FlexStart;
        center.style.width = Length.Percent(100);
        center.style.maxWidth = Length.Percent(100);
        center.style.minWidth = 0;
        center.style.flexShrink = 1;

        CurrentParent.Add(center);
        _parentStack.Push(center);
    }

    private void AddParagraph()
    {
        VisualElement p = CreateContainer();

        p.AddToClassList("html_p");

        p.style.flexDirection = FlexDirection.Column;
        p.style.width = Length.Percent(100);
        p.style.maxWidth = Length.Percent(100);
        p.style.minWidth = 0;
        p.style.flexShrink = 1;

        string align = GetAttr("align", _centerMode ? "center" : "left");

        ApplyAlign(p, align);
        PushTextAlign(ParseTextAlign(align));

        ApplyJustify(p, GetAttr("justify", "start"));
        ApplySize(p);
        PreventOverflow(p);
        ApplyBackground(p);
        ApplyBorder(p);
        ApplyBoxSpacing(p);
        ApplyClasses(p);
        ApplyTooltip(p);

        CurrentParent.Add(p);
        _parentStack.Push(p);
    }

    private void AddDiv()
    {
        VisualElement div = CreateContainer();

        div.AddToClassList("html_div");

        string flex = GetAttr("flex", "column");
        div.style.flexDirection = flex == "row" ? FlexDirection.Row : FlexDirection.Column;

        string align = GetAttr("align", _centerMode ? "center" : "start");

        ApplyAlign(div, align);
        PushTextAlign(ParseTextAlign(align));

        ApplyJustify(div, GetAttr("justify", "start"));
        ApplySize(div);
        PreventOverflow(div);
        ApplyBackground(div);
        ApplyBorder(div);
        ApplyBoxSpacing(div);
        ApplyClasses(div);
        ApplyTooltip(div);

        CurrentParent.Add(div);
        _parentStack.Push(div);
    }

    private void AddTable()
    {
        _cellPaddingStack.Push(_activeCellPadding);
        _cellSpacingStack.Push(_activeCellSpacing);

        if (TryFloat(GetAttr("cellpadding", ""), out float padding))
            _activeCellPadding = padding;

        if (TryFloat(GetAttr("cellspacing", ""), out float spacing))
            _activeCellSpacing = spacing;

        VisualElement table = CreateContainer();

        table.AddToClassList("html_table");

        table.style.overflow = Overflow.Visible;
        table.style.flexDirection = FlexDirection.Column;
        table.style.alignItems = Align.Stretch;
        table.style.justifyContent = Justify.FlexStart;

        table.style.maxWidth = Length.Percent(100);
        table.style.minWidth = 0;
        table.style.flexShrink = 1;

        if (!_attrs.ContainsKey("width") && !_attrs.ContainsKey("fixwidth"))
            table.style.width = Length.Percent(100);

        ApplySize(table);
        PreventOverflow(table);
        ApplyTableAlign(table);
        ApplyBackground(table);
        ApplyBorder(table);
        ApplyBoxSpacing(table);
        ApplyClasses(table);
        ApplyTooltip(table);

        CurrentParent.Add(table);
        _parentStack.Push(table);
    }

    private void AddRow()
    {
        VisualElement row = CreateContainer();

        row.AddToClassList("html_tr");

        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.FlexStart;
        row.style.width = Length.Percent(100);
        row.style.maxWidth = Length.Percent(100);
        row.style.minWidth = 0;
        row.style.flexShrink = 1;
        row.style.flexWrap = Wrap.NoWrap;

        ApplySize(row);
        PreventOverflow(row);
        ApplyBackground(row);
        ApplyBorder(row);
        ApplyBoxSpacing(row);
        ApplyClasses(row);
        ApplyTooltip(row);

        PushTextAlign(_activeTextAlign);

        CurrentParent.Add(row);
        _parentStack.Push(row);
    }

    private void AddCell()
    {
        VisualElement cell = CreateContainer();

        cell.AddToClassList("html_td");

        cell.style.flexDirection = FlexDirection.Column;

        bool fixedWidth = _attrs.ContainsKey("fixwidth") || _attrs.ContainsKey("width");

        if (fixedWidth)
        {
            cell.style.flexGrow = 0;
            cell.style.flexShrink = 1;
            cell.style.minWidth = 0;
            cell.style.maxWidth = Length.Percent(100);
        }
        else
        {
            cell.style.flexGrow = 1;
            cell.style.flexShrink = 1;
            cell.style.minWidth = 0;
        }

        cell.style.minWidth = 0;
        cell.style.maxWidth = Length.Percent(100);
        cell.style.flexDirection = FlexDirection.Row;
        cell.style.flexWrap = Wrap.NoWrap;

        float padding = _activeCellPadding;

        if (TryFloat(GetAttr("padding", ""), out float customPadding))
            padding = customPadding;

        cell.style.overflow = Overflow.Visible;
        cell.style.paddingLeft = padding;
        cell.style.paddingRight = padding;
        cell.style.paddingTop = padding;
        cell.style.paddingBottom = padding;

        if (_activeCellSpacing > 0)
        {
            cell.style.marginLeft = _activeCellSpacing;
            cell.style.marginRight = _activeCellSpacing;
            cell.style.marginTop = _activeCellSpacing;
            cell.style.marginBottom = _activeCellSpacing;
        }

        string align = GetAttr("align", _centerMode ? "center" : "start");

        ApplyAlign(cell, align);
        PushTextAlign(ParseTextAlign(align));
        ApplyValign(cell, GetAttr("valign", "middle"));

        bool previousNoWrap = _noWrapMode;

        if (_attrs.ContainsKey("nowrap") || IsTruthy(GetAttr("nowrap", "")))
            _noWrapMode = true;

        ApplySize(cell);

        if (TryInt(GetAttr("colspan", ""), out int colspan) && colspan > 1)
            cell.style.flexGrow = colspan;

        PreventOverflow(cell);
        ApplyBackground(cell);
        ApplyBorder(cell);
        ApplyBoxSpacing(cell, false);
        ApplyClasses(cell);
        ApplyTooltip(cell);

        cell.RegisterCallback<DetachFromPanelEvent>(_ =>
        {
            _noWrapMode = previousNoWrap;
        });

        CurrentParent.Add(cell);
        _parentStack.Push(cell);
    }

    private void AddMultiEdit()
    {
        TextField input = new TextField
        {
            multiline = true
        };

        input.AddToClassList("html_multiedit");

        input.pickingMode = PickingMode.Position;
        input.focusable = true;
        input.isDelayed = false;

        string name = GetAttr("var", GetAttr("name", ""));

        if (!string.IsNullOrEmpty(name))
            _valueProviders[name] = () => input.value ?? string.Empty;

        if (TryFloat(GetAttr("width", ""), out float width))
            input.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            input.style.height = height;

        if (TryInt(GetAttr("maxlength", ""), out int maxLength))
            input.maxLength = maxLength;

        input.value = GetAttr("value", "");

        if (IsTruthy(GetAttr("readonly", "")))
            input.isReadOnly = true;

        ResetBoxSpacing(input);
        PreventOverflow(input);
        ApplyTooltip(input);
        ApplyDisabled(input);

        CurrentParent.Add(input);
    }

    private void AddInput()
    {
        TextField input = new TextField();

        input.AddToClassList("html_input");

        string name = GetAttr("var", GetAttr("name", ""));

        if (!string.IsNullOrEmpty(name))
            _valueProviders[name] = () => input.value ?? string.Empty;

        if (TryFloat(GetAttr("width", ""), out float width))
            input.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            input.style.height = height;

        if (TryInt(GetAttr("maxlength", ""), out int maxLength))
            input.maxLength = maxLength;

        input.value = GetAttr("value", "");

        if (IsTruthy(GetAttr("readonly", "")))
            input.isReadOnly = true;

        if (GetAttr("type", "").ToLowerInvariant() == "password")
            input.isPasswordField = true;

        ResetBoxSpacing(input);
        PreventOverflow(input);
        ApplyTooltip(input);
        ApplyDisabled(input);

        CurrentParent.Add(input);
    }

    private void AddButton()
    {
        string value = GetAttr("value", "Button");
        string action = GetAttr("action", "");

        Button button = new Button();

        button.text = value;
        button.name = value;
        button.AddToClassList("html_button");

        button.pickingMode = PickingMode.Position;
        button.focusable = true;

        if (TryFloat(GetAttr("width", ""), out float width))
            button.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            button.style.height = height;

        string fore = GetAttr("fore", "");
        string back = GetAttr("back", GetAttr("background", ""));

        Texture2D foreTexture = LoadTexture(fore);
        Texture2D backTexture = LoadTexture(back);

        if (foreTexture != null)
            button.style.backgroundImage = new StyleBackground(foreTexture);
        else if (backTexture != null)
            button.style.backgroundImage = new StyleBackground(backTexture);

        if (foreTexture != null || backTexture != null)
        {
            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (backTexture != null)
                    button.style.backgroundImage = new StyleBackground(backTexture);
            });

            button.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (foreTexture != null)
                    button.style.backgroundImage = new StyleBackground(foreTexture);
                else if (backTexture != null)
                    button.style.backgroundImage = new StyleBackground(backTexture);
            });
        }

        ResetBoxSpacing(button);
        PreventOverflow(button);
        ApplyTooltip(button);
        ApplyDisabled(button);

        button.clicked += () =>
        {
            if (!button.enabledSelf)
                return;

            string finalAction = PrepareAction(action);

            if (!string.IsNullOrEmpty(finalAction))
                _onAction?.Invoke(finalAction);
        };

        CurrentParent.Add(button);
    }

    private void AddImage()
    {
        string src = GetAttr("src", "");

        if (string.IsNullOrEmpty(src))
            return;

        Image image = new Image();

        image.AddToClassList("html_image");

        Texture2D texture = LoadTexture(src);

        if (texture != null)
            image.image = texture;

        if (TryFloat(GetAttr("width", ""), out float width))
            image.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            image.style.height = height;

        ResetBoxSpacing(image);
        PreventOverflow(image);
        ApplyTooltip(image);

        string action = GetAttr("action", GetAttr("href", ""));

        if (!string.IsNullOrEmpty(action))
        {
            image.pickingMode = PickingMode.Position;
            image.focusable = true;

            image.RegisterCallback<ClickEvent>(evt =>
            {
                string finalAction = PrepareAction(action);

                if (!string.IsNullOrEmpty(finalAction))
                    _onAction?.Invoke(finalAction);

                evt.StopPropagation();
            });
        }

        CurrentParent.Add(image);
    }

    private void StartDropdown()
    {
        _activeDropdown = new DropdownField();
        _activeDropdown.AddToClassList("html_dropdown");

        _activeDropdownVar = GetAttr("var", GetAttr("name", ""));
        _activeDropdownSelectedIndex = -1;

        _activeDropdownLabels.Clear();
        _activeDropdownValues.Clear();
        _textBuffer.Clear();

        if (TryFloat(GetAttr("width", ""), out float width))
            _activeDropdown.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            _activeDropdown.style.height = height;

        string selected = GetAttr("selected", "");

        if (TryInt(selected, out int selectedIndex))
            _activeDropdownSelectedIndex = selectedIndex;

        ResetBoxSpacing(_activeDropdown);
        PreventOverflow(_activeDropdown);
        ApplyTooltip(_activeDropdown);
        ApplyDisabled(_activeDropdown);
    }

    private void AddDropdownOption()
    {
        if (_activeDropdown == null)
            return;

        string value = GetAttr("value", "");
        string label = GetAttr("label", "");

        _activeDropdownLabels.Add(label);
        _activeDropdownValues.Add(value);

        if (IsTruthy(GetAttr("selected", "")))
            _activeDropdownSelectedIndex = _activeDropdownLabels.Count - 1;

        _textBuffer.Clear();
    }

    private void FinishDropdownOptionText()
    {
        if (_activeDropdown == null || _activeDropdownLabels.Count == 0)
            return;

        int index = _activeDropdownLabels.Count - 1;

        string label = _activeDropdownLabels[index];
        string value = _activeDropdownValues[index];
        string innerText = NormalizeL2Text(_textBuffer.ToString()).Trim();

        _textBuffer.Clear();

        if (string.IsNullOrEmpty(label))
            label = !string.IsNullOrEmpty(innerText) ? innerText : value;

        if (string.IsNullOrEmpty(value))
            value = label;

        _activeDropdownLabels[index] = label;
        _activeDropdownValues[index] = value;
    }

    private void FinishDropdown()
    {
        if (_activeDropdown == null)
            return;

        FinishDropdownOptionText();

        _activeDropdown.choices = new List<string>(_activeDropdownLabels);

        if (_activeDropdownLabels.Count > 0)
        {
            int index = Mathf.Clamp(_activeDropdownSelectedIndex, 0, _activeDropdownLabels.Count - 1);
            _activeDropdown.value = _activeDropdownLabels[index];
        }

        if (!string.IsNullOrEmpty(_activeDropdownVar))
        {
            string varName = _activeDropdownVar;
            DropdownField dropdown = _activeDropdown;
            List<string> labels = new List<string>(_activeDropdownLabels);
            List<string> values = new List<string>(_activeDropdownValues);

            _valueProviders[varName] = () =>
            {
                int index = labels.IndexOf(dropdown.value);

                if (index >= 0 && index < values.Count)
                    return values[index];

                return dropdown.value ?? string.Empty;
            };
        }

        CurrentParent.Add(_activeDropdown);

        _activeDropdown = null;
        _activeDropdownVar = null;
        _activeDropdownSelectedIndex = -1;
        _activeDropdownLabels.Clear();
        _activeDropdownValues.Clear();
        _textBuffer.Clear();
    }

    private void AddCheckbox()
    {
        Toggle toggle = new Toggle();

        toggle.AddToClassList("html_checkbox");

        string label = GetAttr("label", GetAttr("value", ""));
        string name = GetAttr("var", GetAttr("name", ""));

        toggle.text = label;

        if (IsTruthy(GetAttr("checked", "")))
            toggle.value = true;

        if (!string.IsNullOrEmpty(name))
        {
            string checkedValue = GetAttr("value", "1");
            string uncheckedValue = GetAttr("unchecked", "0");

            _valueProviders[name] = () => toggle.value ? checkedValue : uncheckedValue;
        }

        if (TryFloat(GetAttr("width", ""), out float width))
            toggle.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            toggle.style.height = height;

        ResetBoxSpacing(toggle);
        PreventOverflow(toggle);
        ApplyTooltip(toggle);
        ApplyDisabled(toggle);

        CurrentParent.Add(toggle);
    }

    private void AddRadio()
    {
        RadioButton radio = new RadioButton();

        radio.AddToClassList("html_radio");

        string group = GetAttr("var", GetAttr("name", ""));
        string value = GetAttr("value", "");
        string label = GetAttr("label", value);

        radio.text = label;

        if (IsTruthy(GetAttr("checked", "")))
            radio.value = true;

        if (!string.IsNullOrEmpty(group))
        {
            radio.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue)
                    return;

                foreach (KeyValuePair<string, RadioButton> pair in _radioGroups)
                {
                    if (pair.Key.StartsWith(group + ":", StringComparison.Ordinal) && pair.Value != radio)
                        pair.Value.value = false;
                }
            });

            _radioGroups[group + ":" + value] = radio;

            _valueProviders[group] = () =>
            {
                foreach (KeyValuePair<string, RadioButton> pair in _radioGroups)
                {
                    if (pair.Key.StartsWith(group + ":", StringComparison.Ordinal) && pair.Value.value)
                        return pair.Key.Substring(group.Length + 1);
                }

                return string.Empty;
            };
        }

        ResetBoxSpacing(radio);
        PreventOverflow(radio);
        ApplyTooltip(radio);
        ApplyDisabled(radio);

        CurrentParent.Add(radio);
    }

    private VisualElement CreateContainer()
    {
        VisualElement ve = new VisualElement();

        ve.style.display = DisplayStyle.Flex;
        ve.style.visibility = Visibility.Visible;

        ve.style.flexGrow = 0;
        ve.style.flexShrink = 1;
        ve.style.minWidth = 0;
        ve.style.maxWidth = Length.Percent(100);

        ResetBoxSpacing(ve);

        return ve;
    }

    private void ResetBoxSpacing(VisualElement ve)
    {
        ve.style.marginTop = 0;
        ve.style.marginBottom = 0;
        ve.style.marginLeft = 0;
        ve.style.marginRight = 0;

        ve.style.paddingTop = 0;
        ve.style.paddingBottom = 0;
        ve.style.paddingLeft = 0;
        ve.style.paddingRight = 0;

        ve.style.minHeight = 0;
    }

    private void PreventOverflow(VisualElement ve)
    {
        ve.style.maxWidth = Length.Percent(100);
        ve.style.minWidth = 0;
        ve.style.flexShrink = 1;
    }

    private void PopParent()
    {
        if (_parentStack.Count > 0)
            _parentStack.Pop();
    }

    private void PushTextAlign(TextAnchor align)
    {
        _textAlignStack.Push(_activeTextAlign);
        _activeTextAlign = align;
    }

    private void PopTextAlign()
    {
        _activeTextAlign = _textAlignStack.Count > 0
            ? _textAlignStack.Pop()
            : TextAnchor.MiddleLeft;
    }

    private void ApplyTableAlign(VisualElement ve)
    {
        string align = GetAttr("align", "").ToLowerInvariant();

        if (align == "center")
            ve.style.alignSelf = Align.Center;
        else if (align == "right" || align == "end")
            ve.style.alignSelf = Align.FlexEnd;
        else if (align == "left" || align == "start")
            ve.style.alignSelf = Align.FlexStart;
    }

    private void ApplyAlign(VisualElement ve, string align)
    {
        switch (align.ToLowerInvariant())
        {
            case "center":
                ve.style.alignItems = Align.Center;
                break;

            case "right":
            case "end":
                ve.style.alignItems = Align.FlexEnd;
                break;

            case "stretch":
                ve.style.alignItems = Align.Stretch;
                break;

            default:
                ve.style.alignItems = Align.FlexStart;
                break;
        }
    }

    private void ApplyValign(VisualElement ve, string valign)
    {
        switch (valign.ToLowerInvariant())
        {
            case "top":
                ve.style.justifyContent = Justify.FlexStart;
                break;

            case "bottom":
                ve.style.justifyContent = Justify.FlexEnd;
                break;

            case "middle":
            case "center":
                ve.style.justifyContent = Justify.Center;
                break;

            default:
                ve.style.justifyContent = Justify.FlexStart;
                break;
        }
    }

    private void ApplyJustify(VisualElement ve, string justify)
    {
        switch (justify.ToLowerInvariant())
        {
            case "center":
                ve.style.justifyContent = Justify.Center;
                break;

            case "between":
                ve.style.justifyContent = Justify.SpaceBetween;
                break;

            case "around":
                ve.style.justifyContent = Justify.SpaceAround;
                break;

            case "end":
            case "right":
                ve.style.justifyContent = Justify.FlexEnd;
                break;

            default:
                ve.style.justifyContent = Justify.FlexStart;
                break;
        }
    }

    private void ApplySize(VisualElement ve)
    {
        string width = GetAttr("fixwidth", GetAttr("width", ""));

        if (TryFloat(width, out float w))
        {
            ve.style.width = w;
            ve.style.maxWidth = Length.Percent(100);
            ve.style.minWidth = 0;
            ve.style.flexShrink = 1;

            if (_attrs.ContainsKey("fixwidth"))
            {
                ve.style.flexGrow = 0;
                ve.style.flexShrink = 1;
                ve.style.minWidth = 0;
                ve.style.maxWidth = Length.Percent(100);
            }
        }

        if (TryFloat(GetAttr("height", ""), out float h))
            ve.style.height = h;

        if (TryFloat(GetAttr("minwidth", ""), out float minW))
            ve.style.minWidth = minW;

        if (TryFloat(GetAttr("maxwidth", ""), out float maxW))
            ve.style.maxWidth = maxW;

        if (TryFloat(GetAttr("minheight", ""), out float minH))
            ve.style.minHeight = minH;

        if (TryFloat(GetAttr("maxheight", ""), out float maxH))
            ve.style.maxHeight = maxH;
    }

    private void ApplyBackground(VisualElement ve)
    {
        string bgColor = GetAttr("bgcolor", GetAttr("background-color", ""));

        if (!string.IsNullOrEmpty(bgColor))
        {
            string normalized = NormalizeColor(bgColor);

            if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
                ve.style.backgroundColor = parsed;
        }

        string background = GetAttr("background", GetAttr("bg", ""));

        if (string.IsNullOrEmpty(background))
            return;

        Texture2D texture = LoadTexture(background);

        if (texture != null)
            ve.style.backgroundImage = new StyleBackground(texture);
    }

    private void ApplyBorder(VisualElement ve)
    {
        string border = GetAttr("border", "");

        if (!TryFloat(border, out float width) || width <= 0)
            return;

        Color color = new Color32(90, 90, 90, 255);

        string borderColor = GetAttr("bordercolor", GetAttr("border-color", ""));

        if (!string.IsNullOrEmpty(borderColor))
        {
            string normalized = NormalizeColor(borderColor);

            if (ColorUtility.TryParseHtmlString(normalized, out Color parsed))
                color = parsed;
        }

        ve.style.borderTopWidth = width;
        ve.style.borderBottomWidth = width;
        ve.style.borderLeftWidth = width;
        ve.style.borderRightWidth = width;

        ve.style.borderTopColor = color;
        ve.style.borderBottomColor = color;
        ve.style.borderLeftColor = color;
        ve.style.borderRightColor = color;
    }

    private void ApplyBoxSpacing(VisualElement ve, bool allowPaddingOverride = true)
    {
        if (TryFloat(GetAttr("margin", ""), out float margin))
        {
            ve.style.marginTop = margin;
            ve.style.marginBottom = margin;
            ve.style.marginLeft = margin;
            ve.style.marginRight = margin;
        }

        if (TryFloat(GetAttr("margintop", ""), out float mt))
            ve.style.marginTop = mt;

        if (TryFloat(GetAttr("marginbottom", ""), out float mb))
            ve.style.marginBottom = mb;

        if (TryFloat(GetAttr("marginleft", ""), out float ml))
            ve.style.marginLeft = ml;

        if (TryFloat(GetAttr("marginright", ""), out float mr))
            ve.style.marginRight = mr;

        if (allowPaddingOverride && TryFloat(GetAttr("padding", ""), out float padding))
        {
            ve.style.paddingTop = padding;
            ve.style.paddingBottom = padding;
            ve.style.paddingLeft = padding;
            ve.style.paddingRight = padding;
        }

        if (allowPaddingOverride && TryFloat(GetAttr("paddingtop", ""), out float pt))
            ve.style.paddingTop = pt;

        if (allowPaddingOverride && TryFloat(GetAttr("paddingbottom", ""), out float pb))
            ve.style.paddingBottom = pb;

        if (allowPaddingOverride && TryFloat(GetAttr("paddingleft", ""), out float pl))
            ve.style.paddingLeft = pl;

        if (allowPaddingOverride && TryFloat(GetAttr("paddingright", ""), out float pr))
            ve.style.paddingRight = pr;
    }

    private void ApplyClasses(VisualElement ve)
    {
        string className = GetAttr("class", "");

        if (string.IsNullOrEmpty(className))
            return;

        string[] classes = className.Split(' ');

        foreach (string cls in classes)
        {
            if (!string.IsNullOrWhiteSpace(cls))
                ve.AddToClassList(cls);
        }
    }

    private void ApplyTooltip(VisualElement ve)
    {
        string tooltip = GetAttr("tooltip", GetAttr("title", ""));

        if (!string.IsNullOrEmpty(tooltip))
            ve.tooltip = tooltip;
    }

    private void ApplyDisabled(VisualElement ve)
    {
        string disabled = GetAttr("disabled", "");

        if (IsTruthy(disabled))
            ve.SetEnabled(false);
    }

    private Texture2D LoadTexture(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (TextureCache.TryGetValue(path, out Texture2D cached))
            return cached;

        if (MissingTextureCache.Contains(path))
            return null;

        Texture2D texture = Resources.Load<Texture2D>(path);

        if (texture != null)
        {
            TextureCache[path] = texture;
            return texture;
        }

        MissingTextureCache.Add(path);
        Debug.LogWarning("L2HtmlRenderer: texture not found in Resources: " + path);
        return null;
    }

    private string PrepareAction(string action)
    {
        if (string.IsNullOrEmpty(action))
            return string.Empty;

        action = action.Replace("bypass", "").Replace("-h", "").Trim();

        return ReplaceVariables(action);
    }

    private string ReplaceVariables(string action)
    {
        action = VariableRegex.Replace(action, match =>
        {
            string key = match.Groups[1].Value;

            if (_valueProviders.TryGetValue(key, out Func<string> provider))
                return provider.Invoke();

            return match.Value;
        });

        action = PercentVariableRegex.Replace(action, match =>
        {
            string key = match.Groups[1].Value;

            if (_valueProviders.TryGetValue(key, out Func<string> provider))
                return provider.Invoke();

            return match.Value;
        });

        return action;
    }

    private string NormalizeL2Text(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = text.Replace("\r", " ")
                   .Replace("\n", " ")
                   .Replace("\t", " ")
                   .Replace("&nbsp;", " ");

        // collapse ONLY extreme spacing, keep single spaces
        return Regex.Replace(text, @" {2,}", " ");
    }

    private string NormalizeColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return "#DCD9DC";

        color = color.Trim();

        switch (color.ToLowerInvariant())
        {
            case "level":
                return "#FFD700";
            case "name":
                return "#FFFFFF";
            case "title":
                return "#B0C4DE";
            case "red":
                return "#FF0000";
            case "green":
                return "#00FF00";
            case "blue":
                return "#0000FF";
            case "white":
                return "#FFFFFF";
            case "black":
                return "#000000";
            case "yellow":
                return "#FFFF00";
            case "orange":
                return "#FFA500";
            case "gray":
            case "grey":
                return "#808080";
        }

        if (color.StartsWith("#"))
            return color;

        if (HexColorRegex.IsMatch(color))
            return "#" + color;

        if (ColorUtility.TryParseHtmlString(color, out Color parsed))
            return "#" + ColorUtility.ToHtmlStringRGB(parsed);

        return "#DCD9DC";
    }

    private float ConvertFontSize(float htmlSize)
    {
        if (htmlSize <= 1) return 10;
        if (htmlSize == 2) return 12;
        if (htmlSize == 3) return 13;
        if (htmlSize == 4) return 15;
        if (htmlSize == 5) return 17;
        if (htmlSize == 6) return 19;

        return htmlSize;
    }

    private TextAnchor ParseTextAlign(string align)
    {
        switch (align.ToLowerInvariant())
        {
            case "center":
                return TextAnchor.MiddleCenter;

            case "right":
            case "end":
                return TextAnchor.MiddleRight;

            default:
                return TextAnchor.MiddleLeft;
        }
    }

    private string ReadTagName(string tagContent)
    {
        int index = tagContent.IndexOfAny(new[]
        {
            ' ',
            '\t',
            '\r',
            '\n',
            '/'
        });

        if (index == -1)
            return tagContent.Trim();

        return tagContent.Substring(0, index).Trim();
    }

    private void ReadAttributes(string tagContent, Dictionary<string, string> target)
    {
        target.Clear();

        int i = 0;
        int length = tagContent.Length;

        while (i < length && !char.IsWhiteSpace(tagContent[i]) && tagContent[i] != '/')
            i++;

        while (i < length)
        {
            while (i < length && (char.IsWhiteSpace(tagContent[i]) || tagContent[i] == '/'))
                i++;

            if (i >= length)
                break;

            int keyStart = i;

            while (i < length &&
                   !char.IsWhiteSpace(tagContent[i]) &&
                   tagContent[i] != '=' &&
                   tagContent[i] != '/')
            {
                i++;
            }

            if (i <= keyStart)
                break;

            string key = tagContent.Substring(keyStart, i - keyStart).ToLowerInvariant();

            while (i < length && char.IsWhiteSpace(tagContent[i]))
                i++;

            if (i >= length || tagContent[i] != '=')
            {
                target[key] = string.Empty;
                continue;
            }

            i++;

            while (i < length && char.IsWhiteSpace(tagContent[i]))
                i++;

            if (i >= length)
            {
                target[key] = string.Empty;
                break;
            }

            string value;
            char quote = tagContent[i];

            if (quote == '"' || quote == '\'')
            {
                i++;
                int valueStart = i;

                while (i < length && tagContent[i] != quote)
                    i++;

                value = tagContent.Substring(valueStart, i - valueStart);

                if (i < length && tagContent[i] == quote)
                    i++;
            }
            else
            {
                int valueStart = i;

                while (i < length && !char.IsWhiteSpace(tagContent[i]) && tagContent[i] != '/')
                    i++;

                value = tagContent.Substring(valueStart, i - valueStart);
            }

            target[key] = Decode(value);
        }
    }

    private string GetAttr(string key, string defaultValue)
    {
        return _attrs.TryGetValue(key.ToLowerInvariant(), out string value) ? value : defaultValue;
    }

    private bool TryFloat(string value, out float result)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private bool TryInt(string value, out int result)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    private bool IsTruthy(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        value = value.Trim().ToLowerInvariant();

        return value == "true" ||
               value == "1" ||
               value == "yes" ||
               value == "checked" ||
               value == "selected" ||
               value == "disabled" ||
               value == "readonly" ||
               value == "nowrap";
    }

    private string Decode(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&nbsp;", " ")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&quot;", "\"")
            .Replace("&amp;", "&");
    }
}