#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Temp/generate.resurrection asks the already-open editor to build wh_resurrection.
/// A second Unity -executeMethod cannot open this project while the editor is running.
/// </summary>
[InitializeOnLoad]
public static class L2EffectGenerateRequest
{
    static L2EffectGenerateRequest()
    {
        EditorApplication.update += Poll;
    }

    public static string RequestPath
    {
        get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "generate.resurrection")); }
    }

    static void Poll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;
        if (!File.Exists(RequestPath))
            return;

        File.Delete(RequestPath);
        Debug.Log("[L2EffectGenerator] generating resurrection");
        L2EffectGeneratorWindow.GenerateResurrection();
    }
}
#endif
