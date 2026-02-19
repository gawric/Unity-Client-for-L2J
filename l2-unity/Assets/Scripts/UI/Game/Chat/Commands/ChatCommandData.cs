public sealed class ChatCommandData
{
    public int Id { get; }
    public ChatCommand Type { get; }
    public string Prefix { get; }
    public string Name { get; }

    public ChatCommandData(int id, ChatCommand type, string prefix, string name)
    {
        Id = id;
        Type = type;
        Prefix = prefix;
        Name = name;
    }
}