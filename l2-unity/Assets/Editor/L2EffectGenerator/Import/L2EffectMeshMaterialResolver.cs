#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class L2EffectMeshMaterialResolver
{
    static readonly Regex TextureImportHint = new Regex(
        @"Texture'([^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex MaterialSlotHint = new Regex(
        @"Material\s*=\s*(?<cls>Texture|Shader|FinalBlend|Combiner|Material|TexPanner|TexOscillator|TexRotator|TexScaler)'(?<path>[^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex UmodelTextureLoaded = new Regex(
        @"Loading Texture\s+(?<name>\S+)\s+from package",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex UmodelTextureExported = new Regex(
        @"Exporting Texture\s+(?<name>\S+)\s+to",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex UmodelImportHint = new Regex(
        @"Import\((?<cls>\w+)'(?<path>[^']+)'\)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex TwoSidedHint = new Regex(
        @"\bTwoSided\s*=\s*true\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    static readonly Regex UseVertexColorHint = new Regex(
        @"\bUseVertexColor\s*=\s*true\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    static readonly Dictionary<string, List<string>> MeshSidecarTextures =
        new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // Temporarily off: test Shader/Combiner walk for D3D9 t0/t1 without this map.
            // { "circle_flow_01", new List<string> { "circleflow_1", "circleflow_2" } }
        };
    static readonly Regex NestedMaterialHint = new Regex(
        @"^\s*(?<prop>Material1|Material2|Diffuse|Opacity|SpecularityMask|Specular|SelfIlluminationMask|SelfIllumination|Detail|NormalMap|Material)\s*=\s*(?<cls>Texture|Shader|FinalBlend|Combiner|Material|TexPanner|TexOscillator|TexRotator|TexScaler)'(?<path>[^']+)'",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
    static readonly string[] ShaderColorProps =
    {
        "Diffuse", "SelfIllumination", "Opacity", "Specular"
    };
    static readonly string[] ShaderMaskProps =
    {
        "SpecularityMask", "SelfIlluminationMask", "Detail", "NormalMap"
    };
    static readonly HashSet<string> MissingMaterialKeys =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, L2EffectMeshPackageBinding> MeshBindingCache =
        new Dictionary<string, L2EffectMeshPackageBinding>(StringComparer.OrdinalIgnoreCase);

    public static L2EffectMeshPackageBinding Ensure(string staticMeshReference)
    {
        var empty = new L2EffectMeshPackageBinding();
        if (!L2EffectImportUtil.TryParseRef(staticMeshReference, out string package, out string objectName))
        {
            return empty;
        }

        if (MeshBindingCache.TryGetValue(objectName, out L2EffectMeshPackageBinding cached))
        {
            return cached;
        }

        var binding = new L2EffectMeshPackageBinding();
        MergeSidecarNames(objectName, binding);

        string umodel = L2EffectPackageRoots.FindUModelExe();
        if (string.IsNullOrEmpty(umodel))
        {
            MeshBindingCache[objectName] = binding;
            return binding;
        }

        try
        {
            string outDir = Path.Combine(
                L2EffectPackageRoots.ExtractRoot(),
                "Meshes",
                L2EffectImportUtil.Sanitize(objectName));
            Directory.CreateDirectory(outDir);
            L2EffectUModelClient.EnsureSilentConfig();

            string props = L2EffectImportUtil.FindExported(outDir, objectName, ".props.txt");
            string psk = L2EffectImportUtil.FindExported(outDir, objectName, ".psk", ".pskx");
            string log = string.Empty;
            if (string.IsNullOrEmpty(props))
            {
                log = L2EffectUModelClient.ExtractObjectLog(package, objectName, false, outDir);
                props = L2EffectImportUtil.FindExported(outDir, objectName, ".props.txt");
                psk = L2EffectImportUtil.FindExported(outDir, objectName, ".psk", ".pskx");
            }

            if (!string.IsNullOrEmpty(props) && File.Exists(props))
            {
                ParseMeshProps(File.ReadAllText(props), binding);
            }

            CollectPskMaterialNames(psk, binding);
            ParseUmodelLog(log, binding);
            if (binding.TextureNames.Count == 0)
            {
                ParseUmodelLog(
                    L2EffectUModelClient.DumpObjectLog(package, objectName, false),
                    binding);
            }

            ExpandLayeredMaterials(objectName, binding);
            CopyExtractedPngs(outDir, objectName);
            CollectExportedPngNames(outDir, binding);
            MergeSidecarNames(objectName, binding);

            for (int i = 0; i < binding.TextureNames.Count; i++)
            {
                RememberSidecarName(objectName, binding.TextureNames[i]);
                if (L2EffectImportUtil.LoadImportedTexture(binding.TextureNames[i]) == null)
                {
                    L2EffectGeneratorViewerImport.TryImportTexture(binding.TextureNames[i]);
                }
            }

            if (binding.TextureNames.Count > 0)
            {
                Debug.Log(
                    "[L2EffectGenerator] " + objectName +
                    ": mesh materials " + string.Join(", ", binding.TextureNames) +
                    (binding.TwoSided ? " (TwoSided)" : string.Empty) +
                    (binding.UseVertexColor ? " (UseVertexColor)" : string.Empty));
            }
            else if (binding.TextureReferences.Count > 0)
            {
                Debug.LogWarning(
                    "[L2EffectGenerator] " + objectName +
                    ": no leaf textures, unresolved " +
                    string.Join(", ", binding.TextureReferences));
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[L2EffectGenerator] mesh material read skipped for " +
                objectName + ": " + ex.Message);
        }

        MeshBindingCache[objectName] = binding;
        return binding;
    }

    public static bool TryGetExtras(
        string staticMeshReference,
        out bool twoSided,
        out bool useVertexColor)
    {
        L2EffectMeshPackageBinding binding = Ensure(staticMeshReference);
        twoSided = binding.TwoSided;
        useVertexColor = binding.UseVertexColor;
        return binding.TextureNames.Count > 0 || twoSided || useVertexColor;
    }

    public static void CollectSidecarTextures(string staticMeshReference, List<Texture2D> result)
    {
        if (result == null)
        {
            return;
        }

        List<string> names = Ensure(staticMeshReference).TextureNames;
        for (int i = 0; i < names.Count; i++)
        {
            Texture2D texture = L2EffectImportUtil.LoadImportedTexture(names[i]) ??
                                L2EffectGeneratorViewerImport.TryImportTexture(names[i]);
            if (texture != null && !result.Contains(texture))
            {
                result.Add(texture);
            }
        }
    }

    public static void CopyExtractedPngs(string extractDir, string meshObjectName)
    {
        if (string.IsNullOrEmpty(extractDir) || !Directory.Exists(extractDir))
        {
            return;
        }

        string[] pngs = L2EffectImportUtil.GetFilesSafe(extractDir, "*.png");
        for (int i = 0; i < pngs.Length; i++)
        {
            string name = Path.GetFileNameWithoutExtension(pngs[i]);
            string destPath = L2EffectPackageRoots.TextureDestFolder + "/" + name + ".png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(destPath) != null)
            {
                RememberSidecarName(meshObjectName, name);
                continue;
            }

            if (L2EffectImportUtil.CopyIntoProject(pngs[i], destPath))
            {
                RememberSidecarName(meshObjectName, name);
            }
        }
    }

    public static void RememberSidecarTextures(string meshObjectName, string extractDir, string pskPath)
    {
        var names = new List<string>();
        CollectHintNames(extractDir, names);
        string props = Path.ChangeExtension(pskPath, ".props.txt");
        if (File.Exists(props))
        {
            CollectHintNamesFromText(File.ReadAllText(props), names);
        }

        for (int i = 0; i < names.Count; i++)
        {
            RememberSidecarName(meshObjectName, names[i]);
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(
                    L2EffectPackageRoots.TextureDestFolder + "/" + names[i] + ".png") == null)
            {
                L2EffectGeneratorViewerImport.TryImportTexture(names[i]);
            }
        }
    }

    static void CollectHintNames(string dir, List<string> names)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            return;
        }

        string[] logs = L2EffectImportUtil.GetFilesSafe(dir, "*.txt");
        for (int i = 0; i < logs.Length; i++)
        {
            CollectHintNamesFromText(File.ReadAllText(logs[i]), names);
        }
    }

    static void CollectHintNamesFromText(string text, List<string> names)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        foreach (Match match in TextureImportHint.Matches(text))
        {
            if (!L2EffectImportUtil.TryParseRef(match.Groups[1].Value, out _, out string name) ||
                string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (!names.Exists(existing =>
                    string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
            {
                names.Add(name);
            }
        }
    }

    static void RememberSidecarName(string meshObjectName, string textureName)
    {
        if (string.IsNullOrEmpty(meshObjectName) || string.IsNullOrEmpty(textureName))
        {
            return;
        }

        if (!MeshSidecarTextures.TryGetValue(meshObjectName, out List<string> names))
        {
            names = new List<string>();
            MeshSidecarTextures[meshObjectName] = names;
        }

        if (!names.Exists(existing =>
                string.Equals(existing, textureName, StringComparison.OrdinalIgnoreCase)))
        {
            names.Add(textureName);
        }
    }

    static void MergeSidecarNames(string objectName, L2EffectMeshPackageBinding binding)
    {
        if (!MeshSidecarTextures.TryGetValue(objectName, out List<string> names))
        {
            return;
        }

        for (int i = 0; i < names.Count; i++)
        {
            AddTextureName(binding, names[i], names[i]);
        }
    }

    static void ParseMeshProps(string text, L2EffectMeshPackageBinding binding)
    {
        if (string.IsNullOrEmpty(text) || binding == null)
        {
            return;
        }

        if (TwoSidedHint.IsMatch(text))
        {
            binding.TwoSided = true;
        }

        if (UseVertexColorHint.IsMatch(text))
        {
            binding.UseVertexColor = true;
        }

        foreach (Match match in MaterialSlotHint.Matches(text))
        {
            binding.SectionCount++;
            string cls = match.Groups["cls"].Value;
            string path = match.Groups["path"].Value;
            if (string.Equals(cls, "Texture", StringComparison.OrdinalIgnoreCase))
            {
                AddTextureRef(binding, path);
            }
            else
            {
                AddUnique(binding.TextureReferences, cls + "'" + path + "'");
            }
        }

        foreach (Match match in TextureImportHint.Matches(text))
        {
            AddTextureRef(binding, match.Groups[1].Value);
        }
    }

    static void ExpandLayeredMaterials(string meshObjectName, L2EffectMeshPackageBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        var pending = new List<string>(binding.TextureReferences);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < pending.Count; i++)
        {
            if (!TryParseClassRef(pending[i], out string cls, out string path))
            {
                continue;
            }

            if (string.Equals(cls, "Texture", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            WalkMaterial(meshObjectName, cls, path, binding, visited, 0);
        }
    }

    static void WalkMaterial(
        string meshObjectName,
        string className,
        string reference,
        L2EffectMeshPackageBinding binding,
        HashSet<string> visited,
        int depth)
    {
        if (binding == null ||
            depth > 8 ||
            !L2EffectImportUtil.TryParseRef(reference, out string package, out string objectName) ||
            string.IsNullOrEmpty(objectName) ||
            !visited.Add(className + ":" + objectName))
        {
            return;
        }

        if (string.Equals(className, "Texture", StringComparison.OrdinalIgnoreCase))
        {
            AddTextureRef(binding, reference);
            return;
        }

        string key = className + ":" + objectName;
        if (MissingMaterialKeys.Contains(key))
        {
            return;
        }

        string props = L2EffectUModelClient.ExtractMaterialProps(package, objectName, className);
        if (string.IsNullOrEmpty(props) || !File.Exists(props))
        {
            MissingMaterialKeys.Add(key);
            return;
        }

        string materialDir = Path.Combine(
            L2EffectPackageRoots.ExtractRoot(),
            "Materials",
            L2EffectImportUtil.Sanitize(objectName));
        CopyExtractedPngs(materialDir, meshObjectName);
        string text = File.ReadAllText(props);
        if (string.Equals(className, "Shader", StringComparison.OrdinalIgnoreCase))
        {
            WalkShaderInputs(meshObjectName, text, binding, visited, depth);
            return;
        }

        foreach (Match match in NestedMaterialHint.Matches(text))
        {
            string cls = match.Groups["cls"].Value;
            string path = match.Groups["path"].Value;
            WalkMaterial(meshObjectName, cls, path, binding, visited, depth + 1);
        }
    }

    static void WalkShaderInputs(
        string meshObjectName,
        string text,
        L2EffectMeshPackageBinding binding,
        HashSet<string> visited,
        int depth)
    {
        var byProp = new Dictionary<string, Match>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in NestedMaterialHint.Matches(text))
        {
            byProp[match.Groups["prop"].Value] = match;
        }

        int before = binding.TextureNames.Count;
        WalkNamedInputs(ShaderColorProps, byProp, meshObjectName, binding, visited, depth);
        if (binding.TextureNames.Count == before)
        {
            WalkNamedInputs(ShaderMaskProps, byProp, meshObjectName, binding, visited, depth);
        }
    }

    static void WalkNamedInputs(
        string[] props,
        Dictionary<string, Match> byProp,
        string meshObjectName,
        L2EffectMeshPackageBinding binding,
        HashSet<string> visited,
        int depth)
    {
        for (int i = 0; i < props.Length; i++)
        {
            if (!byProp.TryGetValue(props[i], out Match match))
            {
                continue;
            }

            WalkMaterial(
                meshObjectName,
                match.Groups["cls"].Value,
                match.Groups["path"].Value,
                binding,
                visited,
                depth + 1);
        }
    }

    static bool TryParseClassRef(string raw, out string className, out string path)
    {
        className = string.Empty;
        path = raw;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        int quote = raw.IndexOf('\'');
        if (quote <= 0)
        {
            return L2EffectImportUtil.TryParseRef(raw, out _, out _);
        }

        className = raw.Substring(0, quote).Trim();
        int end = raw.LastIndexOf('\'');
        path = end > quote ? raw.Substring(quote + 1, end - quote - 1) : raw;
        return !string.IsNullOrEmpty(className);
    }

    static void ParseUmodelLog(string log, L2EffectMeshPackageBinding binding)
    {
        if (string.IsNullOrEmpty(log) || binding == null)
        {
            return;
        }

        foreach (Match match in UmodelTextureLoaded.Matches(log))
        {
            AddTextureName(binding, match.Groups["name"].Value, match.Groups["name"].Value);
        }

        foreach (Match match in UmodelTextureExported.Matches(log))
        {
            AddTextureName(binding, match.Groups["name"].Value, match.Groups["name"].Value);
        }

        foreach (Match match in UmodelImportHint.Matches(log))
        {
            string cls = match.Groups["cls"].Value;
            string path = match.Groups["path"].Value;
            if (string.Equals(cls, "Texture", StringComparison.OrdinalIgnoreCase))
            {
                AddTextureRef(binding, path);
            }
            else
            {
                AddUnique(binding.TextureReferences, cls + "'" + path + "'");
            }
        }
    }

    static void CollectExportedPngNames(string extractDir, L2EffectMeshPackageBinding binding)
    {
        if (string.IsNullOrEmpty(extractDir) || !Directory.Exists(extractDir) || binding == null)
        {
            return;
        }

        string[] pngs = L2EffectImportUtil.GetFilesSafe(extractDir, "*.png");
        for (int i = 0; i < pngs.Length; i++)
        {
            AddTextureName(binding, Path.GetFileNameWithoutExtension(pngs[i]), pngs[i]);
        }
    }

    static void CollectPskMaterialNames(string pskPath, L2EffectMeshPackageBinding binding)
    {
        if (string.IsNullOrEmpty(pskPath) || !File.Exists(pskPath) || binding == null)
        {
            return;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(pskPath);
            int offset = 0;
            while (offset + 32 <= bytes.Length)
            {
                string id = Encoding.ASCII.GetString(bytes, offset, 20).TrimEnd('\0', ' ');
                int dataSize = BitConverter.ToInt32(bytes, offset + 24);
                int dataCount = BitConverter.ToInt32(bytes, offset + 28);
                offset += 32;
                if (dataSize <= 0 || dataCount <= 0)
                {
                    continue;
                }

                long dataBytes = (long)dataSize * dataCount;
                if (offset + dataBytes > bytes.Length)
                {
                    break;
                }

                if (string.Equals(id, "MATT0000", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < dataCount; i++)
                    {
                        int nameStart = offset + i * dataSize;
                        int nameLen = Math.Min(64, dataSize);
                        int actualLen = nameLen;
                        for (int n = 0; n < nameLen; n++)
                        {
                            if (bytes[nameStart + n] == 0)
                            {
                                actualLen = n;
                                break;
                            }
                        }

                        string name = Encoding.ASCII.GetString(bytes, nameStart, actualLen);
                        if (!IsDummyPskMaterialName(name))
                        {
                            AddTextureName(binding, name, name);
                        }
                    }

                    return;
                }

                offset += (int)dataBytes;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[L2EffectGenerator] psk material read skipped: " + ex.Message);
        }
    }

    static bool IsDummyPskMaterialName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return true;
        }

        if (name.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("default", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("defaultmaterial", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name.StartsWith("material", StringComparison.OrdinalIgnoreCase))
        {
            string suffix = name.Length > 8 ? name.Substring(8) : string.Empty;
            if (suffix.Length == 0 ||
                (suffix[0] == '_' && int.TryParse(suffix.Substring(1), out _)))
            {
                return true;
            }
        }

        return false;
    }

    static void AddTextureRef(L2EffectMeshPackageBinding binding, string reference)
    {
        if (!L2EffectImportUtil.TryParseRef(reference, out _, out string name) ||
            string.IsNullOrEmpty(name))
        {
            return;
        }

        AddTextureName(binding, name, reference);
    }

    static void AddTextureName(L2EffectMeshPackageBinding binding, string name, string reference)
    {
        name = L2EffectImportUtil.CleanToken(name);
        if (binding == null || string.IsNullOrWhiteSpace(name) || IsDummyPskMaterialName(name))
        {
            return;
        }

        AddUnique(binding.TextureNames, name);
        if (!string.IsNullOrWhiteSpace(reference))
        {
            AddUnique(binding.TextureReferences, reference);
        }
    }

    static void AddUnique(List<string> values, string value)
    {
        if (values == null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!values.Exists(existing =>
                string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
        {
            values.Add(value);
        }
    }
}
#endif
