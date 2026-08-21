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

        // Interlude ServerList ends here. Later logins append chars-on-server.
        // Decrypted login packets still contain padding + checksum — do not treat that as payload.
        if (reader.Remaining <= 8)
            return;

        if (!reader.HasRemaining(3))
            return;

        reader.ReadSh();
        if (!reader.HasRemaining(1))
            return;

        int charsOnServerCount = reader.ReadB();
        if (charsOnServerCount > 7)
            return;
        if (charsOnServerCount <= 0)
            return;

        for (int i = 0; i < charsOnServerCount; i++)
        {
            if (!reader.HasRemaining(2))
                break;

            byte serverId = reader.ReadB();
            byte charCount = reader.ReadB();
            if (reader.HasRemaining(1))
                reader.ReadB();
            CharsOnServers[serverId] = charCount;
        }
    }
}
