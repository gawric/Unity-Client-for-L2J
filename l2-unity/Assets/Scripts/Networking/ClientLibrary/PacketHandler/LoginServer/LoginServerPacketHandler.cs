using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class LoginServerPacketHandler : ServerPacketHandler
{
    public override void HandlePacket(IData itemQueue) {
        ItemLogin item = (ItemLogin)itemQueue;
        switch (item.PaketType()) {
            case LoginInterludeServerPacketType.Init:
                OnInitReceive(item.DecodeData());
                break;

            case LoginInterludeServerPacketType.GGAuth:
                OnGGAuth(item.DecodeData());
                break;
            case LoginInterludeServerPacketType.LoginFail:
                LoginFail(item.DecodeData());
                break;
            case LoginInterludeServerPacketType.LoginOk:
                LoginOk(item.DecodeData());
                break;
            case LoginInterludeServerPacketType.ServerList:
                ServerList(item.DecodeData());
                break;
            case LoginInterludeServerPacketType.PlayOk:
                PlayOk(item.DecodeData());
                break;

        }
    }

    protected override byte[] DecryptPacket(byte[] data) {
        if (LoginClient.Instance.LogCryptography) {
            Debug.Log("<---- [LOGIN] ENCRYPTED: " + StringUtils.ByteArrayToString(data));
        }

        LoginClient.Instance.DecryptBlowFish.processBigBlock(data, 0, data, 0, data.Length);

        if (LoginClient.Instance.LogCryptography) {
            Debug.Log("<---- [LOGIN] DECRYPTED: " + StringUtils.ByteArrayToString(data));
        }
        Debug.Log(data);
        return data;
    }



    private void OnInitReceive(byte[] data) {
        InitPacket packet = new InitPacket(data);
        byte[] rsaKey = packet.PublicKey;
        byte[] blowfishKey = packet.BlowfishKey;
        LoginClient.Instance.SetRSAKey(rsaKey);
        LoginClient.Instance.SetBlowFishKey(blowfishKey);
        LoginClient.Instance.SetSessionId(packet.SessionId);
        _client.InitPacket = false;
        SendLoginDataQueue.Instance().AddItem(CreatorPackets.CreateGGPacket(packet) , true , true);
    }

    private void OnGGAuth(byte[] data)
    {
        GGAuth packet = new GGAuth(data);
        //Debug.Log("GGAuth session id server " + packet.SessionId + " | use session id client " + LoginClient.Instance.GetGessionId());
       // Debug.Log("Send AuthPacket account " + LoginClient.Instance.Account + "passwd  " + LoginClient.Instance.Password);
        SendLoginDataQueue.Instance().AddItem(CreatorPackets.CreateAuthPacket(LoginClient.Instance.Account , LoginClient.Instance.Password , packet.Response), true, true);
    }


    private void LoginFail(byte[] data)
    {
        LoginFail packet = new LoginFail(data);
        int reasonid = packet.RessionId;
        LoginWindow.Instance.ShowErrorTextOtherThread(packet.Message);
        EventProcessor.Instance.QueueEvent(() => LoginClient.Instance.Disconnect());
    }

    private void LoginOk(byte[] data)
    {
        LoginOk packet = new LoginOk(data);

        //Debug.Log("Seesion key 1 " + packet.SessionKey1);
        //Debug.Log("Seesion key 1 " + packet.SessionKey2);

        LoginClient.Instance.SessionKey1 = packet.SessionKey1;
        LoginClient.Instance.SessionKey2 = packet.SessionKey2;

        GameClient.Instance.SessionKey1 = packet.SessionKey1;
        GameClient.Instance.SessionKey2 = packet.SessionKey2;

        EventProcessor.Instance.QueueEvent(() => LoginClient.Instance.OnAuthAllowed());
    }

    private void ServerList(byte[] data)
    {
        ServerList packet = new ServerList(data);

        EventProcessor.Instance.QueueEvent(
            () => LoginClient.Instance.OnServerListReceived(packet.LastServer, packet.ServersData, packet.CharsOnServers));

    }

    private void PlayOk(byte[] data)
    {
        PlayOk packet = new PlayOk(data);

        GameClient.Instance.PlayKey1 = packet.PlayOk1;
        GameClient.Instance.PlayKey2 = packet.PlayOk2;

            GameManager.Instance.OnLoginServerPlayOk();

            if (GameManager.Instance.GameState != GameState.READY_TO_CONNECT)
                return;

           if (GameManager.Instance.IsSwitchingServer)
               return;

           GameManager.Instance.IsSwitchingServer = true;

            try
            {
                if (_client != null)
                    _client.Disconnect();

                GameClient.Instance.Connect();
            }
            finally
            {
                GameManager.Instance.IsSwitchingServer = false;
            }
    }

}
