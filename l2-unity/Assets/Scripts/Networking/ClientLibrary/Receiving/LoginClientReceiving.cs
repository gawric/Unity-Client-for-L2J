using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class LoginClientReceiving
{
    private readonly AsynchronousClient _asyncClient;
    private const int HeaderSize = 2;

    public LoginClientReceiving(AsynchronousClient asyncClient)
    {
        _asyncClient = asyncClient;
    }

    public Task StartReceiving(Socket socket, System.Threading.CancellationToken token)
    {
        Debug.Log("Start receiving LoginClient");
        return Task.Run(() => Receiving(socket, token), token);
    }

    private void Receiving(Socket socket, System.Threading.CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested && _asyncClient.IsConnected)
            {
                var stream = _asyncClient._stream;
                if (stream != null)
                {
                    int lo = stream.ReadByte();
                    int hi = stream.ReadByte();

                    if (lo < 0 || hi < 0)
                        throw new EndOfStreamException("Server closed connection.");

                    int totalLen = (hi << 8) | lo;
                    if (totalLen <= HeaderSize)
                        throw new EndOfStreamException($"Receiving Exception: totalLen={totalLen}");

                    int dataLen = totalLen - HeaderSize;

                    byte[] data = new byte[dataLen];
                    GameClientReceiving.ReadWholeArray(stream, data);

                    IncomingLoginDataQueue.Instance().AddItem(data, _asyncClient.InitPacket, _asyncClient.CryptEnabled);
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (IOException)
        {
        }
        catch (SocketException)
        {
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}