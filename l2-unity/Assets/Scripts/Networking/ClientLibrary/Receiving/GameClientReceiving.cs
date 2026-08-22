using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class GameClientReceiving
{
    private readonly AsynchronousClient _asyncClient;
    private const int HeaderSize = 2;

    private readonly IncomingGameQueue _incoming;

    public GameClientReceiving(AsynchronousClient asyncClient, IncomingGameQueue incoming)
    {
        _asyncClient = asyncClient;
        _incoming = incoming;
    }

    public Task StartReceiving(Socket socket, System.Threading.CancellationToken token)
    {
        Debug.Log("Start receiving GameClient");
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
                    int dataLen = ReadPacketLength(stream);
                    if (dataLen <= 0)
                        throw new EndOfStreamException("Invalid packet length.");

                    byte[] data = new byte[dataLen];
                    ReadWholeArray(stream, data);

                    if (!_asyncClient.IsConnected)
                        break;

                    _incoming.AddItem(data, _asyncClient.InitPacket, _asyncClient.CryptEnabled);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            LobbyFlowLog.Warn("Game RX stopped — socket disposed");
        }
        catch (IOException ex)
        {
            LobbyFlowLog.Warn("Game RX IOException: " + ex.Message);
        }
        catch (SocketException ex)
        {
            LobbyFlowLog.Warn("Game RX SocketException: " + ex.Message);
        }
        catch (Exception e)
        {
            LobbyFlowLog.Exception("Game RX", e);
        }
    }

    private static int ReadPacketLength(Stream stream)
    {
        byte[] header = new byte[HeaderSize];
        ReadWholeArray(stream, header);

        int totalLen = header[0] | (header[1] << 8);

        if (totalLen <= HeaderSize)
            throw new EndOfStreamException($"ReadPacketLength Exception: totalLen={totalLen}");

        return totalLen - HeaderSize;
    }

    public static void ReadWholeArray(Stream stream, byte[] data)
    {
        int offset = 0;
        int remaining = data.Length;

        while (remaining > 0)
        {
            int read = stream.Read(data, offset, remaining);

            if (read <= 0)
                throw new EndOfStreamException($"ReadWholeArray: End of stream with {remaining} bytes left");
            remaining -= read;
            offset += read;
        }
    }
}
