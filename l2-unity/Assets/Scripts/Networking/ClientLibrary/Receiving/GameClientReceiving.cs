using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

public class GameClientReceiving
{
    private readonly AsynchronousClient _asyncClient;
    private const int HeaderSize = 2;

    public GameClientReceiving(AsynchronousClient asyncClient)
    {
        _asyncClient = asyncClient;
    }

    public void StartReceiving(Socket socket)
    {
        Debug.Log("Start receiving GameClient");
        Task.Run(() => Receiving(socket));
    }

    private void Receiving(Socket socket)
    {
        using (NetworkStream stream = new NetworkStream(socket, ownsSocket: false))
        {
            try
            {
                while (_asyncClient.IsConnected)
                {
                    int payloadLen = ReadPacketLength(stream);
                    if (payloadLen <= 0)
                        throw new EndOfStreamException("Invalid packet length.");

                    byte[] payload = new byte[payloadLen];
                    ReadWholeArray(stream, payload);

                    if (!_asyncClient.IsConnected)
                        break;

                    ItemServer item = IncomingGameDataQueue.Instance().CreateItem(payload, _asyncClient.InitPacket, _asyncClient.CryptEnabled);

                    IncomingGameCombatQueue.Instance().AddItem(item);
                    IncomingGameDataQueue.Instance().AddItem(item);
                    IncomingGameMessageQueue.Instance().AddItem(item);
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
