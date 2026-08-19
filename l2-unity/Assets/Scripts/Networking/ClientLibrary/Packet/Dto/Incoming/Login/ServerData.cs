public class ServerData
{
    public byte[] ip;
    public int port;
    public int currentPlayers;
    public int maxPlayers;
    public int status;
    public int serverId;

    public ServerData()
    {
        ip = new byte[4];
    }
}
