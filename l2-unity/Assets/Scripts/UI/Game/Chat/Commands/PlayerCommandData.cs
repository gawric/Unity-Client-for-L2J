public sealed class PlayerCommandData
{
    public int Id { get; }
    public PlayerCommand Type { get; }
    public string Command { get; }

    public PlayerCommandData(int id, PlayerCommand type, string command)
    {
        Id = id;
        Type = type;
        Command = command;
    }
}