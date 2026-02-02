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

    public void StartReceiving(Socket socket)
    {
        Debug.Log("Start receiving LoginClient");
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
                    int lo = stream.ReadByte();
                    int hi = stream.ReadByte();

                    if (lo < 0 || hi < 0)
                        throw new EndOfStreamException("Server closed connection.");

                    int totalLen = (hi << 8) | lo;
                    if (totalLen <= HeaderSize)
                        throw new EndOfStreamException($"Receiving Exception: totalLen={totalLen}");

                    int payloadLen = totalLen - HeaderSize;

                    byte[] payload = new byte[payloadLen];
                    GameClientReceiving.ReadWholeArray(stream, payload);

                    IncomingLoginDataQueue.Instance().AddItem(payload, _asyncClient.InitPacket, _asyncClient.CryptEnabled);
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
}
