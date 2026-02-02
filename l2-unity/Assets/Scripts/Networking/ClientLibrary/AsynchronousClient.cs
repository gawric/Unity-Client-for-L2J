using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class AsynchronousClient
{
    private Socket _socket;
    private int _port;
    private volatile bool _connected;

    private ClientPacketHandler _clientPacketHandler;
    private ServerPacketHandler _serverPacketHandler;

    private DefaultClient _client;
    private bool _cryptEnabled = false;
    private bool _initPacket = true;
    private bool _initPacketEnabled;

    private LoginClientReceiving _loginReceiving;
    private GameClientReceiving _gameReceiving;

    public bool InitPacket { get => _initPacket; set => _initPacket = value; }
    public bool IsConnected { get => _connected; set => _connected = value; }

    public bool CryptEnabled
    {
        get => _cryptEnabled;
        set
        {
            Debug.Log("Crypt" + (value ? " enabled." : " disabled."));
            _cryptEnabled = value;
        }
    }

    public AsynchronousClient(
        string ip,
        int port,
        DefaultClient client,
        ClientPacketHandler clientPacketHandler,
        ServerPacketHandler serverPacketHandler,
        bool enableInitPacket)
    {
        _port = port;
        _client = client;

        _clientPacketHandler = clientPacketHandler;
        _serverPacketHandler = serverPacketHandler;

        _clientPacketHandler.SetClient(this);
        _serverPacketHandler.SetClient(this, _clientPacketHandler);

        _initPacketEnabled = enableInitPacket;
        _initPacket = enableInitPacket;

        bool isLogin = IsLoginClient(client);
        SetQueue(isLogin);
        SetReceiving(isLogin);
    }

    private void SetReceiving(bool isLoginClient)
    {
        if (isLoginClient)
            _loginReceiving = new LoginClientReceiving(this);
        else
            _gameReceiving = new GameClientReceiving(this);
    }

    private void StartReceiving(bool isLoginClient)
    {
        if (isLoginClient)
            _loginReceiving.StartReceiving(_socket);
        else
            _gameReceiving.StartReceiving(_socket);
    }

    private void SetQueue(bool isLoginClient)
    {
        if (isLoginClient)
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

    public bool Connect()
    {
        IPAddress ipAddress;
        try
        {
            string ipStr = SettingServerIp.IpAddressServer;
            ipAddress = IPAddress.Parse(ipStr);

            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (_initPacketEnabled)
                _initPacket = true;
        }
        catch
        {
            ipAddress = IPAddress.Parse("127.0.0.1");
            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        Debug.Log($"Connecting... IP {ipAddress} port {_port}");

        try
        {
            var task = _socket.ConnectAsync(ipAddress, _port);
            if (!task.Wait(5000))
            {
                Debug.Log("Connection timeout.");
                CloseSocket();
                DisposeQueue();
                _connected = false;
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Connect exception: {e}");
            CloseSocket();
            DisposeQueue();
            _connected = false;
            return false;
        }

        if (_socket.Connected)
        {
            Debug.Log("Connection success.");
            _connected = true;
            StartReceiving(IsLoginClient(_client));
            return true;
        }

        Debug.LogWarning("Connection failed.");
        EventProcessor.Instance.QueueEvent(() => _client.OnConnectionFailed());
        CloseSocket();
        DisposeQueue();
        _connected = false;
        return false;
    }

    private bool IsLoginClient(DefaultClient client)
    {
        return client.GetType() == typeof(LoginClient);
    }

    public void Disconnect()
    {
        if (!_connected)
            return;

        Debug.LogWarning("Disconnect");
        _connected = false;

        try
        {
            _serverPacketHandler.CancelTokens();

            CloseSocket();

            DisposeQueue();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        EventProcessor.Instance.QueueEvent(() => _client.OnDisconnect());
    }

    private void CloseSocket()
    {
        try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
        try { _socket?.Close(); } catch { }
        try { _socket?.Dispose(); } catch { }
        _socket = null;
    }

    private void DisposeQueue()
    {
        if (IsLoginClient(_client))
        {
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
            if (_socket == null || !_socket.Connected)
                return;

            byte[] data = packet.GetData();
            int totalSize = data.Length + 2;
            if (totalSize > ushort.MaxValue)
                throw new InvalidOperationException("Packet too large");

            ushort sizeHeader = (ushort)totalSize;

            byte[] header = BitConverter.GetBytes(sizeHeader);

            using (NetworkStream stream = new NetworkStream(_socket, ownsSocket: false))
            {
                stream.Write(header, 0, 2);
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
}
