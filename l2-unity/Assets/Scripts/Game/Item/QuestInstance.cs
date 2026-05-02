using UnityEngine;

public class QuestInstance
{
    private int _questID;
    private int _flags;
    private int _repeat;

    public int QuestID => _questID;
    public int Status => _flags;

    public QuestInstance(int qId, int flags)
    {
        _questID = qId;
        _flags = flags;
        _repeat = -1;
    }

    public QuestInstance(int qId, int flags , int repeat)
    {
        _questID = qId;
        _flags = flags;
        _repeat = repeat;
    }

    public bool IsComplete()
    {
        return _flags == 0;

    }

    private QuestName GetQuestName() => QuestNameTable.Instance.GetQuestName(_questID, _flags);

    //public bool IsRepeat() => GetQuestName()?.Repeat == 1;
    public bool IsRepeat()
    {
        if (_repeat != -1) return _repeat == 1;
        return GetQuestName()?.Repeat == 1;
    }

    public string QuestName() => GetQuestName()?.Main_name ?? "";

    public string LevelRange() => GetQuestName()?.GetLevelRange() ?? "";

    public string QuestProgName() => GetQuestName()?.ProgName ?? "";

    public string GetQuestSource() => GetQuestName()?.GetSource() ?? "";

    public string GetQuestEntity() => GetQuestName()?.EntityName ?? "";

    public string GetDescription() => GetQuestName()?.Description ?? "";
}