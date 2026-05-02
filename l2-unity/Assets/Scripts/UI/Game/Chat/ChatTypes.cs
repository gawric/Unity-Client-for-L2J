public static class ChatTypes
{
    private const int MAX_ID = 18;

    private static readonly ChatTypeData[] _byId;

    static ChatTypes()
    {
        _byId = new ChatTypeData[MAX_ID + 1];

        Register(ChatType.GENERAL, 0, "General", "#dcd9dc");
        Register(ChatType.SHOUT, 1, "Shout", "#ffb266");
        Register(ChatType.WHISPER, 2, "Whisper", "#ff6bdc");
        Register(ChatType.PARTY, 3, "Party", "#4cff4c");
        Register(ChatType.CLAN, 4, "Clan", "#66ccff");
        Register(ChatType.GM, 5, "GM", "#ffff66");
        Register(ChatType.PETITION_PLAYER, 6, "Petition (Player)", "#b3b3ff");
        Register(ChatType.PETITION_GM, 7, "Petition (GM)", "#ff9933");
        Register(ChatType.TRADE, 8, "Trade", "#ffd700");
        Register(ChatType.ALLIANCE, 9, "Alliance", "#66ffff");
        Register(ChatType.ANNOUNCEMENT, 10, "Announcement", "#00ffff");
        Register(ChatType.BOAT, 11, "Boat", "#80ccff");
        Register(ChatType.FRIEND, 12, "Friend", "#99ff99");
        Register(ChatType.MSNCHAT, 13, "MSN Chat", "#4de6ff");
        Register(ChatType.PARTYMATCH_ROOM, 14, "Party Match", "#ccccff");
        Register(ChatType.PARTYROOM_COMMANDER, 15, "Party Commander", "#ff9999");
        Register(ChatType.PARTYROOM_ALL, 16, "Party Room", "#9999ff");
        Register(ChatType.HERO_VOICE, 17, "Hero Voice", "#ffd700");
        Register(ChatType.CRITICAL_ANNOUNCE, 18, "Critical", "#ff0000");
    }

    private static void Register(ChatType type, int id, string text, string colorHex)
    {
        _byId[id] = new ChatTypeData(id, id, text, colorHex);
    }

    public static ChatTypeData GetById(int id)
    {
        if ((uint)id <= MAX_ID)
        {
            var data = _byId[id];
            return data != null && data.Id == id ? data : null;
        }

        return null;
    }
}