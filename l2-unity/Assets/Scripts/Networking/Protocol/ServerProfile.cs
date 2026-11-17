using System;

/// <summary>
/// Perfil de un servidor: qué crónica habla y de dónde salen sus datos.
/// ACIS/Interlude es el perfil por defecto para no cambiar el comportamiento actual.
/// </summary>
[Serializable]
public sealed class ServerProfile
{
    public string Id = "acis-interlude";
    public string DisplayName = "L2J ACIS (Interlude)";
    public ChronicleProtocol Protocol = ChronicleProtocol.InterludeAcis;
    public int ProtocolVersion = 746;
    public string DataRoot = string.Empty;

    public static ServerProfile AcisInterlude()
    {
        return new ServerProfile
        {
            Id = "acis-interlude",
            DisplayName = "L2J ACIS (Interlude)",
            Protocol = ChronicleProtocol.InterludeAcis,
            ProtocolVersion = 746,
            DataRoot = string.Empty,
        };
    }

    public static ServerProfile LuceraClassic166()
    {
        return new ServerProfile
        {
            Id = "lucera-classic-166",
            DisplayName = "Lucera Classic (166)",
            Protocol = ChronicleProtocol.LuceraClassic166,
            ProtocolVersion = 166,
            DataRoot = "lucera-classic-166",
        };
    }

    /// <summary>
    /// Perfil activo. Si se pasa <c>-serverProfile lucera</c> (o
    /// <c>-serverProfile=lucera</c>) usa Lucera; en cualquier otro caso ACIS.
    /// </summary>
    public static ServerProfile Active
    {
        get
        {
            if (_active == null)
            {
                _active = IsLuceraRequested() ? LuceraClassic166() : AcisInterlude();
            }

            return _active;
        }
    }

    public static ServerProfile Default
    {
        get { return AcisInterlude(); }
    }

    public static void Use(ServerProfile profile)
    {
        _active = profile ?? AcisInterlude();
    }

    private static ServerProfile _active;

    private static bool IsLuceraRequested()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i] ?? string.Empty;
            if (arg.Equals("-serverProfile", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("--serverProfile", StringComparison.OrdinalIgnoreCase))
            {
                return i + 1 < args.Length &&
                       args[i + 1].IndexOf("lucera", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (arg.StartsWith("-serverProfile=", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("--serverProfile=", StringComparison.OrdinalIgnoreCase))
            {
                return arg.IndexOf("lucera", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        return false;
    }
}
