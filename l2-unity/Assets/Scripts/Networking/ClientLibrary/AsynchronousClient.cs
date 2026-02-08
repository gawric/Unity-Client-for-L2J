using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AsynchronousClient
{
    private System.Threading.CancellationTokenSource _cts;

    private int _state;
    private int _port;

    private Socket _socket;
    public NetworkStream _stream;
    private DefaultClient _client;

    private ClientPacketHandler _clientPacketHandler;
    private ServerPacketHandler _serverPacketHandler;
    private LoginClientReceiving _loginReceiving;
    private GameClientReceiving _gameReceiving;

    private bool _cryptEnabled = false;
    private bool _initPacket = true;
    private bool _initPacketEnabled;

    public bool InitPacket { get => _initPacket; set => _initPacket = value; }
    public bool IsConnected => Volatile.Read(ref _state) == 2;

    public bool CryptEnabled
    {
        get => _cryptEnabled;
        set
        {
            Debug.Log("Crypt" + (value ? " enabled." : " disabled."));
            _cryptEnabled = value;
        }
    }

    public AsynchronousClient(string ip, int port, DefaultClient client, ClientPacketHandler clientPacketHandler, ServerPacketHandler serverPacketHandler, bool enableInitPacket)
    {
        _port = port;
        _client = client;

        _clientPacketHandler = clientPacketHandler;
        _serverPacketHandler = serverPacketHandler;

        _clientPacketHandler.SetClient(this);
        _serverPacketHandler.SetClient(this, _clientPacketHandler);

        _initPacketEnabled = enableInitPacket;
        _initPacket = enableInitPacket;

        var isLogin = IsLoginClient(client);
        SetQueue(isLogin);
        SetReceiving(isLogin);
    }

    public async Task<bool> Connect()
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            return false;

        _cts = new CancellationTokenSource();
        var isLogin = IsLoginClient(_client);

        try
        {
            string ipStr = SettingServerIp.IpAddressServer;
            var ip = IPAddress.Parse(ipStr);
            _socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            if (_initPacketEnabled)
                _initPacket = true;

            await _socket.ConnectAsync(ip, _port);

            _stream = new NetworkStream(_socket, ownsSocket: false);

            Volatile.Write(ref _state, 2);

            Debug.Log("Connection success for: " + (isLogin ? " LoginClient." : " GameClient."));

            if (isLogin)
                _loginReceiving.StartReceiving(_socket, _cts.Token);
            else
                _gameReceiving.StartReceiving(_socket, _cts.Token);

            return true;
        }
        catch
        {
            Debug.LogWarning("Connection failed for: " + (isLogin ? " LoginClient." : " GameClient."));
            Disconnect();
            EventProcessor.Instance.QueueEvent(() => _client.OnConnectionFailed());
            return false;
        }
    }

    public void Disconnect()
    {
        int prev = Interlocked.Exchange(ref _state, 3);
        if (prev == 0)
        {
            Interlocked.Exchange(ref _state, 0);
            return;
        }

        CloseSocket();

        DisposeQueue();

        EventProcessor.Instance.QueueEvent(() => _client.OnDisconnect());

        Interlocked.Exchange(ref _state, 0);

        var isLogin = IsLoginClient(_client);
        Debug.LogWarning((isLogin ? " LoginClient:" : " GameClient:") + " Disconnected.");
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
        }
    }

    public void SendPacket(ClientPacket packet)
    {
        try
        {
            if (_socket == null || !_socket.Connected || _stream == null)
                return;

            byte[] data = packet.GetData();
            int totalSize = data.Length + 2;
            if (totalSize > ushort.MaxValue)
                throw new InvalidOperationException("Packet too large");

            ushort sizeHeader = (ushort)totalSize;

            byte[] header = BitConverter.GetBytes(sizeHeader);

            _stream.Write(header, 0, 2);
            _stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private void SetReceiving(bool isLoginClient)
    {
        if (isLoginClient)
            _loginReceiving = new LoginClientReceiving(this);
        else
            _gameReceiving = new GameClientReceiving(this);
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

    private void CloseSocket()
    {
        try { _serverPacketHandler.CancelTokens(); } catch { }
        try { _cts?.Cancel(); } catch { }
        try { _socket?.Shutdown(SocketShutdown.Both); } catch { }
        try { _stream?.Dispose(); } catch { }
        try { _socket?.Close(); } catch { }
        try { _socket?.Dispose(); } catch { }
        _stream = null;
        _socket = null;
    }

    private bool IsLoginClient(DefaultClient client)
    {
        return client.GetType() == typeof(LoginClient);
    }
}
