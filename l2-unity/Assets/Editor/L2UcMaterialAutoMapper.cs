#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies common Lineage 2 UE3 .uc emitter parameters to selected Unity materials.
/// Usage:
///   1) In Project window select exactly one .uc asset and one or more .mat assets.
///   2) Run Tools/L2 Effects/Apply selected UC to selected Materials.
///   3) The tool matches Material.name to Begin Object Name=... in the .uc file.
///      Suffixes like _Left, _Right and trailing clone suffixes are also tried.
///
/// This is intentionally conservative: it only writes properties that exist on the material's shader.
/// It does not change textures, shader assignment, prefab transforms, or renderer settings.
/// </summary>
public static class L2UcMaterialAutoMapper
{
    private const float UuToUnity = 0.01f;

    private sealed class Emitter
    {
        public string ClassName;
        public string Name;
        public Dictionary<string, string> Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    [MenuItem("Tools/L2 Effects/Apply selected UC to selected Materials")]
    public static void ApplySelectedUcToSelectedMaterials()
    {
        var selected = Selection.objects;
        string ucPath = null;
        var materials = new List<Material>();

        foreach (var obj in selected)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;

            if (path.EndsWith(".uc", StringComparison.OrdinalIgnoreCase))
            {
                if (ucPath != null)
                {
                    EditorUtility.DisplayDialog("L2 UC Mapper", "Select only one .uc file.", "OK");
                    return;
                }
                ucPath = path;
            }
            else if (obj is Material mat)
            {
                materials.Add(mat);
            }
        }

        if (ucPath == null || materials.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "L2 UC Mapper",
                "Select one .uc asset and one or more .mat assets, then run the menu command again.",
                "OK");
            return;
        }

        var ucText = File.ReadAllText(ucPath);
        var emitters = ParseEmitters(ucText);
        if (emitters.Count == 0)
        {
            EditorUtility.DisplayDialog("L2 UC Mapper", "No Begin Object emitters found in: " + ucPath, "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "L2 UC Mapper",
                "Apply UC values from:\n" + ucPath + "\n\nto " + materials.Count + " selected materials?\n\nOnly existing shader properties will be changed. Undo is supported.",
                "Apply",
                "Cancel"))
        {
            return;
        }

        int matched = 0;
        int changed = 0;
        var log = new System.Text.StringBuilder();

        foreach (var mat in materials)
        {
            var emitter = FindEmitterForMaterial(mat.name, emitters);
            if (emitter == null)
            {
                log.AppendLine("NO MATCH: " + mat.name);
                continue;
            }

            Undo.RecordObject(mat, "Apply L2 UC to Material");
            int n = ApplyEmitterToMaterial(emitter, mat);
            EditorUtility.SetDirty(mat);
            matched++;
            changed += n;
            log.AppendLine(mat.name + " <= " + emitter.Name + " (" + n + " property writes)");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("L2 UC Mapper finished.\n" + log);
        EditorUtility.DisplayDialog(
            "L2 UC Mapper",
            "Done. Matched materials: " + matched + "/" + materials.Count + "\nProperty writes: " + changed + "\nDetails are in Console.",
            "OK");
    }

    private static Dictionary<string, Emitter> ParseEmitters(string ucText)
    {
        var result = new Dictionary<string, Emitter>(StringComparer.OrdinalIgnoreCase);
        var re = new Regex(@"Begin\s+Object\s+Class=(\w+)\s+Name=(\w+)(.*?)\n\s*End\s+Object", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        foreach (Match m in re.Matches(ucText))
        {
            var e = new Emitter { ClassName = m.Groups[1].Value, Name = m.Groups[2].Value };
            var body = m.Groups[3].Value;
            foreach (var raw in body.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("//")) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                e.Fields[key] = val;
            }
            result[e.Name] = e;
        }
        return result;
    }

    private static Emitter FindEmitterForMaterial(string materialName, Dictionary<string, Emitter> emitters)
    {
        string n = materialName;
        if (emitters.TryGetValue(n, out var exact)) return exact;

        // Common hand-authored duplicates: SpriteEmitter8_Left/Right should use SpriteEmitter8.
        string withoutSide = Regex.Replace(n, @"_(Left|Right)$", "", RegexOptions.IgnoreCase);
        if (emitters.TryGetValue(withoutSide, out var side)) return side;

        // Common material clones: MeshEmitter34_1 / MeshEmitter34_2 can use MeshEmitter34 if exact emitter is absent.
        string withoutNumericClone = Regex.Replace(n, @"_\d+$", "");
        if (emitters.TryGetValue(withoutNumericClone, out var clone)) return clone;

        return null;
    }

    private static int ApplyEmitterToMaterial(Emitter e, Material mat)
    {
        int changed = 0;

        changed += SetFloatIfField(mat, e, "FadeIn", "_FadeIn", Bool01);
        changed += SetFloatIfField(mat, e, "FadeOut", "_Fadeout", Bool01);
        changed += SetFloatIfField(mat, e, "FadeInEndTime", "_FadeInEndTime", FirstNumber);
        changed += SetFloatIfField(mat, e, "FadeOutStartTime", "_FadeoutStartTime", FirstNumber);
        changed += SetFloatIfField(mat, e, "Opacity", "_Opacity", FirstNumber);
        changed += SetFloatIfField(mat, e, "Opacity", "_Alpha", FirstNumber);

        changed += SetFloatIfField(mat, e, "TextureUSubdivisions", "_TextureUSubdivisions", FirstNumber);
        changed += SetFloatIfField(mat, e, "TextureVSubdivisions", "_TextureVSubdivisions", FirstNumber);
        changed += SetFloatIfField(mat, e, "SubdivisionStart", "_SubdivisionStart", FirstNumber);
        changed += SetFloatIfField(mat, e, "SubdivisionEnd", "_SubdivisionEnd", FirstNumber);
        changed += SetFloatIfField(mat, e, "UseRandomSubdivision", "_UseRandomSubdivision", Bool01);
        changed += SetFloatIfField(mat, e, "BlendBetweenSubdivisions", "_BlendBetweenSubdivisions", Bool01);

        changed += SetFloatIfField(mat, e, "SpinParticles", "_SpinParticles", Bool01);
        changed += SetFloatIfField(mat, e, "UseSizeScale", "_UseSizeScale", Bool01);
        changed += SetFloatIfField(mat, e, "UniformSize", "_UniformSize", Bool01);
        changed += SetFloatIfField(mat, e, "ColorScaleRepeats", "_ColorScaleRepeats", FirstNumber);
        changed += SetFloatIfField(mat, e, "SizeScaleRepeats", "_SizeScaleRepeats", FirstNumber);

        if (e.Fields.ContainsKey("LifetimeRange"))
        {
            var r = ParseMinMax(e.Fields["LifetimeRange"]);
            changed += SetVector(mat, "_LifetimeRange", new Vector4(r.x, r.y, 0, 0));
            changed += SetFloat(mat, "_HasLifetime", 1f);
        }

        if (e.Fields.ContainsKey("InitialDelayRange"))
        {
            var r = ParseMinMax(e.Fields["InitialDelayRange"]);
            changed += SetVector(mat, "_InitialDelayRange", new Vector4(r.x, r.y, 0, 0));
        }

        if (e.Fields.ContainsKey("StartSizeRange"))
        {
            var x = ParseAxisMinMax(e.Fields["StartSizeRange"], "X");
            var y = ParseAxisMinMax(e.Fields["StartSizeRange"], "Y");
            var z = ParseAxisMinMax(e.Fields["StartSizeRange"], "Z");
            changed += SetVector(mat, "_SizeRange", new Vector4(x.x, x.y, 0, 0));
            changed += SetVector(mat, "_SizeRangeX", new Vector4(x.x, x.y, 0, 0));
            changed += SetVector(mat, "_SizeRangeY", new Vector4(y.x, y.y, 0, 0));
            changed += SetVector(mat, "_SizeRangeZ", new Vector4(z.x, z.y, 0, 0));
        }

        if (e.Fields.ContainsKey("StartLocationOffset"))
        {
            var v = ParseUcVector(e.Fields["StartLocationOffset"], 0f);
            // UE3: X right, Y forward, Z up. Unity: X right, Y up, Z forward.
            changed += SetVector(mat, "_StartLocationOffset", new Vector4(v.x * UuToUnity, v.z * UuToUnity, v.y * UuToUnity, 0));
        }

        if (e.Fields.ContainsKey("StartLocationPolarRange"))
        {
            var ax = ParseAxisMinMax(e.Fields["StartLocationPolarRange"], "X");
            var ay = ParseAxisMinMax(e.Fields["StartLocationPolarRange"], "Y");
            var az = ParseAxisMinMax(e.Fields["StartLocationPolarRange"], "Z");
            changed += SetVector(mat, "_StartLocationPolarRangeX", new Vector4(ax.x, ax.y, 0, 0));
            changed += SetVector(mat, "_StartLocationPolarRangeY", new Vector4(ay.x, ay.y, 0, 0));
            changed += SetVector(mat, "_StartLocationPolarRangeZ", new Vector4(az.x, az.y, 0, 0));
            changed += SetVector(mat, "_PolarAzimuthDeg", new Vector4(ax.x, ax.y, 0, 0));
            changed += SetVector(mat, "_PolarPitchDeg", new Vector4(ay.x, ay.y, 0, 0));
            changed += SetVector(mat, "_PolarRadius", new Vector4(az.x, az.y, 0, 0));
        }

        if (e.Fields.ContainsKey("StartLocationRange"))
        {
            var x = ParseAxisMinMax(e.Fields["StartLocationRange"], "X");
            var y = ParseAxisMinMax(e.Fields["StartLocationRange"], "Y");
            var z = ParseAxisMinMax(e.Fields["StartLocationRange"], "Z");
            changed += SetVector(mat, "_StartLocationRangeX", new Vector4(x.x * UuToUnity, x.y * UuToUnity, 0, 0));
            changed += SetVector(mat, "_StartLocationRangeY", new Vector4(z.x * UuToUnity, z.y * UuToUnity, 0, 0));
            changed += SetVector(mat, "_StartLocationRangeZ", new Vector4(y.x * UuToUnity, y.y * UuToUnity, 0, 0));
            changed += SetVector(mat, "_StartLocationRangeXY", new Vector4(x.x, x.y, y.x, y.y));
        }

        if (e.Fields.ContainsKey("StartVelocityRange"))
        {
            var x = ParseAxisMinMax(e.Fields["StartVelocityRange"], "X");
            var y = ParseAxisMinMax(e.Fields["StartVelocityRange"], "Y");
            var z = ParseAxisMinMax(e.Fields["StartVelocityRange"], "Z");
            changed += SetVector(mat, "_StartVelocityRangeX", new Vector4(x.x * UuToUnity, x.y * UuToUnity, 0, 0));
            changed += SetVector(mat, "_StartVelocityRangeY", new Vector4(z.x * UuToUnity, z.y * UuToUnity, 0, 0));
            changed += SetVector(mat, "_StartVelocityRangeZ", new Vector4(y.x * UuToUnity, y.y * UuToUnity, 0, 0));
            changed += SetVector(mat, "_StartVelocityRange", new Vector4(x.x, x.y, y.x, y.y));
        }

        if (e.Fields.ContainsKey("VelocityLossRange"))
        {
            var x = ParseAxisMinMax(e.Fields["VelocityLossRange"], "X");
            changed += SetVector(mat, "_VelocityLossRange", new Vector4(x.x, x.y, 0, 0));
            changed += SetFloat(mat, "_UseVelocityLoss", 1f);
        }

        if (e.Fields.ContainsKey("SpinsPerSecondRange"))
        {
            var x = ParseAxisMinMax(e.Fields["SpinsPerSecondRange"], "X");
            var y = ParseAxisMinMax(e.Fields["SpinsPerSecondRange"], "Y");
            var z = ParseAxisMinMax(e.Fields["SpinsPerSecondRange"], "Z");
            changed += SetVector(mat, "_SpinsPerSecondRange", new Vector4(x.x, x.y, 0, 0));
            changed += SetVector(mat, "_SpinsPerSecondRangeX", new Vector4(x.x, x.y, 0, 0));
            changed += SetVector(mat, "_SpinsPerSecondRangeY", new Vector4(y.x, y.y, 0, 0));
            changed += SetVector(mat, "_SpinsPerSecondRangeZ", new Vector4(z.x, z.y, 0, 0));
        }

        if (e.Fields.ContainsKey("StartSpinRange"))
        {
            var x = ParseAxisMinMax(e.Fields["StartSpinRange"], "X");
            var y = ParseAxisMinMax(e.Fields["StartSpinRange"], "Y");
            var z = ParseAxisMinMax(e.Fields["StartSpinRange"], "Z");
            changed += SetVector(mat, "_StartSpinRange", new Vector4(x.x, x.y, 0, 0));
            changed += SetVector(mat, "_StartSpinRangeX", new Vector4(x.x, x.y, 0, 0));
            changed += SetVector(mat, "_StartSpinRangeY", new Vector4(y.x, y.y, 0, 0));
            changed += SetVector(mat, "_StartSpinRangeZ", new Vector4(z.x, z.y, 0, 0));
        }

        changed += ApplyColorScales(e, mat);
        changed += ApplySizeScales(e, mat);
        changed += ApplyColorMultiplier(e, mat);

        return changed;
    }

    private static int ApplyColorScales(Emitter e, Material mat)
    {
        int changed = 0;
        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            string key = "ColorScale(" + i + ")";
            if (!e.Fields.TryGetValue(key, out var value)) continue;
            count++;
            float t = ExtractNamedFloat(value, "RelativeTime", i == 0 ? 0f : 1f);
            var c = ParseUcColor(value);

            changed += SetColor(mat, "_ColorScale" + i, c);
            changed += SetColor(mat, "_ColorScale" + i + "Color", c);

            if (i == 0)
                changed += SetFloat(mat, "_ColorScale0Time", t);
            else
                changed += SetFloat(mat, "_ColorScaleTime" + i, t);
            changed += SetFloat(mat, "_ColorScale" + i + "Time", t);
        }
        if (count > 0)
        {
            changed += SetFloat(mat, "_UseColorScale", 1f);
            changed += SetFloat(mat, "_ColorScaleCount", count);
        }
        return changed;
    }

    private static int ApplySizeScales(Emitter e, Material mat)
    {
        int changed = 0;
        for (int i = 0; i < 10; i++)
        {
            string key = "SizeScale(" + i + ")";
            if (!e.Fields.TryGetValue(key, out var value)) continue;
            float t = ExtractNamedFloat(value, "RelativeTime", i == 0 ? 0f : 1f);
            float s = ExtractNamedFloat(value, "RelativeSize", 1f);
            changed += SetVector(mat, "_SizeScale" + i, new Vector4(t, s, 0, 0));
        }
        return changed;
    }

    private static int ApplyColorMultiplier(Emitter e, Material mat)
    {
        if (!e.Fields.TryGetValue("ColorMultiplierRange", out var value)) return 0;
        int changed = 0;
        var r = ParseAxisMinMax(value, "X");
        var g = ParseAxisMinMax(value, "Y");
        var b = ParseAxisMinMax(value, "Z");
        changed += SetColor(mat, "_ColorMultMin", new Color(r.x, g.x, b.x, 1));
        changed += SetColor(mat, "_ColorMultMax", new Color(r.y, g.y, b.y, 1));
        changed += SetVector(mat, "_ColorMultiplierRangeR", new Vector4(r.x, r.y, 0, 0));
        changed += SetVector(mat, "_ColorMultiplierRangeG", new Vector4(g.x, g.y, 0, 0));
        changed += SetVector(mat, "_ColorMultiplierRangeB", new Vector4(b.x, b.y, 0, 0));
        return changed;
    }

    private static int SetFloatIfField(Material mat, Emitter e, string ucField, string prop, Func<string, float> convert)
    {
        return e.Fields.TryGetValue(ucField, out var value) ? SetFloat(mat, prop, convert(value)) : 0;
    }

    private static int SetFloat(Material mat, string prop, float value)
    {
        if (!mat.HasProperty(prop)) return 0;
        mat.SetFloat(prop, value);
        return 1;
    }

    private static int SetVector(Material mat, string prop, Vector4 value)
    {
        if (!mat.HasProperty(prop)) return 0;
        mat.SetVector(prop, value);
        return 1;
    }

    private static int SetColor(Material mat, string prop, Color value)
    {
        if (!mat.HasProperty(prop)) return 0;
        mat.SetColor(prop, value);
        return 1;
    }

    private static float Bool01(string s)
    {
        return s.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0 ? 1f : 0f;
    }

    private static float FirstNumber(string s)
    {
        var m = Regex.Match(s, @"[-+]?\d+(?:\.\d+)?");
        return m.Success ? ParseFloat(m.Value) : 0f;
    }

    private static float ExtractNamedFloat(string s, string name, float defaultValue)
    {
        var m = Regex.Match(s, name + @"=([-+]?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        return m.Success ? ParseFloat(m.Groups[1].Value) : defaultValue;
    }

    private static Vector2 ParseMinMax(string s)
    {
        float min = ExtractNamedFloat(s, "Min", 0f);
        float max = ExtractNamedFloat(s, "Max", float.NaN);
        if (float.IsNaN(max)) max = min;
        return new Vector2(min, max);
    }

    private static Vector2 ParseAxisMinMax(string s, string axis)
    {
        string sub = ExtractParenValue(s, axis);
        if (string.IsNullOrEmpty(sub)) return Vector2.zero;
        return ParseMinMax(sub);
    }

    private static Vector3 ParseUcVector(string s, float defaultValue)
    {
        return new Vector3(
            ExtractNamedFloat(s, "X", defaultValue),
            ExtractNamedFloat(s, "Y", defaultValue),
            ExtractNamedFloat(s, "Z", defaultValue));
    }

    private static Color ParseUcColor(string s)
    {
        string c = ExtractParenValue(s, "Color");
        if (string.IsNullOrEmpty(c)) c = s;
        float r = ExtractNamedFloat(c, "R", 255f) / 255f;
        float g = ExtractNamedFloat(c, "G", 255f) / 255f;
        float b = ExtractNamedFloat(c, "B", 255f) / 255f;
        float a = ExtractNamedFloat(c, "A", 255f) / 255f;
        return new Color(r, g, b, a);
    }

    private static string ExtractParenValue(string s, string name)
    {
        int start = s.IndexOf(name + "=", StringComparison.OrdinalIgnoreCase);
        if (start < 0) return null;
        int open = s.IndexOf('(', start);
        if (open < 0) return null;
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '(') depth++;
            else if (s[i] == ')')
            {
                depth--;
                if (depth == 0) return s.Substring(open + 1, i - open - 1);
            }
        }
        return null;
    }

    private static float ParseFloat(string s)
    {
        return float.Parse(s, CultureInfo.InvariantCulture);
    }
}
#endif
