using HtmlAgilityPack;

using System.Collections.Generic;
using System.Security.Principal;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class NpcHtmlMessage : ServerPacket
{

    private int _npcObjId;
    private string _html;
    private int _itemId;
    private IParse parce;
    public List<IElementsUI> Elements()
    {
        return parce.GetElements();
    }


    public NpcHtmlMessage(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _npcObjId = ReadI();
        _html = ReadOtherS();
        _itemId = ReadI();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(_html);

        //var htmlBody = htmlDoc.DocumentNode.SelectSingleNode("//a");
        //var htmlNodes = htmlDoc.DocumentNode.SelectNodes("//a");
        parce = new ParseDetected();

        PrintNode(htmlDoc.DocumentNode  , parce);
       // Debug.Log("");

    }

    static void PrintNode(HtmlNode node , IParse parce)
    {
        // ���� ���� ���������, ������� ��� �����
        if (node.NodeType == HtmlNodeType.Text)
        {
           // Debug.Log("����� �� ����� " + node.InnerText.Trim());

            parce.Parse(node.InnerText.Trim());
            //Console.WriteLine($"Text: '{node.InnerText.Trim()}'");
        }
        else
        {
            //Debug.Log("����� �� ���  " + node.Name);

            // �������� �� �������� �����
            foreach (var child in node.ChildNodes)
            {
                parce.ParseNode(child);
                PrintNode(child, parce);
            }
        }
    }
}
