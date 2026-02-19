using System.Collections.Generic;

public class ServerList : ServerPacket
{
    private List<ServerListPacket.ServerData> _serverData;
    private byte _lastServer;
    public  Dictionary<int, int> CharsOnServers { get { return _charsOnServers; } }
    public List<ServerListPacket.ServerData> ServersData { get { return _serverData; } }
    public byte LastServer { get { return _lastServer; } }
    private Dictionary<int, int> _charsOnServers;

    public ServerList(byte[] d) : base(d)
    {
        _serverData = new List<ServerListPacket.ServerData>();
        _charsOnServers = new Dictionary<int, int>();
        Parse();
    }
    public override void Parse()
    {
        int _serverCount = ReadB();
        _lastServer = ReadB();
        ParceChildren(_serverCount);
    }

    private void ParceChildren(int _serverCount)
    {
        //Debug.Log("1");
        for (int i = 0; i < _serverCount; i++)
        {
            ServerListPacket.ServerData serverData = new ServerListPacket.ServerData();
            serverData.serverId = ReadB();
            serverData.ip[0] = ReadB();
            serverData.ip[1] = ReadB();
            serverData.ip[2] = ReadB();
            serverData.ip[3] = ReadB();

            serverData.port = ReadI();
            int ageLimit = ReadB();
            int pvp = ReadB();
            serverData.currentPlayers = ReadSh();
            serverData.maxPlayers = ReadSh();
            serverData.status = ReadB(); //0 - down // 1 - ok
            int serverType = ReadI();// 1: Normal, 2: Relax, 4: Public Test, 8: No Label, 16: Character Creation Restricted, 32: Event, 64: Free.
            int brackets = ReadB();

            _serverData.Add(serverData);
        }

        int unknow = ReadSh();
        //all chars to accounts
        int charsOnServerCount = ReadB();
        if (charsOnServerCount > 7) return;
        if (charsOnServerCount > 0)
        {
            for (int i = 0; i < charsOnServerCount; i++)
            {


                byte serverId = ReadB();
                byte charCount = ReadB();
                byte charToDelete = ReadB();


                _charsOnServers[serverId] = charCount;
            }
        }
    }

    //public class ServerData
   // {
      //  public byte[] ip;
       // public int port;
       // public int currentPlayers;
       // public int maxPlayers;
       // public int status;
       // public int serverId;

       // public ServerData()
       // {
        //    ip = new byte[4];
        //}
    //}
}
