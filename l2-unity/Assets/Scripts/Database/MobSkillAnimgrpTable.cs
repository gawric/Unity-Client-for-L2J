using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class MobSkillAnimgrpTable
{
    private static MobSkillAnimgrpTable _instance;

    public static MobSkillAnimgrpTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = new MobSkillAnimgrpTable();
            return _instance;
        }
    }

    private readonly Dictionary<long, string> _sequences = new Dictionary<long, string>();

    public void Initialize()
    {
        _sequences.Clear();

        string dataPath = Path.Combine(
            Application.streamingAssetsPath,
            "Data/Meta/MobSkillAnimgrp_Interlude.txt");
        if (!File.Exists(dataPath))
        {
            // Debug.LogWarning("File not found: " + dataPath);
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                int npcId = 0;
                int skillId = 0;
                string sequence = null;
                string[] fields = line.Split('\t');

                for (int i = 0; i < fields.Length; i++)
                {
                    int separator = fields[i].IndexOf('=');
                    if (separator <= 0)
                        continue;

                    string key = fields[i].Substring(0, separator);
                    string value = fields[i].Substring(separator + 1);
                    switch (key)
                    {
                        case "npc_id":
                            int.TryParse(value, out npcId);
                            break;
                        case "skill_id":
                            int.TryParse(value, out skillId);
                            break;
                        case "seq_name":
                            sequence = DatUtils.CleanupString(value);
                            break;
                    }
                }

                if (npcId > 0 && skillId > 0 && !string.IsNullOrEmpty(sequence))
                    _sequences[Key(npcId, skillId)] = sequence;
            }
        }

        // Debug.Log($"Successfully imported {_sequences.Count} mob skill animation(s)");
    }

    public bool TryGetSequence(int npcId, int skillId, out string sequence)
    {
        return _sequences.TryGetValue(Key(npcId, skillId), out sequence);
    }

    private static long Key(int npcId, int skillId)
    {
        return ((long)(uint)npcId << 32) | (uint)skillId;
    }
}
