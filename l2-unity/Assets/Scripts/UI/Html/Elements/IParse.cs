using HtmlAgilityPack;
using System.Collections.Generic;
using UnityEngine;

public interface IParse
{
    void Parse(string text);   
    void ParseNode(HtmlNode node);

    List<IElementsUI> GetElements();

}
