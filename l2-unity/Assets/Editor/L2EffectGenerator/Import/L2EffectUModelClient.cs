#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class L2EffectUModelClient
{
    const int UModelTimeoutMs = 180000;

    public static string ExtractObject(string packageName, string objectName, bool texture)
    {
        string outDir = Path.Combine(
            L2EffectPackageRoots.ExtractRoot(),
            texture ? "Textures" : "Meshes",
            L2EffectImportUtil.Sanitize(objectName));
        ExtractObjectLog(packageName, objectName, texture, outDir);
        return Directory.Exists(outDir) ? outDir : null;
    }

    public static string ExtractObjectLog(
        string packageName,
        string objectName,
        bool texture,
        string outDir)
    {
        string umodel = L2EffectPackageRoots.FindUModelExe();
        if (string.IsNullOrEmpty(umodel))
        {
            return string.Empty;
        }

        Directory.CreateDirectory(outDir);
        EnsureSilentConfig();

        string already = texture
            ? L2EffectImportUtil.FindExported(outDir, objectName, ".png", ".tga")
            : L2EffectImportUtil.FindExported(outDir, objectName, ".psk", ".pskx");
        if (!string.IsNullOrEmpty(already))
        {
            return string.Empty;
        }

        var log = new StringBuilder();
        foreach (string pkg in FindPackageFiles(packageName, texture))
        {
            string[] classes = texture
                ? new[] { "Texture", null }
                : new[] { "StaticMesh", "VertMesh", "SkeletalMesh", null };
            for (int i = 0; i < classes.Length; i++)
            {
                log.Append(RunUModel(umodel, pkg, objectName, classes[i], outDir));
                string found = texture
                    ? L2EffectImportUtil.FindExported(outDir, objectName, ".png", ".tga")
                    : L2EffectImportUtil.FindExported(outDir, objectName, ".props.txt", ".psk", ".pskx");
                if (!string.IsNullOrEmpty(found))
                {
                    return log.ToString();
                }
            }
        }

        return log.ToString();
    }

    public static string ExtractMaterialProps(string packageName, string objectName, string classHint)
    {
        string outDir = Path.Combine(
            L2EffectPackageRoots.ExtractRoot(),
            "Materials",
            L2EffectImportUtil.Sanitize(objectName));
        Directory.CreateDirectory(outDir);
        string already = L2EffectImportUtil.FindExported(outDir, objectName, ".props.txt");
        if (!string.IsNullOrEmpty(already))
        {
            return already;
        }

        string umodel = L2EffectPackageRoots.FindUModelExe();
        if (string.IsNullOrEmpty(umodel))
        {
            return null;
        }

        EnsureSilentConfig();
        string[] classes = string.IsNullOrEmpty(classHint)
            ? new[]
            {
                "Shader", "Combiner", "FinalBlend", "TexPanner", "TexOscillator",
                "TexRotator", "TexScaler", "Texture", null
            }
            : new[] { classHint, null };
        foreach (string pkg in FindPackageFiles(packageName, texture: true))
        {
            for (int i = 0; i < classes.Length; i++)
            {
                RunUModel(umodel, pkg, objectName, classes[i], outDir, dump: false);
                string found = L2EffectImportUtil.FindExported(outDir, objectName, ".props.txt");
                if (!string.IsNullOrEmpty(found))
                {
                    return found;
                }
            }
        }

        return null;
    }

    public static string DumpObjectLog(string packageName, string objectName, bool texture)
    {
        string umodel = L2EffectPackageRoots.FindUModelExe();
        if (string.IsNullOrEmpty(umodel))
        {
            return string.Empty;
        }

        EnsureSilentConfig();
        string outDir = Path.Combine(
            L2EffectPackageRoots.ExtractRoot(),
            texture ? "Textures" : "Meshes",
            L2EffectImportUtil.Sanitize(objectName),
            "dump");
        Directory.CreateDirectory(outDir);
        var log = new StringBuilder();
        string[] classes = texture
            ? new[] { "Texture", "Shader", "Combiner", null }
            : new[] { "StaticMesh", "VertMesh", "SkeletalMesh", null };
        foreach (string pkg in FindPackageFiles(packageName, texture))
        {
            for (int i = 0; i < classes.Length; i++)
            {
                log.Append(RunUModel(umodel, pkg, objectName, classes[i], outDir, dump: true));
                if (log.ToString().IndexOf("Object info:", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return log.ToString();
                }
            }
        }

        return log.ToString();
    }

    public static string RunUModel(
        string umodelExe,
        string packagePath,
        string objectName,
        string classHint,
        string outDir,
        bool dump = false)
    {
        string searchPath = L2EffectPackageRoots.TextureSearchPath(packagePath);
        var psi = new ProcessStartInfo
        {
            FileName = umodelExe,
            WorkingDirectory = L2EffectPackageRoots.UModelWorkDir(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Arguments = (dump ? "-dump" : "-export -png -psk") +
                        " -game=l2" +
                        " -path=" + L2EffectImportUtil.Quote(searchPath) +
                        " -out=" + L2EffectImportUtil.Quote(outDir) +
                        " " + L2EffectImportUtil.Quote(packagePath) +
                        " " + objectName +
                        (string.IsNullOrEmpty(classHint) ? string.Empty : " " + classHint)
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                if (process == null)
                {
                    return string.Empty;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(UModelTimeoutMs))
                {
                    try { process.Kill(); } catch { }
                }

                return stdout + Environment.NewLine + stderr;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[L2EffectGenerator] umodel skipped: " + ex.Message);
            return string.Empty;
        }
    }

    public static IEnumerable<string> FindPackageFiles(string packageName, bool texture)
    {
        string[] exts = texture
            ? new[] { ".utx", ".u" }
            : new[] { ".usx", ".ukx", ".u" };

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> dirs = texture
            ? L2EffectPackageRoots.TexturePackageDirs()
            : L2EffectPackageRoots.MeshPackageDirs();
        foreach (string dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (string name in L2EffectPackageRoots.PackageNameCandidates(packageName, texture))
            {
                for (int i = 0; i < exts.Length; i++)
                {
                    string exact = Path.Combine(dir, name + exts[i]);
                    if (File.Exists(exact) && seen.Add(exact))
                    {
                        yield return exact;
                    }
                }
            }
        }
    }

    public static void EnsureSilentConfig()
    {
        string cfg = Path.Combine(L2EffectPackageRoots.UModelWorkDir(), "umodel.cfg");
        if (File.Exists(cfg))
        {
            return;
        }

        File.WriteAllText(cfg,
            "Export =\n{\n    ExportDdsTexture = false\n    TextureFormat = 2\n    DontOverwriteFiles = false\n}\n" +
            "bShowExportOptions = false\n" +
            "bShowSaveOptions = false\n");
    }
}
#endif
