#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class L2EffectImportUtil
{
    public static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "unnamed";
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    public static string CleanToken(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        int nullIndex = name.IndexOf('\0');
        if (nullIndex >= 0)
        {
            name = name.Substring(0, nullIndex);
        }

        name = name.Trim().Trim('"', '\'');
        int slash = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
        if (slash >= 0)
        {
            name = name.Substring(slash + 1);
        }

        return Sanitize(name);
    }

    public static string Quote(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "\"\"";
        }

        return "\"" + path.Trim('"') + "\"";
    }

    public static bool TryParseRef(string raw, out string package, out string objectName)
    {
        package = "";
        objectName = "";
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string value = raw.Trim().Trim('"', '\'');
        int quote = raw.IndexOf('\'');
        if (quote >= 0)
        {
            int end = raw.LastIndexOf('\'');
            if (end > quote)
            {
                value = raw.Substring(quote + 1, end - quote - 1);
            }
        }

        string[] parts = value.Split('.');
        objectName = CleanToken(parts[parts.Length - 1]);
        if (parts.Length >= 2)
        {
            package = CleanToken(parts[0]);
        }

        return !string.IsNullOrWhiteSpace(objectName);
    }

    public static bool CopyIntoProject(string sourcePath, string assetPath)
    {
        try
        {
            EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            string fullDest = Path.GetFullPath(
                Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, assetPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullDest)!);
            File.Copy(sourcePath, fullDest, true);
            AssetDatabase.ImportAsset(assetPath);
            return AssetDatabase.LoadMainAssetAtPath(assetPath) != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[L2EffectGenerator] copy skipped: " + ex.Message);
            return false;
        }
    }

    public static void EnsureFolder(string assetFolder)
    {
        if (string.IsNullOrEmpty(assetFolder) || AssetDatabase.IsValidFolder(assetFolder))
        {
            return;
        }

        string[] parts = assetFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    public static string FindExported(string dir, string objectName, params string[] exts)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || exts == null)
        {
            return null;
        }

        string safeName = Sanitize(CleanToken(objectName));
        if (string.IsNullOrEmpty(safeName))
        {
            return null;
        }

        for (int i = 0; i < exts.Length; i++)
        {
            string pattern = safeName + exts[i];
            if (!IsSafeSearchPattern(pattern))
            {
                continue;
            }

            string[] hits = GetFilesSafe(dir, pattern);
            if (hits.Length > 0)
            {
                return hits[0];
            }
        }

        return null;
    }

    public static string[] GetFilesSafe(string dir, string pattern)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir) || !IsSafeSearchPattern(pattern))
        {
            return Array.Empty<string>();
        }

        try
        {
            return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[L2EffectGenerator] file search skipped (" + pattern + " in " + dir + "): " +
                ex.Message);
            return Array.Empty<string>();
        }
    }

    public static bool IsSafeSearchPattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c == '*' || c == '?')
            {
                continue;
            }

            if (c < 32 || c == '"' || c == '<' || c == '>' || c == '|' || c == ':' ||
                c == '/' || c == '\\')
            {
                return false;
            }
        }

        return true;
    }

    public static Texture2D LoadImportedTexture(string textureName)
    {
        if (string.IsNullOrWhiteSpace(textureName))
        {
            return null;
        }

        string[] extensions = { ".png", ".tga", ".jpg", ".jpeg" };
        for (int i = 0; i < extensions.Length; i++)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                L2EffectPackageRoots.TextureDestFolder + "/" + textureName + extensions[i]);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    public static Mesh LoadProjectMesh(string objectName)
    {
        string[] paths =
        {
            L2EffectPackageRoots.MeshDestFolder + "/" + objectName + ".asset",
            L2EffectPackageRoots.MeshDestFolder + "/" + objectName + ".obj",
            L2EffectPackageRoots.MeshDestFolder + "/" + objectName + ".fbx"
        };
        for (int p = 0; p < paths.Length; p++)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(paths[p]);
            if (mesh != null)
            {
                return mesh;
            }

            UnityEngine.Object[] imported = AssetDatabase.LoadAllAssetsAtPath(paths[p]);
            for (int i = 0; i < imported.Length; i++)
            {
                if (imported[i] is Mesh importedMesh)
                {
                    return importedMesh;
                }
            }
        }

        return null;
    }
}
#endif
