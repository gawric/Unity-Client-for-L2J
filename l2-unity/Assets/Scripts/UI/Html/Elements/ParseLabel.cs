using UnityEngine;

public class ParseLabel : IElementsUI
{
    private string _text;
    public ParseLabel(string text)
    {
        _text = text;
    }

    public string Text() { return _text; }
}
