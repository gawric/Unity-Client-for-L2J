using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParseDetected : IParse
{

    private Dictionary<string, IElementsUI> _elements = new Dictionary<string, IElementsUI>();
    private int _count = 0;
    public List<IElementsUI> GetElements()
    {
        return _elements.Values.ToList();
    }

    public void Parse(string text)
    {

    }

    public void ParseNode(HtmlNode node)
    {
        try
        {
            switch (node.Name)
            {
                case "#text":
                    AddText(node);
                    break;
                case "br":
                case "br1":
                    AddBr(node);
                    break;
                case "a":
                    AddHref(node);
                    break;
                case "font":
                    AddFont(node);
                    break;
                case "edit":
                    AddEdit(node);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("HtmlMessage->ParseNode: error " + ex.ToString());
        }
    }

    private void AddText(HtmlNode node)
    {
        if (!_elements.ContainsKey(node.InnerText))
        {
            _elements.Add(node.InnerText, new ParseLabel(node.InnerText));
        }
    }
    private void AddBr(HtmlNode node)
    {
       _elements.Add("br" + _count++, new ParseBr());
    }
    private void AddHref(HtmlNode node)
    {
        var href = node.Attributes.FirstOrDefault();
        var name = node.InnerText;

        if (!_elements.ContainsKey(name))
        {
            _elements.Add(name, new ParseHref(name, href.Value));
        }
        else
        {
            AddDictNotUniq(name, name, href.Value);
        }
    }
    private void AddFont(HtmlNode node)
    {
        var fontColor = node.Attributes.FirstOrDefault();
        string color = fontColor.Value;
        string text = node.InnerText;
        ParseFontColor parseFontColor = new ParseFontColor(color, text);

        if (!AddHrefColor(text, parseFontColor, _elements))
        {
            _elements.Add(text, parseFontColor);
        }
    }
    private bool AddHrefColor(string textId , ParseFontColor color , Dictionary<string, IElementsUI> dict)
    {
        if (dict.ContainsKey(textId))
        {
           IElementsUI element =  dict[textId];
            
            if(element != null && element.GetType() == typeof(ParseHref))
            {
                var href = (ParseHref)element;
                href.SetColor(color);
                return true;
            }
        }
        return false;
    }


    private void AddEdit(HtmlNode node)
    {
        string text = node.Name;
        var elementNameVar = "";
        if (node.HasAttributes)
        {
            var attr = node.Attributes;
            elementNameVar = attr.Where(p => p.Name.Equals("var"))
                .FirstOrDefault().Value;
        }
        _elements.Add(text , new ParseEdit(elementNameVar));
    }


    private void AddDictNotUniq(string nameName, string name, string hrefValue)
    {
        _elements.Remove(name);
        _elements.Add(nameName, new ParseHref(name, hrefValue));
    }



}
