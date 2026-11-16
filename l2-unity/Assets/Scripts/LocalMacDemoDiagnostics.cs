using System;
using System.Collections;
using UnityEngine;

// Opt-in diagnostics for the separate local client; inactive during normal play.
public sealed class LocalMacDemoDiagnostics : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-local-demo-diagnostics") < 0)
            return;

        var host = new GameObject(nameof(LocalMacDemoDiagnostics));
        DontDestroyOnLoad(host);
        host.AddComponent<LocalMacDemoDiagnostics>();
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(10);
        int invalidMaterials = 0;
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                Shader shader = material != null ? material.shader : null;
                if (shader != null && shader.isSupported && shader.name != "Hidden/InternalErrorShader")
                    continue;

                invalidMaterials++;
                Debug.LogWarning($"[LocalMacDemo] Invalid material: object={renderer.name}, material={(material != null ? material.name : "null")}, shader={(shader != null ? shader.name : "null")}");
            }
        }
        Debug.Log($"[LocalMacDemo] Menu check: {Screen.width}x{Screen.height}, invalid material slots={invalidMaterials}, graphics={SystemInfo.graphicsDeviceType}");
        Destroy(gameObject);
    }
}
