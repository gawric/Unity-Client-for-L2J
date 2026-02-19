public class QuestName
{
    private int _quest_id;
    private int _quest_prog;
    private string _main_name;
    private string _prog_name;
    private string _description;
    private int _lvl_min;
    private int _lvl_max;
    private int _quest_type;
    private string _entity_name;
    private string _description_tooltip;
    private int _areaId;
    //3 not repeat
    //2  repaet
    //0 - unk
    private int _repeat;
    private string _get_item_in_quest;
    private int _contact_npc_id;
    private int _contact_npc_x;
    private int _contact_npc_y;
    private int _contact_npc_z;
    private string _requirements;

    private string[] _titlesToRemove = new string[]
    {
        "Tetrarch",
        "Grand Master",
        "Master",
        "Hermit",
        "Magister",
        "Hierarch",
        "Sentinel",
        "Blacksmith",
        "Abyssal",
        "Abandoned",
        "Captain",
        "Priest",
        "Celebrant",
        "Warehouse",
        "High Priest",
        "Sentry",
        "Trader"
    };

    public int Quest_id { get => _quest_id; set => _quest_id = value; }

    public int QuestProg { get => _quest_prog; set => _quest_prog = value; }

    public int Repeat { get => _repeat; set => _repeat = value; }
    public int AreaId { get => _areaId; set => _areaId = value; }

    public string ProgName { get => _prog_name; set => _prog_name = value; }

    public string EntityName { get => _entity_name; set => _entity_name = value; }
    public string DescriptionToolTip { get => _description_tooltip; set => _description_tooltip = value; }
    public string Main_name { get => _main_name; set => _main_name = value; }

    public string Requirements { get => _requirements; set => _requirements = value; }
    public string Description { get => _description; set => _description = value; }
    public int LvlMin { get => _lvl_min; set => _lvl_min = value; }
    public int LvlMax { get => _lvl_max; set => _lvl_max = value; }

    public int QuestType { get => _quest_type; set => _quest_type = value; }
    public string itemInQuest { get => _get_item_in_quest; set => _get_item_in_quest = value; }

    public int ContactNpcId { get => _contact_npc_id; set => _contact_npc_id = value; }

    public int ContactNpcX { get => _contact_npc_x; set => _contact_npc_x = value; }
    public int ContactNpcY { get => _contact_npc_y; set => _contact_npc_y = value; }
    public int ContactNpcZ { get => _contact_npc_z; set => _contact_npc_z = value; }

    private string source = "";

    public string GetSource()
    {
        if(!string.IsNullOrEmpty(source)) return source;

        if (string.IsNullOrEmpty(_entity_name))
            return string.Empty;

        string result = _entity_name;

        foreach (var title in _titlesToRemove)
        {
            result = result.Replace(title, "").Trim();
        }

        source = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ").Trim();

        return source;
    }

    public string GetLevelRange()
    {
        if (_lvl_max == 0)
        {
            return $"{_lvl_min}";
        }
        return $"{_lvl_min} ~ {_lvl_max}";
    }

    public string GetRepetable()
    {
        //3 not repeat
        //2  repaet
        //0 - unk

        if (_repeat == 3)
        {
            return "1";
        }else if (_repeat < 3)
        {
            return "repeat";
        }

        return "1";
    }

}