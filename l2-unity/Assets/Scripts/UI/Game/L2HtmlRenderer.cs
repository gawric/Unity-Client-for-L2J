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
    //private static readonly Color LinkColor = new Color(1f, 0.843f, 0f, 1f);
    private static readonly Color LinkColor = new Color32(70, 120, 230, 255);

    private static readonly Regex MultiSpaceRegex = new Regex(@"[ ]{3,}", RegexOptions.Compiled);
    private static readonly Regex VariableRegex = new Regex(@"\$(\w+)", RegexOptions.Compiled);
    private static readonly Regex HexColorRegex = new Regex(@"^[0-9a-fA-F]{6}$|^[0-9a-fA-F]{8}$", RegexOptions.Compiled);

    private static readonly Dictionary<string, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
    private static readonly HashSet<string> MissingTextureCache = new HashSet<string>();

    private readonly VisualElement _root;
    private readonly Action<string> _onAction;

    private readonly Stack<VisualElement> _parentStack = new Stack<VisualElement>();
    private readonly Stack<string> _colorStack = new Stack<string>();
    private readonly Stack<string> _linkActionStack = new Stack<string>();
    private readonly Stack<bool> _centerStack = new Stack<bool>();

    private readonly Dictionary<string, Func<string>> _valueProviders = new Dictionary<string, Func<string>>();

    private readonly StringBuilder _textBuffer = new StringBuilder(256);

    private readonly Dictionary<string, string> _attrs = new Dictionary<string, string>(16);

    private string _activeColor;
    private bool _centerMode;
    private bool _insideHead;
    private bool _insideTitle;

    private DropdownField _activeDropdown;
    private string _activeDropdownVar;
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

        if (string.IsNullOrWhiteSpace(html))
            return;

        html = Decode(html);

        int i = 0;

        while (i < html.Length)
        {
            char c = html[i];

            if (c == '<')
            {
                if (!_insideHead && !_insideTitle)
                    FlushText();
                else
                    _textBuffer.Clear();

                int end = FindTagEnd(html, i + 1);
                if (end < 0)
                    break;

                string rawTag = html.Substring(i + 1, end - i - 1).Trim();
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
        _linkActionStack.Clear();
        _centerStack.Clear();
        _valueProviders.Clear();

        _textBuffer.Clear();

        _activeColor = null;
        _centerMode = false;
        _insideHead = false;
        _insideTitle = false;

        _activeDropdown = null;
        _activeDropdownVar = null;
        _activeDropdownLabels.Clear();
        _activeDropdownValues.Clear();
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

            if (closingName == "div" ||
                closingName == "table" ||
                closingName == "tr" ||
                closingName == "td" ||
                closingName == "center")
            {
                PopParent();
            }

            if (closingName == "font")
                _activeColor = _colorStack.Count > 0 ? _colorStack.Pop() : null;

            if (closingName == "a" && _linkActionStack.Count > 0)
                _linkActionStack.Pop();

            if (closingName == "select" || closingName == "combobox")
                FinishDropdown();

            if (closingName == "option")
                FinishDropdownOptionText();

            if (closingName == "center")
                _centerMode = _centerStack.Count > 0 && _centerStack.Pop();

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

        if (_insideHead || _insideTitle)
        {
            _textBuffer.Clear();
            return;
        }

        if (tagName == "html" || tagName == "body")
            return;

        ReadAttributes(rawTag, _attrs);

        switch (tagName)
        {
            case "br":
            case "br1":
                AddBreak();
                break;
            case "center":
                AddCenter();
                break;
            case "font":
                _colorStack.Push(_activeColor);
                _activeColor = NormalizeColor(GetAttr("color", GetAttr("value", "#DCD9DC")));
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
        }
    }

    private void FlushText()
    {
        if (_textBuffer.Length == 0)
            return;

        if (_activeDropdown != null)
            return;

        string text = NormalizeL2Text(_textBuffer.ToString());
        _textBuffer.Clear();

        if (string.IsNullOrWhiteSpace(text))
            return;

        bool isLink = !string.IsNullOrEmpty(CurrentLinkAction);

        Label label = new Label();

        label.text = !string.IsNullOrEmpty(_activeColor) ? "<color=" + _activeColor + ">" + text + "</color>" : text;

        if (isLink)
            label.text = "<u>" + label.text + "</u>";

        label.enableRichText = true;
        label.AddToClassList(isLink ? "html_link" : "html_text");

        //label.style.color = isLink ? LinkColor : Color.white;

        if (isLink)
        {
            label.style.color = string.IsNullOrEmpty(_activeColor) ? LinkColor: Color.white;
        }
        else
        {
            label.style.color = Color.white;
        }

        label.style.fontSize = 13;
        label.style.flexShrink = 1;
        label.style.whiteSpace = WhiteSpace.Normal;

        label.style.unityTextAlign = _centerMode ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;

        label.style.marginTop = 0;
        label.style.marginBottom = 0;
        label.style.paddingTop = 1;
        label.style.paddingBottom = 1;

        if (isLink)
        {
            string action = CurrentLinkAction;

            label.pickingMode = PickingMode.Position;
            label.focusable = true;

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
        br.style.marginTop = 0;
        br.style.marginBottom = 0;
        br.style.paddingTop = 0;
        br.style.paddingBottom = 0;
        br.style.minHeight = 0;

        CurrentParent.Add(br);
    }

    private void AddCenter()
    {
        _centerStack.Push(_centerMode);
        _centerMode = true;

        VisualElement center = CreateContainer();

        center.AddToClassList("html_center");
        center.style.flexDirection = FlexDirection.Column;
        center.style.alignItems = Align.Center;
        center.style.justifyContent = Justify.FlexStart;
        center.style.width = Length.Percent(100);

        CurrentParent.Add(center);
        _parentStack.Push(center);
    }

    private void AddDiv()
    {
        VisualElement div = CreateContainer();

        div.AddToClassList("html_div");

        string flex = GetAttr("flex", "column");
        div.style.flexDirection = flex == "row" ? FlexDirection.Row : FlexDirection.Column;

        ApplyAlign(div, GetAttr("align", _centerMode ? "center" : "start"));
        ApplyJustify(div, GetAttr("justify", "start"));
        ApplySize(div);
        ApplyBackground(div);
        ApplyClasses(div);

        CurrentParent.Add(div);
        _parentStack.Push(div);
    }

    private void AddTable()
    {
        VisualElement table = CreateContainer();

        table.AddToClassList("html_table");

        table.style.flexDirection = FlexDirection.Column;
        table.style.alignItems = Align.Stretch;
        table.style.justifyContent = Justify.FlexStart;
        table.style.width = Length.Percent(100);

        ApplySize(table);
        ApplyBackground(table);
        ApplyClasses(table);

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
        row.style.flexShrink = 1;

        ApplySize(row);
        ApplyBackground(row);
        ApplyClasses(row);

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
            cell.style.flexShrink = 0;
        }
        else
        {
            cell.style.flexGrow = 1;
            cell.style.flexShrink = 1;
        }

        cell.style.paddingLeft = 1;
        cell.style.paddingRight = 1;
        cell.style.paddingTop = 1;
        cell.style.paddingBottom = 1;

        string align = GetAttr("align", _centerMode ? "center" : "start");
        ApplyAlign(cell, align);

        ApplySize(cell);
        ApplyBackground(cell);
        ApplyClasses(cell);

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

        input.style.flexShrink = 0;
        input.style.marginTop = 0;
        input.style.marginBottom = 0;
        input.style.paddingTop = 0;
        input.style.paddingBottom = 0;

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

        input.style.flexShrink = 0;
        input.style.marginTop = 0;
        input.style.marginBottom = 0;
        input.style.paddingTop = 0;
        input.style.paddingBottom = 0;

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

        button.style.flexShrink = 0;
        button.style.marginTop = 0;
        button.style.marginBottom = 0;
        button.style.paddingTop = 0;
        button.style.paddingBottom = 0;

        button.clicked += () =>
        {
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

        image.style.flexShrink = 0;
        image.style.marginTop = 0;
        image.style.marginBottom = 0;
        image.style.paddingTop = 0;
        image.style.paddingBottom = 0;

        CurrentParent.Add(image);
    }

    private void StartDropdown()
    {
        _activeDropdown = new DropdownField();
        _activeDropdown.AddToClassList("html_dropdown");

        _activeDropdownVar = GetAttr("var", GetAttr("name", ""));

        _activeDropdownLabels.Clear();
        _activeDropdownValues.Clear();
        _textBuffer.Clear();

        if (TryFloat(GetAttr("width", ""), out float width))
            _activeDropdown.style.width = width;

        if (TryFloat(GetAttr("height", ""), out float height))
            _activeDropdown.style.height = height;

        _activeDropdown.style.flexShrink = 0;

        ResetBoxSpacing(_activeDropdown);
    }

    private void AddDropdownOption()
    {
        if (_activeDropdown == null)
            return;

        string value = GetAttr("value", "");
        string label = GetAttr("label", "");

        _activeDropdownLabels.Add(label);
        _activeDropdownValues.Add(value);
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
            _activeDropdown.value = _activeDropdownLabels[0];

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
        _activeDropdownLabels.Clear();
        _activeDropdownValues.Clear();
        _textBuffer.Clear();
    }

    private VisualElement CreateContainer()
    {
        VisualElement ve = new VisualElement();

        ve.style.display = DisplayStyle.Flex;
        ve.style.visibility = Visibility.Visible;

        ve.style.flexGrow = 0;
        ve.style.flexShrink = 0;

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

    private void PopParent()
    {
        if (_parentStack.Count > 0)
            _parentStack.Pop();
    }

    private void ApplyAlign(VisualElement ve, string align)
    {
        switch (align)
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

    private void ApplyJustify(VisualElement ve, string justify)
    {
        switch (justify)
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
            ve.style.minWidth = w;

            if (_attrs.ContainsKey("fixwidth"))
            {
                ve.style.maxWidth = w;
                ve.style.flexGrow = 0;
                ve.style.flexShrink = 0;
            }
        }

        if (TryFloat(GetAttr("height", ""), out float h))
            ve.style.height = h;
    }

    private void ApplyBackground(VisualElement ve)
    {
        string background = GetAttr("background", GetAttr("bg", ""));

        if (string.IsNullOrEmpty(background))
            return;

        Texture2D texture = LoadTexture(background);

        if (texture != null)
            ve.style.backgroundImage = new StyleBackground(texture);
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
        return VariableRegex.Replace(action, match =>
        {
            string key = match.Groups[1].Value;

            if (_valueProviders.TryGetValue(key, out Func<string> provider))
                return provider.Invoke();

            return match.Value;
        });
    }

    private string NormalizeL2Text(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Replace("&nbsp;", " ");

        return MultiSpaceRegex.Replace(text, "  ");
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

            while (i < length && !char.IsWhiteSpace(tagContent[i]) && tagContent[i] != '=' && tagContent[i] != '/')
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
