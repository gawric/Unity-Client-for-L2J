using L2_login;
using System.Threading;
using UnityEngine;

public abstract class ServerPacketHandler
{
    protected AsynchronousClient _client;
    protected long _timestamp;
    protected CancellationTokenSource _tokenSource;
    protected EventProcessor _eventProcessor;
    protected ClientPacketHandler _clientPacketHandler;

    public void SetClient(AsynchronousClient client, ClientPacketHandler clientPacketHandler) {
        _client = client;
        _tokenSource = new CancellationTokenSource();
        _eventProcessor = EventProcessor.Instance;
        _clientPacketHandler = clientPacketHandler;
    }


    public void CancelTokens() {
        if(_tokenSource != null) _tokenSource.Cancel();
    }

    public abstract void HandlePacket(IData data);

    protected abstract byte[] DecryptPacket(byte[] data);

    public bool DecodeXOR(byte[] packet) {
        if(NewCrypt.decXORPass(packet)) {
            Debug.Log("CLEAR: " + StringUtils.ByteArrayToString(packet));
            return true;
        }

        return false;
    }


    public bool IsExPacket(ItemServer item)
    {
        return GameInterludeServerPacketType.ExTypePacket == item.PaketType();
    }
    //public byte ParceExPacket(byte[] exPacket)
    //{
    //    return new byte(1);
    //}
}
