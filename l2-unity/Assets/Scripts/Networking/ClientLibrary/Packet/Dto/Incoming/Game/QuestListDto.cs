using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestListDto : IWireDto
{

    private List<QuestInstance> _allQuest;


    public List<QuestInstance> Quest { get => _allQuest; }

    public QuestListDto()
    {
        _allQuest = new List<QuestInstance>();
    }

    public void ReadFrom(PacketReader reader)
    {
        int size = reader.ReadSh();

        for(int i = 0; i < size; i++)
        {
            int questId = reader.ReadI();
            int flags = reader.ReadI();
            _allQuest.Add(new QuestInstance(questId, flags));
            //_allQuest.Add(new QuestInstance(2, 1));
            //_allQuest.Add(new QuestInstance(3, flags));
            //_allQuest.Add(new QuestInstance(6, flags, 1));
        }
    }
}
