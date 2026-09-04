#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class L2EffectUcEmitterParser
{
    private static readonly Regex ClassHeaderRegex =
        new Regex(@"^\s*class\s+(?<name>\w+)\s+extends\s+(?<base>\w+)\s*;?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BeginObjectRegex =
        new Regex(@"^\s*Begin Object Class=(?<class>\w+) Name=(?<name>\w+)\s*$", RegexOptions.Compiled);

    private static readonly Regex MaxParticlesRegex =
        new Regex(@"^\s*MaxParticles=(?<value>-?\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled);

    private static readonly Regex InnerNameRegex =
        new Regex(@"^\s*Name=""(?<name>[^""]+)""\s*$", RegexOptions.Compiled);

    private static readonly Regex StaticMeshRegex =
        new Regex(@"^\s*StaticMesh=StaticMesh'(?<path>[^']+)'\s*$", RegexOptions.Compiled);

    private static readonly Regex VertexMeshRegex =
        new Regex(@"^\s*VertexMesh=VertMesh'(?<path>[^']+)'\s*$", RegexOptions.Compiled);

    private static readonly Regex TextureRegex =
        new Regex(@"^\s*Texture=Texture'(?<path>[^']+)'\s*$", RegexOptions.Compiled);

    private static readonly Regex CustomMaterialTextureRegex =
        new Regex(@"^\s*CustomMaterials\(\d+\)=Texture'(?<path>[^']+)'\s*$", RegexOptions.Compiled);

    private static readonly Regex InitialParticlesPerSecondRegex =
        new Regex(@"^\s*InitialParticlesPerSecond=(?<value>-?\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled);

    private static readonly Regex LifetimeRangeRegex =
        new Regex(@"^\s*LifetimeRange=\((?<content>[^)]+)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex InitialDelayRangeRegex =
        new Regex(@"^\s*InitialDelayRange=\((?<content>[^)]+)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex UcMinMaxValueRegex =
        new Regex(@"(?<field>Min|Max)=(?<value>-?\d+(?:\.\d+)?)", RegexOptions.Compiled);

    private static readonly Regex UcAxisRangeRegex =
        new Regex(@"(?<axis>[XYZ])=\((?<content>[^)]*)\)", RegexOptions.Compiled);

    private static readonly Regex UcVectorValueRegex =
        new Regex(@"(?<axis>[XYZ])=(?<value>-?\d+(?:\.\d+)?)", RegexOptions.Compiled);

    private static readonly Regex ColorScaleRegex =
        new Regex(@"^\s*ColorScale\((?<index>\d+)\)=\((?<content>.*)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex SizeScaleRegex =
        new Regex(@"^\s*SizeScale\((?<index>\d+)\)=\((?<content>.*)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex VelocityScaleRegex =
        new Regex(@"^\s*VelocityScale\((?<index>\d+)\)=\((?<content>.*)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex RevolutionScaleRegex =
        new Regex(@"^\s*RevolutionScale\((?<index>\d+)\)=\((?<content>.*)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex BeamEndPointsRegex =
        new Regex(@"^\s*BeamEndPoints\(\d+\)=\((?<content>.*)\)\s*$", RegexOptions.Compiled);

    public static bool TryParseFile(string ucAssetPath, out List<UcEmitterDefinition> emitters, out string errorMessage)
    {
        emitters = new List<UcEmitterDefinition>();
        if (!TryParseFileInfo(ucAssetPath, out UcFileInfo info, out errorMessage))
        {
            return false;
        }

        emitters = info.Emitters;
        return true;
    }

    public static bool TryParseFileInfo(string ucAssetPath, out UcFileInfo info, out string errorMessage)
    {
        info = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(ucAssetPath))
        {
            errorMessage = "UC asset path is empty.";
            return false;
        }

        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ucAssetPath));
        if (!File.Exists(fullPath))
        {
            errorMessage = "UC file not found: " + ucAssetPath;
            return false;
        }

        info = ParseFileText(File.ReadAllText(fullPath));
        if (string.IsNullOrWhiteSpace(info.ClassName))
        {
            info.ClassName = Path.GetFileNameWithoutExtension(ucAssetPath);
        }

        if (info.Emitters.Count == 0)
        {
            errorMessage = "No emitter blocks found in UC file: " + ucAssetPath;
            return false;
        }

        return true;
    }

    public static List<UcEmitterDefinition> ParseText(string ucText)
    {
        return ParseFileText(ucText).Emitters;
    }

    public static UcFileInfo ParseFileText(string ucText)
    {
        var info = new UcFileInfo();
        if (string.IsNullOrEmpty(ucText))
        {
            return info;
        }

        UcEmitterDefinition current = null;
        string[] lines = ucText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            Match classMatch = ClassHeaderRegex.Match(line);
            if (classMatch.Success && string.IsNullOrEmpty(info.ClassName))
            {
                info.ClassName = classMatch.Groups["name"].Value;
                info.ExtendsClass = classMatch.Groups["base"].Value;
                continue;
            }

            Match beginMatch = BeginObjectRegex.Match(line);
            if (beginMatch.Success)
            {
                if (current != null)
                {
                    FinalizeEmitter(current);
                    info.Emitters.Add(current);
                }

                current = new UcEmitterDefinition
                {
                    ClassName = beginMatch.Groups["class"].Value,
                    EmitterName = beginMatch.Groups["name"].Value
                };
                continue;
            }

            if (current == null)
            {
                TryParseFileLevelProperty(info, line);
                continue;
            }

            if (line.TrimStart().StartsWith("End Object", StringComparison.Ordinal))
            {
                FinalizeEmitter(current);
                info.Emitters.Add(current);
                current = null;
                continue;
            }

            Match innerNameMatch = InnerNameRegex.Match(line);
            if (innerNameMatch.Success)
            {
                current.ParticleSlotName = innerNameMatch.Groups["name"].Value;
                continue;
            }

            Match staticMeshMatch = StaticMeshRegex.Match(line);
            if (staticMeshMatch.Success)
            {
                current.StaticMeshReference = staticMeshMatch.Groups["path"].Value;
                continue;
            }

            Match vertexMeshMatch = VertexMeshRegex.Match(line);
            if (vertexMeshMatch.Success)
            {
                current.StaticMeshReference = vertexMeshMatch.Groups["path"].Value;
                continue;
            }

            Match textureMatch = TextureRegex.Match(line);
            if (textureMatch.Success)
            {
                current.TextureReference = textureMatch.Groups["path"].Value;
                continue;
            }

            Match customMaterialMatch = CustomMaterialTextureRegex.Match(line);
            if (customMaterialMatch.Success && string.IsNullOrWhiteSpace(current.TextureReference))
            {
                current.TextureReference = customMaterialMatch.Groups["path"].Value;
                continue;
            }

            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("Texture=", StringComparison.Ordinal) &&
                trimmedLine.IndexOf("None", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                current.TextureReference = null;
                continue;
            }

            Match maxParticlesMatch = MaxParticlesRegex.Match(line);
            if (maxParticlesMatch.Success &&
                int.TryParse(
                    maxParticlesMatch.Groups["value"].Value.Split('.')[0],
                    out int maxParticles))
            {
                current.MaxParticles = Math.Max(0, maxParticles);
                continue;
            }

            Match initialParticlesPerSecondMatch = InitialParticlesPerSecondRegex.Match(line);
            if (initialParticlesPerSecondMatch.Success &&
                TryParseFloat(initialParticlesPerSecondMatch.Groups["value"].Value, out float initialParticlesPerSecond))
            {
                current.HasInitialParticlesPerSecond = true;
                current.InitialParticlesPerSecond = Math.Max(0, (int)Math.Round(initialParticlesPerSecond));
                continue;
            }

            Match lifetimeRangeMatch = LifetimeRangeRegex.Match(line);
            if (lifetimeRangeMatch.Success &&
                TryParseMinMaxRange(
                    lifetimeRangeMatch.Groups["content"].Value,
                    out float lifetimeMin,
                    out float lifetimeMax,
                    out bool hasLifetimeMin,
                    out bool hasLifetimeMax))
            {
                current.HasLifetimeRange = true;
                current.LifetimeMin = hasLifetimeMin ? lifetimeMin : lifetimeMax;
                current.LifetimeMax = hasLifetimeMax ? lifetimeMax : lifetimeMin;
                continue;
            }

            Match initialDelayRangeMatch = InitialDelayRangeRegex.Match(line);
            if (initialDelayRangeMatch.Success &&
                TryParseMinMaxRange(
                    initialDelayRangeMatch.Groups["content"].Value,
                    out float initialDelayMin,
                    out float initialDelayMax,
                    out bool hasInitialDelayMin,
                    out bool hasInitialDelayMax))
            {
                current.HasInitialDelayRange = true;
                current.InitialDelayMin = hasInitialDelayMin ? initialDelayMin : 0f;
                current.InitialDelayMax = hasInitialDelayMax ? initialDelayMax : initialDelayMin;
                continue;
            }

            if (TryParseEmitterMaterialLine(current, line))
            {
                continue;
            }
        }

        if (current != null)
        {
            FinalizeEmitter(current);
            info.Emitters.Add(current);
        }

        return info;
    }

    private static void TryParseFileLevelProperty(UcFileInfo info, string line)
    {
        if (info == null || !TrySplitAssignment(line.Trim(), out string name, out string value))
        {
            return;
        }

        switch (name)
        {
            case "Speed":
                if (TryParseFloat(value, out float speed))
                {
                    info.HasSpeed = true;
                    info.Speed = speed;
                }

                return;
            case "AccSpeed":
                if (TryParseFloat(value, out float accSpeed))
                {
                    info.HasAccSpeed = true;
                    info.AccSpeed = accSpeed;
                }

                return;
            case "Physics":
                info.Physics = value.Trim().Trim('\'', '"');
                return;
            case "bAcceptsProjectors":
                info.HasAcceptsProjectors = true;
                if (TryAssignBool(value, out bool acceptsProjectors))
                {
                    info.AcceptsProjectors = acceptsProjectors;
                }

                return;
        }
    }

    private static void FinalizeEmitter(UcEmitterDefinition emitter)
    {
        if (emitter == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(emitter.ParticleSlotName))
        {
            emitter.ParticleSlotName = emitter.EmitterName;
        }

        emitter.ColorScaleKeys.Sort((a, b) => a.Index.CompareTo(b.Index));
        emitter.SizeScaleKeys.Sort((a, b) => a.Index.CompareTo(b.Index));
        emitter.VelocityScaleKeys.Sort((a, b) => a.Index.CompareTo(b.Index));
        emitter.RevolutionScaleKeys.Sort((a, b) => a.Index.CompareTo(b.Index));
        if (emitter.HighFrequencyPoints < 2)
        {
            emitter.HighFrequencyPoints = 2;
        }
    }

    private static bool TryParseFloat(string value, out float parsedValue)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue);
    }

    private static bool TryParseEmitterMaterialLine(UcEmitterDefinition emitter, string line)
    {
        string trimmed = line.Trim();

        Match colorScaleMatch = ColorScaleRegex.Match(line);
        if (colorScaleMatch.Success)
        {
            emitter.ColorScaleKeys.Add(ParseColorScaleKey(
                int.Parse(colorScaleMatch.Groups["index"].Value, CultureInfo.InvariantCulture),
                colorScaleMatch.Groups["content"].Value));
            return true;
        }

        Match sizeScaleMatch = SizeScaleRegex.Match(line);
        if (sizeScaleMatch.Success)
        {
            emitter.SizeScaleKeys.Add(ParseSizeScaleKey(
                int.Parse(sizeScaleMatch.Groups["index"].Value, CultureInfo.InvariantCulture),
                sizeScaleMatch.Groups["content"].Value));
            return true;
        }

        Match velocityScaleMatch = VelocityScaleRegex.Match(line);
        if (velocityScaleMatch.Success)
        {
            emitter.VelocityScaleKeys.Add(ParseVectorScaleKey(
                int.Parse(velocityScaleMatch.Groups["index"].Value, CultureInfo.InvariantCulture),
                velocityScaleMatch.Groups["content"].Value,
                "RelativeVelocity"));
            return true;
        }

        Match revolutionScaleMatch = RevolutionScaleRegex.Match(line);
        if (revolutionScaleMatch.Success)
        {
            emitter.RevolutionScaleKeys.Add(ParseVectorScaleKey(
                int.Parse(revolutionScaleMatch.Groups["index"].Value, CultureInfo.InvariantCulture),
                revolutionScaleMatch.Groups["content"].Value,
                "RelativeRevolution"));
            return true;
        }

        Match beamEndMatch = BeamEndPointsRegex.Match(line);
        if (beamEndMatch.Success)
        {
            emitter.BeamEndOffset = ParseBeamEndOffset(
                beamEndMatch.Groups["content"].Value);
            emitter.HasBeamEndOffset = true;
            return true;
        }

        if (!TrySplitAssignment(trimmed, out string name, out string value))
        {
            return false;
        }

        switch (name)
        {
            case "DrawStyle": emitter.DrawStyle = value; return true;
            case "StartLocationShape": emitter.StartLocationShape = value; return true;
            case "UseDirectionAs": emitter.UseDirectionAs = value; return true;
            case "GetVelocityDirectionFrom": emitter.GetVelocityDirectionFrom = value; return true;
            case "UseRotationFrom": emitter.UseRotationFrom = value; return true;
            case "CoordinateSystem": emitter.CoordinateSystem = value; return true;
            case "IndependentSprayAccel":
                return TryAssignBool(value, out emitter.IndependentSprayAccel);
            case "ProjectionNormal":
                emitter.ProjectionNormal = ParseVector(value, Vector3.zero);
                emitter.HasProjectionNormal = true;
                return true;
            case "DetermineEndPointBy": emitter.DetermineEndPointBy = value; return true;
            case "HighFrequencyPoints": return TryAssignInt(value, out emitter.HighFrequencyPoints);
            case "LowFrequencyPoints": return TryAssignInt(value, out emitter.LowFrequencyPoints);
            case "RenderTwoSided": return TryAssignBool(value, out emitter.RenderTwoSided);
            case "SpinParticles": return TryAssignBool(value, out emitter.SpinParticles);
            case "UniformSize": return TryAssignBool(value, out emitter.UniformSize);
            case "UseSizeScale": return TryAssignBool(value, out emitter.UseSizeScale);
            case "FadeIn": return TryAssignBool(value, out emitter.FadeIn);
            case "FadeOut": return TryAssignBool(value, out emitter.FadeOut);
            case "UseRandomSubdivision": return TryAssignBool(value, out emitter.UseRandomSubdivision);
            case "BlendBetweenSubdivisions": return TryAssignBool(value, out emitter.BlendBetweenSubdivisions);
            case "UseRevolution": return TryAssignBool(value, out emitter.UseRevolution);
            case "UseRevolutionScale": return TryAssignBool(value, out emitter.UseRevolutionScale);
            case "UseVelocityScale": return TryAssignBool(value, out emitter.UseVelocityScale);
            case "AddLocationFromOtherEmitter":
                return TryAssignInt(value, out emitter.AddLocationFromOtherEmitter);
            case "RespawnDeadParticles": return TryAssignBool(value, out emitter.RespawnDeadParticles);
            case "Opacity": return TryAssignFloat(value, out emitter.Opacity);
            case "FadeInEndTime": return TryAssignFloat(value, out emitter.FadeInEndTime);
            case "FadeOutStartTime": return TryAssignFloat(value, out emitter.FadeOutStartTime);
            case "ColorScaleRepeats": return TryAssignFloat(value, out emitter.ColorScaleRepeats);
            case "SizeScaleRepeats": return TryAssignFloat(value, out emitter.SizeScaleRepeats);
            case "RevolutionScaleRepeats": return TryAssignFloat(value, out emitter.RevolutionScaleRepeats);
            case "VelocityScaleRepeats": return TryAssignFloat(value, out emitter.VelocityScaleRepeats);
            case "TextureUSubdivisions": return TryAssignInt(value, out emitter.TextureUSubdivisions);
            case "TextureVSubdivisions": return TryAssignInt(value, out emitter.TextureVSubdivisions);
            case "SubdivisionStart": return TryAssignInt(value, out emitter.SubdivisionStart);
            case "SubdivisionEnd": return TryAssignInt(value, out emitter.SubdivisionEnd);
            case "Acceleration": emitter.Acceleration = ParseVector(value, Vector3.zero); return true;
            case "StartLocationOffset": emitter.StartLocationOffset = ParseVector(value, Vector3.zero); return true;
            case "SpinCCWorCW":
                emitter.SpinCcwOrCw = ParseVector(value, new Vector3(0.5f, 0.5f, 0.5f));
                return true;
            case "StartLocationRange":
                emitter.StartLocationRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "StartLocationPolarRange":
                emitter.StartLocationPolarRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "StartVelocityRange":
                emitter.StartVelocityRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "MaxAbsVelocity":
                emitter.MaxAbsVelocity = ParseVector(
                    value, new Vector3(10000f, 10000f, 10000f));
                return true;
            case "VelocityLossRange":
                emitter.VelocityLossRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "StartSizeRange":
                emitter.StartSizeRange = ParseVectorRange(value, UniformVectorRange(1f));
                return true;
            case "StartSpinRange":
                emitter.StartSpinRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "SpinsPerSecondRange":
                emitter.SpinsPerSecondRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "RevolutionsPerSecondRange":
                emitter.RevolutionsPerSecondRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "RevolutionCenterOffsetRange":
                emitter.RevolutionCenterOffsetRange = ParseVectorRange(value, UniformVectorRange(0f));
                return true;
            case "SphereRadiusRange":
                if (!TryParseMinMaxRange(
                        value,
                        out float sphereMin,
                        out float sphereMax,
                        out bool hasSphereMin,
                        out bool hasSphereMax) ||
                    (!hasSphereMin && !hasSphereMax))
                {
                    return false;
                }

                emitter.HasSphereRadiusRange = true;
                emitter.SphereRadiusRange = new UcRange(
                    hasSphereMin ? sphereMin : sphereMax,
                    hasSphereMax ? sphereMax : sphereMin);
                return true;
            case "ColorMultiplierRange":
                emitter.ColorMultiplierRange = ParseVectorRange(value, UniformVectorRange(1f));
                return true;
            case "RelativeWarmupTime":
                if (!TryAssignFloat(value, out float relativeWarmupTime))
                {
                    return false;
                }

                emitter.HasRelativeWarmupTime = true;
                emitter.RelativeWarmupTime = relativeWarmupTime;
                return true;
            case "WarmupTicksPerSecond":
                if (!TryAssignFloat(value, out float warmupTicksPerSecond))
                {
                    return false;
                }

                emitter.HasWarmupTicksPerSecond = true;
                emitter.WarmupTicksPerSecond = warmupTicksPerSecond;
                return true;
            default:
                return false;
        }
    }

    private static bool TrySplitAssignment(string line, out string name, out string value)
    {
        int equals = line.IndexOf('=');
        if (equals <= 0)
        {
            name = null;
            value = null;
            return false;
        }

        name = line.Substring(0, equals).Trim();
        value = line.Substring(equals + 1).Trim();
        return true;
    }

    private static bool TryAssignBool(string value, out bool destination)
    {
        if (bool.TryParse(value, out destination))
        {
            return true;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
        {
            destination = integer != 0;
            return true;
        }

        destination = false;
        return false;
    }

    private static bool TryAssignFloat(string value, out float destination)
    {
        return TryParseFloat(value, out destination);
    }

    private static bool TryAssignInt(string value, out int destination)
    {
        if (TryParseFloat(value, out float parsed))
        {
            destination = Math.Max(0, (int)Math.Round(parsed));
            return true;
        }

        destination = 0;
        return false;
    }

    private static UcVectorRange UniformVectorRange(float value)
    {
        return UcVectorRange.Uniform(value);
    }

    private static UcVectorRange ParseVectorRange(string value, UcVectorRange defaults)
    {
        UcVectorRange result = defaults;
        MatchCollection matches = UcAxisRangeRegex.Matches(value);
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            UcRange fallback = match.Groups["axis"].Value == "X"
                ? result.X
                : match.Groups["axis"].Value == "Y" ? result.Y : result.Z;
            UcRange parsed = ParseRange(match.Groups["content"].Value, fallback);
            switch (match.Groups["axis"].Value)
            {
                case "X": result.X = parsed; break;
                case "Y": result.Y = parsed; break;
                case "Z": result.Z = parsed; break;
            }
        }
        return result;
    }

    private static UcRange ParseRange(string content, UcRange fallback)
    {
        if (!TryParseMinMaxRange(
                content,
                out float min,
                out float max,
                out bool hasMin,
                out bool hasMax))
        {
            return fallback;
        }

        return new UcRange(hasMin ? min : fallback.Min, hasMax ? max : fallback.Max);
    }

    private static UcVectorRange ParseBeamEndOffset(string content)
    {
        int offsetIndex = content.IndexOf("offset=", StringComparison.OrdinalIgnoreCase);
        string rangeSource = offsetIndex >= 0
            ? content.Substring(offsetIndex + "offset=".Length)
            : content;
        return ParseVectorRange(rangeSource, UniformVectorRange(0f));
    }

    private static Vector3 ParseVector(string value, Vector3 defaults)
    {
        Vector3 result = defaults;
        MatchCollection matches = UcVectorValueRegex.Matches(value);
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            if (!TryParseFloat(match.Groups["value"].Value, out float parsed))
                continue;
            switch (match.Groups["axis"].Value)
            {
                case "X": result.x = parsed; break;
                case "Y": result.y = parsed; break;
                case "Z": result.z = parsed; break;
            }
        }
        return result;
    }

    private static UcColorScaleKey ParseColorScaleKey(int index, string content)
    {
        float relativeTime = ParseNamedFloat(content, "RelativeTime", 0f);
        Match colorMatch = Regex.Match(content, @"Color=\((?<color>[^)]*)\)");
        string colorContent = colorMatch.Success ? colorMatch.Groups["color"].Value : string.Empty;
        float r = ParseNamedFloat(colorContent, "R", 0f) / 255f;
        float g = ParseNamedFloat(colorContent, "G", 0f) / 255f;
        float b = ParseNamedFloat(colorContent, "B", 0f) / 255f;
        float a = ParseNamedFloat(colorContent, "A", 255f) / 255f;
        return new UcColorScaleKey
        {
            Index = index,
            RelativeTime = relativeTime,
            Color = new Color(r, g, b, a)
        };
    }

    private static UcSizeScaleKey ParseSizeScaleKey(int index, string content)
    {
        return new UcSizeScaleKey
        {
            Index = index,
            RelativeTime = ParseNamedFloat(content, "RelativeTime", 0f),
            RelativeSize = ParseNamedFloat(content, "RelativeSize", 1f)
        };
    }

    private static UcVectorScaleKey ParseVectorScaleKey(
        int index,
        string content,
        string vectorFieldName)
    {
        Match vectorMatch = Regex.Match(
            content,
            Regex.Escape(vectorFieldName) + @"=\((?<vector>[^)]*)\)");
        return new UcVectorScaleKey
        {
            Index = index,
            RelativeTime = ParseNamedFloat(content, "RelativeTime", 0f),
            RelativeValue = ParseVector(
                vectorMatch.Success ? vectorMatch.Groups["vector"].Value : string.Empty,
                Vector3.one)
        };
    }

    private static float ParseNamedFloat(string content, string name, float fallback)
    {
        Match match = Regex.Match(
            content,
            @"(?:^|,)\s*" + Regex.Escape(name) + @"=(?<value>-?\d+(?:\.\d+)?)");
        return match.Success && TryParseFloat(match.Groups["value"].Value, out float parsed)
            ? parsed
            : fallback;
    }

    private static bool TryParseMinMaxRange(
        string content,
        out float minValue,
        out float maxValue,
        out bool hasMin,
        out bool hasMax)
    {
        minValue = 0f;
        maxValue = 0f;
        hasMin = false;
        hasMax = false;

        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        MatchCollection matches = UcMinMaxValueRegex.Matches(content);
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            if (!TryParseFloat(match.Groups["value"].Value, out float parsedValue))
            {
                continue;
            }

            string fieldName = match.Groups["field"].Value;
            if (string.Equals(fieldName, "Min", StringComparison.Ordinal))
            {
                minValue = parsedValue;
                hasMin = true;
            }
            else if (string.Equals(fieldName, "Max", StringComparison.Ordinal))
            {
                maxValue = parsedValue;
                hasMax = true;
            }
        }

        return hasMin || hasMax;
    }
}
#endif
