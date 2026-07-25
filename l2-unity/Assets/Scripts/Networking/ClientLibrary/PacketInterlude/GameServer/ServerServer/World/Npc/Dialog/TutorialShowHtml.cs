public class TutorialShowHtml : ServerPacket
{
    private string _html;
    public string Html => _html;

    public TutorialShowHtml(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        _html = ReadOtherS();
    }
}
