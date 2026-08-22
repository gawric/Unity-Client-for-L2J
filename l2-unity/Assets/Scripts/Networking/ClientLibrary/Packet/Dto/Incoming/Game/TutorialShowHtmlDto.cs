public class TutorialShowHtmlDto : IWireDto
{
    private string _html;
    public string Html => _html;

    

    public void ReadFrom(PacketReader reader)
    {
        _html = reader.ReadOtherS();
    }
}
