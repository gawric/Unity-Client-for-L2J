public class ParseButton : IElementsUI
{
    public string Value { get; private set; }
    public string Action { get; private set; }
    public string Width { get; private set; }
    public string Height { get; private set; }
    public string Back { get; private set; }
    public string Fore { get; private set; }

    public ParseButton(string value, string action, string width, string height, string back, string fore)
    {
        Value = value;
        Action = clearBypass(action);
        Width = width;
        Height = height;
        Back = back;
        Fore = fore;
    }

    private string clearBypass(string btn)
    {
        string pass1 = btn.Replace("bypass", "").Trim();
        string pass2 = pass1.Replace("-h", "").Trim();
        return pass2;
    }
}