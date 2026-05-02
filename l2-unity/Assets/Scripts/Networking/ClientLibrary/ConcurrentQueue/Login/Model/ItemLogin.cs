using L2_login;
using UnityEngine;

public class ItemLogin : IData
{
    private byte[] _data;
    private LoginInterludeServerPacketType packetType;
    public ItemLogin(byte[] data, bool init, bool cryptEnbled)
    {
        Decrypt(data, cryptEnbled, init);
        packetType = (LoginInterludeServerPacketType)data[0];
        this._data = data;
    }

    public byte[] DecodeData() { return _data; }
    public LoginInterludeServerPacketType PaketType() { return packetType; }

    private void Decrypt(byte[] data , bool cryptEnbled , bool init)
    {
        if (cryptEnbled)
        {
            data = DecryptPacket(data);
            if(data.Length == 16)
            {
                Debug.Log("Verify" + StringUtils.ByteArrayToString(data));
            }
            if (init)
            {
                if (!DecodeXOR(data))
                {
                    Debug.LogError("Packet XOR could not be decoded.");
                    return;
                }
            }
            else if (!NewCrypt.verifyChecksum(data))
            {
                Debug.Log("No verify data " + StringUtils.ByteArrayToString(data));
                Debug.LogError("Packet checksum is wrong. Ignoring packet... lenhth " + data.Length);
                return;
            }
        }

    }

    private byte[] DecryptPacket(byte[] data)
    {
        if (LoginClient.Instance.LogCryptography)
        {
            Debug.Log("<---- [LOGIN] ENCRYPTED: " + StringUtils.ByteArrayToString(data));
        }

        LoginClient.Instance.DecryptBlowFish.processBigBlock(data, 0, data, 0, data.Length);

        if (LoginClient.Instance.LogCryptography)
        {
            Debug.Log("<---- [LOGIN] DECRYPTED: " + StringUtils.ByteArrayToString(data));
        }
        return data;
    }

    private bool DecodeXOR(byte[] packet)
    {
        if (NewCrypt.decXORPass(packet))
        {
            Debug.Log("CLEAR: " + StringUtils.ByteArrayToString(packet));
            return true;
        }

        return false;
    }

    public ClientPacket GetPacket()
    {
        throw new System.NotImplementedException();
    }

    public byte[] DecodeExData()
    {
        throw new System.NotImplementedException();
    }
}
