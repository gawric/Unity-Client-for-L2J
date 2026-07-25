#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class L2EffectUcEmitterParser
{
    private static readonly Regex BeginObjectRegex =
        new Regex(@"^\s*Begin Object Class=(?<class>\w+) Name=(?<name>\w+)\s*$", RegexOptions.Compiled);

    private static readonly Regex MaxParticlesRegex =
        new Regex(@"^\s*MaxParticles=(?<value>-?\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled);

    private static readonly Regex InnerNameRegex =
        new Regex(@"^\s*Name=""(?<name>[^""]+)""\s*$", RegexOptions.Compiled);

    private static readonly Regex StaticMeshRegex =
        new Regex(@"^\s*StaticMesh=StaticMesh'(?<path>[^']+)'\s*$", RegexOptions.Compiled);

    private static readonly Regex InitialParticlesPerSecondRegex =
        new Regex(@"^\s*InitialParticlesPerSecond=(?<value>-?\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled);

    private static readonly Regex LifetimeRangeRegex =
        new Regex(@"^\s*LifetimeRange=\((?<content>[^)]+)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex InitialDelayRangeRegex =
        new Regex(@"^\s*InitialDelayRange=\((?<content>[^)]+)\)\s*$", RegexOptions.Compiled);

    private static readonly Regex UcMinMaxValueRegex =
        new Regex(@"(?<field>Min|Max)=(?<value>-?\d+(?:\.\d+)?)", RegexOptions.Compiled);

    public sealed class UcEmitterDefinition
    {
        public string ClassName;
        public string EmitterName;
        public string ParticleSlotName;
        public string StaticMeshReference;
        public int MaxParticles = 1;
        public bool HasInitialParticlesPerSecond;
        public int InitialParticlesPerSecond;
        public bool HasLifetimeRange;
        public float LifetimeMin;
        public float LifetimeMax;
        public bool HasInitialDelayRange;
        public float InitialDelayMin;
        public float InitialDelayMax;
    }

    public static bool TryParseFile(string ucAssetPath, out List<UcEmitterDefinition> emitters, out string errorMessage)
    {
        emitters = new List<UcEmitterDefinition>();
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

        emitters = ParseText(File.ReadAllText(fullPath));
        if (emitters.Count == 0)
        {
            errorMessage = "No emitter blocks found in UC file: " + ucAssetPath;
            return false;
        }

        return true;
    }

    public static List<UcEmitterDefinition> ParseText(string ucText)
    {
        var emitters = new List<UcEmitterDefinition>();
        if (string.IsNullOrEmpty(ucText))
        {
            return emitters;
        }

        UcEmitterDefinition current = null;
        string[] lines = ucText.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            Match beginMatch = BeginObjectRegex.Match(line);
            if (beginMatch.Success)
            {
                if (current != null)
                {
                    FinalizeEmitter(current);
                    emitters.Add(current);
                }

                current = new UcEmitterDefinition
                {
                    ClassName = beginMatch.Groups["class"].Value,
                    EmitterName = beginMatch.Groups["name"].Value,
                    MaxParticles = 1
                };
                continue;
            }

            if (current == null)
            {
                continue;
            }

            if (line.TrimStart().StartsWith("End Object", StringComparison.Ordinal))
            {
                FinalizeEmitter(current);
                emitters.Add(current);
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
            }
        }

        if (current != null)
        {
            FinalizeEmitter(current);
            emitters.Add(current);
        }

        return emitters;
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
    }

    private static bool TryParseFloat(string value, out float parsedValue)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue);
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
