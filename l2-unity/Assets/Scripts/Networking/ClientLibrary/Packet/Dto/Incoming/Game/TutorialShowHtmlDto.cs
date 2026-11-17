public class TutorialShowHtmlDto : IWireDto
{
    private string _html;
    public string Html => _html;

    public static TutorialShowHtmlDto From(string html)
    {
        return new TutorialShowHtmlDto { _html = html };
    }

    

    public void ReadFrom(PacketReader reader)
    {
        _html = reader.ReadOtherS();
    }
}
