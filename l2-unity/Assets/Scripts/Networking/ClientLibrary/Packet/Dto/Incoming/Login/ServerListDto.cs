using System.Collections.Generic;

public sealed class ServerListDto : IWireDto
{
    public byte LastServer;
    public List<ServerData> ServersData = new List<ServerData>();
    public Dictionary<int, int> CharsOnServers = new Dictionary<int, int>();

    public void ReadFrom(PacketReader reader)
    {
        int serverCount = reader.ReadB();
        LastServer = reader.ReadB();
        for (int i = 0; i < serverCount; i++)
        {
            ServerData serverData = new ServerData();
            serverData.serverId = reader.ReadB();
            serverData.ip[0] = reader.ReadB();
            serverData.ip[1] = reader.ReadB();
            serverData.ip[2] = reader.ReadB();
            serverData.ip[3] = reader.ReadB();
            serverData.port = reader.ReadI();
            reader.ReadB();
            reader.ReadB();
            serverData.currentPlayers = reader.ReadSh();
            serverData.maxPlayers = reader.ReadSh();
            serverData.status = reader.ReadB();
            reader.ReadI();
            reader.ReadB();
            ServersData.Add(serverData);
        }

        reader.ReadSh();
        int charsOnServerCount = reader.ReadB();
        if (charsOnServerCount > 7)
            return;
        if (charsOnServerCount <= 0)
            return;

        for (int i = 0; i < charsOnServerCount; i++)
        {
            byte serverId = reader.ReadB();
            byte charCount = reader.ReadB();
            reader.ReadB();
            CharsOnServers[serverId] = charCount;
        }
    }
}
