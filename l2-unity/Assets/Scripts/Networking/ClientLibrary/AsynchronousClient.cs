using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class AsynchronousClient {
    private Socket _socket;
    private string _ipAddress;
    private int _port;
    private bool _connected;
    private ClientPacketHandler _clientPacketHandler;
    private ServerPacketHandler _serverPacketHandler;

    
    private DefaultClient _client;
    private bool _cryptEnabled = false;
    private bool _initPacket = true;
    private bool _initPacketEnabled;
    private LoginClientReceiving _loginReceiving;
    private GameClientReceiving _gameReceiving;

    public bool InitPacket { get { return _initPacket; } set { _initPacket = value; } }
    public bool IsConnected { get { return _connected; } set { _connected = value; } }
    public bool CryptEnabled {
        get { return _cryptEnabled; }
        set {
            Debug.Log("Crypt" + (value ? " enabled." : " disabled."));
            _cryptEnabled = value;
        }
    }


    public AsynchronousClient(string ip, int port, DefaultClient client, ClientPacketHandler clientPacketHandler,
        ServerPacketHandler serverPacketHandler, bool enableInitPacket) {
        Debug.Log(ip);
        Debug.Log(port);
        _ipAddress = "";
        _port = port;
        _clientPacketHandler = clientPacketHandler;
        _serverPacketHandler = serverPacketHandler;
        _clientPacketHandler.SetClient(this);
        _serverPacketHandler.SetClient(this, _clientPacketHandler);
        SetQueue(IsLoginClient(client));
        _client = client;
        _initPacketEnabled = enableInitPacket;
        _initPacket = enableInitPacket;
        SetReceiving(IsLoginClient(client));
    }

    private void SetReceiving(bool IsLoginClient)
    {
        if(IsLoginClient){
            _loginReceiving = new LoginClientReceiving(this);
        }
        else
        {
            _gameReceiving = new GameClientReceiving(this);
        }
    }

    private void StartReceiving(bool IsLoginClient)
    {
        if (IsLoginClient)
        {
            _loginReceiving.StartReceiving(_socket);
        }
        else
        {
            _gameReceiving.StartReceiving(_socket);
        }
    }
    private void SetQueue(bool IsLoginClient)
    {
        if (IsLoginClient)
        {
            SendLoginDataQueue.Instance().SetPacketHandler(_clientPacketHandler);
            IncomingLoginDataQueue.Instance().SetPacketHandler(_serverPacketHandler);
        }
        else
        {
            SendGameDataQueue.Instance().SetPacketHandler(_clientPacketHandler);
            IncomingGameDataQueue.Instance().SetPacketHandler(_serverPacketHandler);
            IncomingGameCombatQueue.Instance().SetPacketHandler(new GsInterludeCombatHandler());
            IncomingGameMessageQueue.Instance().SetPacketHandler(new GSInterludeMessageHandler());
        }
    }

    public bool Connect() {
        IPAddress ipAddress;
        try
        {
            string ipAddress2 = SettingServerIp.IpAddressServer;
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(ipAddress2);
            //ipAddress = ipHostInfo.AddressList[0];
            AddressFamily ipv4Family = GetAddressFamily(ipAddress2);
            ipAddress = IPAddress.Parse(ipAddress2);
            _socket = new Socket(ipv4Family, SocketType.Stream, ProtocolType.Tcp);

            if (_initPacketEnabled)
            {
                _initPacket = true;
            }
        }
        catch (Exception)
        {
            ipAddress = IPAddress.Parse("127.0.0.1");
            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }


        Debug.Log("Connecting..." + " IP " + ipAddress.ToString() + " port " + _port);
          
        IAsyncResult result = _socket.BeginConnect(ipAddress, _port, null, null);

        bool success = result.AsyncWaitHandle.WaitOne(5000, true);

        if (_socket.Connected) {
            Debug.Log("Connection success.");
            _socket.EndConnect( result );
            _connected = true;
            StartReceiving(IsLoginClient(_client));
            return true;
        } else {
            Debug.Log("Connection failed.");
            EventProcessor.Instance.QueueEvent(() => _client.OnConnectionFailed());
            _socket.Close();
            DisposeQueue();
            _connected = false;
            return false;
        }
    }

    static AddressFamily GetAddressFamily(string ipAddress)
    {
        if (IPAddress.TryParse(ipAddress, out IPAddress ip))
        {
            return ip.AddressFamily;
        }
        else
        {
            throw new ArgumentException("Некорректный IP-адрес.", nameof(ipAddress));
        }
    }

    private bool IsLoginClient(DefaultClient client)
    {
        return client.GetType() == typeof(LoginClient);
    }
    public void Disconnect() {
        if(!_connected) {
            return;
        }

        Debug.Log("Disconnect");

        try {
            _serverPacketHandler.CancelTokens();
            //IncomingGameMessageQueue.Instance().GetPacketHandler().CancelTokens(); 
            _connected = false;         
            _socket.Close();
            _socket.Dispose();
            DisposeQueue();
        } catch (Exception e) {
            Debug.LogError(e);
        }


        EventProcessor.Instance.QueueEvent(() => _client.OnDisconnect());
    }

    private void DisposeQueue()
    {
        if(IsLoginClient(_client)){
            IncomingLoginDataQueue.Instance().Dispose();
            SendLoginDataQueue.Instance().Dispose();
        }
        else
        {
            SendGameDataQueue.Instance().Dispose();
            IncomingGameDataQueue.Instance().Dispose();
            IncomingGameMessageQueue.Instance().Dispose();
            IncomingGameCombatQueue.Instance().Dispose();
            this.IsConnected = false;
        }

    }
    public void SendPacket(ClientPacket packet)
    {
        try
        {
            byte[] data = packet.GetData();
            int totalSize = data.Length + 2;
            if (totalSize > ushort.MaxValue) throw new InvalidOperationException("Packet too large");


            ushort sizeHeader = (ushort)totalSize;
            byte[] header = BitConverter.GetBytes(sizeHeader);

            // Предполагаем, что stream хранится и переиспользуется в поле класса, чтобы не создавать каждый раз
            using (NetworkStream stream = new NetworkStream(_socket, ownsSocket: false))
            {
                stream.Write(header, 0, 2);
                stream.Write(data, 0, data.Length);
            }

            //Debug.Log("Sent final: [" + string.Join(",", data) + "]");
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }




}
