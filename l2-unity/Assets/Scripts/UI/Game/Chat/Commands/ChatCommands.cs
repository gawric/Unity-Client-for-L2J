public static class ChatCommands
{
    private const int MAX_ID = 9;
    private static readonly ChatCommandData[] _byId;

    static ChatCommands()
    {
        _byId = new ChatCommandData[MAX_ID + 1];

        Register(ChatCommand.SHOUT,    1, "!", "Shout");
        Register(ChatCommand.WHISPER,  2, "\"", "Whisper");
        Register(ChatCommand.PARTY,    3, "#", "Party");
        Register(ChatCommand.CLAN,     4, "@", "Clan");
        Register(ChatCommand.TRADE,    8, "+", "Trade");
        Register(ChatCommand.ALLIANCE, 9, "$", "Alliance");
    }

    private static void Register(ChatCommand type, int id, string prefix, string name)
    {
        _byId[id] = new ChatCommandData(id, type, prefix, name);
    }

    public static ChatCommandData GetById(int id)
    {
        if ((uint)id <= MAX_ID)
        {
            var data = _byId[id];
            return data != null && data.Id == id ? data : null;
        }

        return null;
    }

    public static ChatCommandData FindByText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        char first = text[0];

        for (int i = 1; i <= MAX_ID; i++)
        {
            var cmd = _byId[i];
            if (cmd != null && cmd.Prefix[0] == first)
                return cmd;
        }

        return null;
    }
}
