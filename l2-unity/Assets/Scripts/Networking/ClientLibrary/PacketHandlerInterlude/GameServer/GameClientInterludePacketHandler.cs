using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class GameClientInterludePacketHandler : ClientPacketHandler
{
    public override void SendPacket(ClientPacket packet)
    {
        _client.SendPacket(packet);
    }

    public void SendRequestSetTarget()
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� ");
    }

    public void SendRequestSetTarget(int id)
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� ");
       // RequestAutoAttackPacket packet = new RequestAutoAttackPacket();
        //SendPacket(packet);
    }

    

   public void SendRequestSelectCharacter(int id)
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� ");
       // RequestCharSelectPacket packet = new RequestCharSelectPacket(slot);
       // SendPacket(packet);
    }


    public void UpdateMoveDirection(Vector3 v3)
    {
       //Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� ");
        //RequestMoveDirectionPacket packet = new RequestMoveDirectionPacket(direction);
        //SendPacket(packet);
    }

    public void InflictAttack(int id, AttackType type)
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� InflictAttack");
    }

    public void SendRequestAutoAttack()
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� SendRequestAutoAttack");
    }
    

    public void EndLoadWorld()
    {
        InitPacketsLoadWord.getInstance().IsInit = false;
        InitPacketsLoadWord.getInstance().UseInitPackets();
        
        var sendPaket = CreatorPacketsUser.CreatSkillCoolTime();
        bool enable = GameClient.Instance.IsCryptEnabled();
        Debug.Log("GameClientInterlude EndLoadWorld �������� �������");
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
    }

    public void UpdatePosition(Vector3 position)
    {
        //RequestMovePacket packet = new RequestMovePacket(position);
        //SendPacket(packet);
        //Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� UpdatePosition");
    }

    public void UpdateRotation(float y)
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� UpdateRotation");
    }
    public void UpdateAnimation(byte id, float value)
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� UpdateAnimation");
    }

    public void SendMessage(string text)
    {
        Debug.Log("GameClientInterludePacketHandler: �� ����������� ����� �������� ��� ��� SendMessage");
    }
 
}
