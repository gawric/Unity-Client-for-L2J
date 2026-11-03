using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// While the Editor is open, Temp/autologin.play starts Play mode.
/// The runtime driver then logs in from Temp/autologin.request.
/// </summary>
[InitializeOnLoad]
public static class UnityAutoLoginPlay
{
    static UnityAutoLoginPlay()
    {
        EditorApplication.update += Poll;
    }

    public static string PlayPath
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "autologin.play")); }
    }

    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!File.Exists(PlayPath))
            return;

        File.Delete(PlayPath);
        Debug.Log("[AutoLogin] starting Play");
        EditorApplication.isPlaying = true;
    }
}
