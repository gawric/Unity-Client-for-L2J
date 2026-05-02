using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QuestNameTable
{
    private static QuestNameTable _instance;
    private static Dictionary<int, List<QuestName>> _questNames;

    private const int indexQuestId = 1;
    private const int indexQuestProg = 2;
    private const int indexMainName = 3;
    private const int indexProgname = 4;
    private const int indexDescription = 5;
    private const int indexDescriptionToolTips = 53;
    private const int indexQuestType = 6;

    private const int indexLvlMin = 39;
    private const int indexLvlMax = 40;
    private const int indexRequirements = 50;

    private const int indexEntity_name = 42;
    //maybe not true
    private const int indexRepaet= 41;
    private const int indexAreaId = 131;
    public static QuestNameTable Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new QuestNameTable();
                _questNames = new Dictionary<int, List<QuestName>>();
            }

            return _instance;
        }
    }


    public QuestName GetQuestName(int questId, int questProg)
    {
        if (_questNames.ContainsKey(questId))
        {
            foreach (var quest in _questNames[questId])
            {
                if (quest.QuestProg == questProg)
                {
                    return quest;
                }
            }
        }
        return null;
    }

  

    public List<QuestName> GetQuestsWithLastSubtask()
    {
        List<QuestName> quests = new List<QuestName>();

        foreach (var entry in _questNames)
        {
            QuestName lastQuest = entry.Value[entry.Value.Count - 1];
            quests.Add(lastQuest);
        }

        return quests;
    }


    public void Initialize()
    {
        try
        {
            string dataPathE = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Questname-e_interlude.txt");
            //string dataPathR = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Questname-r_interlude.txt");
            ReadQuestNameInterlude(dataPathE);
            //ReadArmorInterlude(dataPathR);
            //DebugPrint();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize QuestNameTable: {ex.Message}");
        }
    }

    public void ReadQuestNameInterlude(string dataPath)
    {

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            int index = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (index != 0)
                {
                    string[] ids = line.Split('\t');
                    int id = Int32.Parse(ids[indexQuestId]);
                    QuestName quest =  CreateModel(ids , id);

                    if (!_questNames.ContainsKey(id))
                    {
                        var  list = new List<QuestName>{quest};
                        _questNames.Add(id , list);
                    }
                    else
                    {
                        _questNames[id].Add(quest);
                    }
                }
                index++;
            }

        }

    }

    private QuestName CreateModel(string[] ids , int quest_id)
    {
        QuestName quest = new QuestName();

        int questProg = int.Parse(ids[indexQuestProg]);
        string mainName = DatUtils.CleanupStringOldData(ids[indexMainName]);
        string progName = DatUtils.CleanupStringOldData(ids[indexProgname]);
        string description = DatUtils.CleanupStringOldData(ids[indexDescription]);
        string descriptionToolTips = DatUtils.CleanupStringOldData(ids[indexDescriptionToolTips]);
        int questType = int.Parse(ids[indexQuestType]);
        int lvlMin = int.Parse(ids[indexLvlMin]);
        int lvlMax = int.Parse(ids[indexLvlMax]);
        string requirements = DatUtils.CleanupStringOldData(ids[indexRequirements]);
        string entity_name = DatUtils.CleanupStringOldData(ids[indexEntity_name]);
        int repeat = int.Parse(ids[indexRepaet]);
        int areaId = int.Parse(ids[indexAreaId]);

        quest.Quest_id = quest_id;
        quest.QuestProg = questProg;
        quest.Main_name = mainName;
        quest.ProgName = progName;
        quest.Description = DatUtils.CleanupStringNewN(description);
        quest.DescriptionToolTip = descriptionToolTips;
        quest.QuestType = questType;
        quest.LvlMin = lvlMin;
        quest.LvlMax = lvlMax;
        quest.Requirements = requirements;
        quest.EntityName = entity_name;
        quest.Repeat = repeat;
        quest.AreaId = areaId;

        return quest;
    }

    private void DebugPrint()
    {
        foreach (KeyValuePair<int, List<QuestName>> entry in _questNames)
        {
            foreach(QuestName name in entry.Value)
            {
                //EntityName берем последнего из списка он и будет Клиентом в форме или источником
                Debug.Log(" ID Номер quest " + entry.Key + " value " + name.Main_name + " entityName " + name.EntityName + " questType " + name.Repeat);
            }
            
        }
    }
}
