using System;
using System.IO;
using UnityEngine;

// Login server address without a rebuild: "-login-host host[:port]" or login-server.txt next to the game.
public static class LoginEndpoint
{
    public const string FileName = "login-server.txt";

    public static void Apply(ref string host, ref int port)
    {
        string value = FromCommandLine();
        string source = "command line";
        if (string.IsNullOrWhiteSpace(value))
        {
            value = FromFile(out source);
        }
        if (string.IsNullOrWhiteSpace(value))
            return;

        Parse(value.Trim(), ref host, ref port);
        Debug.Log("[LoginEndpoint] " + host + ":" + port + " from " + source);
    }

    static string FromCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-login-host")
                return args[i + 1];
        }
        return null;
    }

    static string FromFile(out string source)
    {
        source = null;
        foreach (string dir in CandidateDirs())
        {
            string path = Path.Combine(dir, FileName);
            if (!File.Exists(path))
                continue;

            foreach (string line in File.ReadAllLines(path))
            {
                string text = line.Trim();
                if (text.Length > 0 && !text.StartsWith("#"))
                {
                    source = path;
                    return text;
                }
            }
        }
        return null;
    }

    // Windows: folder with the exe. macOS: inside the .app, then the folder that holds it.
    static string[] CandidateDirs()
    {
        string data = Application.dataPath;
        return new[]
        {
            Path.GetFullPath(Path.Combine(data, "..")),
            Path.GetFullPath(Path.Combine(data, "..", "..")),
        };
    }

    static void Parse(string value, ref string host, ref int port)
    {
        int colon = value.LastIndexOf(':');
        if (colon > 0 && value.IndexOf(':') == colon)
        {
            int parsed;
            if (int.TryParse(value.Substring(colon + 1), out parsed) && parsed > 0 && parsed < 65536)
                port = parsed;
            host = value.Substring(0, colon);
        }
        else
        {
            host = value;
        }
    }
}
