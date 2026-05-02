using L2_login;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSendServer : IData
{
    private ClientPacket packet;
    private bool encrypt;
    public ItemSendServer(ClientPacket packet, bool encrypt, bool blowfish)
    {
        this.packet = packet;
        this.encrypt = encrypt;
        //Encrypt(packet, encrypt);
        Blowfish(packet.GetData(), blowfish);
    }
    public byte[] DecodeData()
    {
        throw new System.NotImplementedException();
    }

    //new version encrypt
    private void Blowfish(byte[] data, bool blowfish)
    {
        if (blowfish)
        {
            //Debug.Log($"GameServer Data no encrypt : {StringUtils.ByteArrayToString(data)}");
            GameClient.Instance.GameCrypt.GameServerEncrypt(data , 0 , data.Length);

        }

    }

    public ClientPacket GetPacket()
    {
        return packet;
    }

    public byte[] DecodeExData()
    {
        throw new System.NotImplementedException();
    }
}