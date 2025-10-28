using HtmlAgilityPack;

using System.Collections.Generic;

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

    public int GetNpcId()
    {
        return _npcObjId;
    }

    public override void Parse()
    {
        _npcObjId = ReadI();
        _html = ReadOtherS();
        _itemId = ReadI();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(_html);
        parce = new ParseDetected();

        ProcessHtmlNodes(htmlDoc.DocumentNode  , parce);


    }

    static void ProcessHtmlNodes(HtmlNode node , IParse parce)
    {

        if (node.NodeType == HtmlNodeType.Text)
        {
            parce.Parse(node.InnerText.Trim());

        }
        else
        {
            foreach (var child in node.ChildNodes)
            {
                parce.ParseNode(child);
                ProcessHtmlNodes(child, parce);
            }
        }
    }
}
