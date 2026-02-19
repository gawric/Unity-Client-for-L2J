using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestList : ServerPacket
{

    private List<QuestInstance> _allQuest;


    public List<QuestInstance> Quest { get => _allQuest; }

    public QuestList(byte[] d) : base(d)
    {
        _allQuest = new List<QuestInstance>();
        Parse();
    }

    public override void Parse()
    {
        int size = ReadSh();

        for(int i = 0; i < size; i++)
        {
            int questId = ReadI();
            int flags = ReadI();
            _allQuest.Add(new QuestInstance(questId, flags));
            //_allQuest.Add(new QuestInstance(2, 1));
            //_allQuest.Add(new QuestInstance(3, flags));
            //_allQuest.Add(new QuestInstance(6, flags, 1));
        }
    }
}
