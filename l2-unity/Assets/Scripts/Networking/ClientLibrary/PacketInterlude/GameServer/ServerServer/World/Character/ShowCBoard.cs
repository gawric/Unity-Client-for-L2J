using UnityEngine;

/**
 * @author AbsolutePower
 */
public class ShowCBoard : ServerPacket
{
    private static string _html101 = "";
    private static string _html102 = "";
    private static string _html103 = "";

    public string Html => _html101 + _html102 + _html103;

    public ShowCBoard(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        ReadB();
        // bypass commands
        ReadOtherS();
        ReadOtherS();
        ReadOtherS();
        ReadOtherS();
        ReadOtherS();
        ReadOtherS();
        ReadOtherS();
        ReadOtherS();

        string data = ReadOtherS();

        if (string.IsNullOrEmpty(data))
            return;

        string[] split = data.Split('\u0008');

        if (split.Length < 2)
        {
            Debug.LogError("ShowCBoard invalid data");
            return;
        }

        string id = split[0];
        string html = split[1];

        switch (id)
        {
            case "101":
                _html101 = html;
                _html102 = "";
                _html103 = "";
                Debug.Log("Received CBoard 101");
                break;
            case "102":
                _html102 = html;
                Debug.Log("Received CBoard 102");
                break;
            case "103":
                _html103 = html;
                Debug.Log("Received CBoard 103");
                break;
        }

        Debug.Log("CBoard Part: " + id + " size: " + html.Length);

        if (id == "103" || (id == "102" && string.IsNullOrEmpty(_html103)) || (id == "101" && string.IsNullOrEmpty(_html102)))
        {
            OpenBoard();
        }
    }

    private void OpenBoard()
    {
        string fullHtml = _html101 + _html102 + _html103;
        HtmlWindow.Instance.InjectToCommunityWindow(fullHtml);
        HtmlWindow.Instance.ToggleCommunityBoard(false);
    }
}
