using HtmlAgilityPack;
using System.Collections.Generic;
using UnityEngine;

public class TutorialShowHtml : ServerPacket
{
    private ParseDetected parce;
    private string _html;

    public List<IElementsUI> Elements()
    {
        return parce.GetElements();
    }


    public TutorialShowHtml(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {

        _html = ReadOtherS();

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(_html);

        parce = new ParseDetected();

        DetectedNode(htmlDoc.DocumentNode, parce);


    }

    static void DetectedNode(HtmlNode node, IParse parce)
    {
        // Если узел текстовый, выводим его текст
        if (node.NodeType == HtmlNodeType.Text)
        {
            parce.Parse(node.InnerText.Trim());
        }
        else
        {
            foreach (var child in node.ChildNodes)
            {
                parce.ParseNode(child);
                DetectedNode(child, parce);
            }
        }
    }
}
