using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class NpcDecoEffectsTable
{
    static NpcDecoEffectsTable _instance;

    public static NpcDecoEffectsTable Instance
    {
        get
        {
            if (_instance == null)
                _instance = new NpcDecoEffectsTable();
            return _instance;
        }
    }

    Dictionary<int, NpcDecoEffect> _byNpcId = new Dictionary<int, NpcDecoEffect>();
    bool _loaded;

    public void Initialize()
    {
        if (_loaded)
            return;

        _byNpcId = new Dictionary<int, NpcDecoEffect>();
        string dataPath = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Npc_deco_effects.csv");
        if (!File.Exists(dataPath))
        {
            Debug.LogWarning("File not found: " + dataPath);
            _loaded = true;
            return;
        }

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string header = reader.ReadLine();
            if (string.IsNullOrEmpty(header))
                return;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                List<string> cols = ParseCsvLine(line);
                if (cols.Count < 4)
                    continue;

                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int npcId))
                    continue;

                NpcDecoEffect deco = new NpcDecoEffect
                {
                    NpcId = npcId,
                    ClassName = cols.Count > 1 ? cols[1] : string.Empty,
                    MeshName = cols.Count > 2 ? cols[2] : string.Empty,
                    DecoEffect = cols.Count > 3 ? cols[3] : string.Empty,
                    Scale = 1f
                };
                if (cols.Count > 4 &&
                    float.TryParse(cols[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float scale))
                {
                    deco.Scale = scale;
                }

                _byNpcId[npcId] = deco;
            }
        }

        _loaded = true;
        Debug.Log("Successfully imported " + _byNpcId.Count + " npc deco effect(s)");
    }

    public bool TryGet(int npcId, out NpcDecoEffect deco)
    {
        return _byNpcId.TryGetValue(npcId, out deco);
    }

    static List<string> ParseCsvLine(string line)
    {
        List<string> cols = new List<string>(5);
        StringBuilder current = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                cols.Add(current.ToString());
                current.Length = 0;
                continue;
            }

            current.Append(c);
        }

        cols.Add(current.ToString());
        return cols;
    }
}
