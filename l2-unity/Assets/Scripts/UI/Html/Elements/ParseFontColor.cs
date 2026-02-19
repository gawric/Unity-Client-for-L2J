using System;
using UnityEngine;

public class ParseFontColor : IElementsUI
{
    private string _nameColor;
    private string _text;

    //public Color Name { get => _nameColor; }
    public string Text { get => _text; }
    public ParseFontColor(string nameColor , string text)
    {
        _nameColor = nameColor;
        _text = text;
    }

    public Color GetColor()
    {
        if(string.Equals(_nameColor, "level", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(1f, 0.8f, 0.4f);
        }

        return new Color(220f, 217f, 220f);
    }

    public string ToHex()
    {
        // Преобразуем цвет в шестнадцатеричный формат
        return ColorUtility.ToHtmlStringRGB(GetColor());
    }
}
