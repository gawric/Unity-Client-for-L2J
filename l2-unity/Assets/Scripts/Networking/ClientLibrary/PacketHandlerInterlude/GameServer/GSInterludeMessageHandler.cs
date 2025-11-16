using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static SMParam;
public class GSInterludeMessageHandler : ServerPacketHandler
{

    //private ConcurrentDictionary<int, SystemMessage> _delayMessage;
    private ManualResetEvent _ewh = new ManualResetEvent(false);
    private static CancellationTokenSource _cancelTokenSource;
    private static CancellationToken _token;
    public GSInterludeMessageHandler()
    {
        //_delayMessage = new ConcurrentDictionary<int, SystemMessage>();
        _ewh = new ManualResetEvent(false);
        _cancelTokenSource = new CancellationTokenSource();
        _token = _cancelTokenSource.Token;
        StorageVariable.getInstance().SetManualMessage(_ewh);
       // WaitDelayMessage();
    }
    public override void HandlePacket(IData itemQueue)
    {
        ItemServer item = (ItemServer)itemQueue;
        GSInterludeMessagePacketType type = (GSInterludeMessagePacketType)item.ByteType();

        switch (type)
        {
            case GSInterludeMessagePacketType.SystemMessage:
                OnMessage(itemQueue.DecodeData());
                break;
            case GSInterludeMessagePacketType.CreatureSay:
                OnCreatureSay(itemQueue.DecodeData());
                break;
            case GSInterludeMessagePacketType.NpcSay:
                OnNpcSay(itemQueue.DecodeData());
                break;
            default:
                var s = 1;
                break;
        }

    }

    private void OnMessage(byte[] data)
    {
        //SystemMessageInterlude packet = new SystemMessageInterlude(data);
        //Debug.Log("������ ��������� �� ����������� � ����� ������ !!!! " + 1);
        SystemMessagePacket packet = new SystemMessagePacket(data);
        try
        {
            if (InitPacketsLoadWord.getInstance().IsInit)
            {
                InitPacketsLoadWord.getInstance().AddPacketsInit(packet);
            }
            else
            {
                ShowMessage(packet);
            }
        }
        catch (Exception ex)
        {
            Debug.Log("GSInterludeMessageHandler: ����������� ������ � ������� ���������!!!!! " + ex.ToString());
        }
     

    }

    private void OnCreatureSay(byte[] data)
    {
        //SystemMessageInterlude packet = new SystemMessageInterlude(data);
        //Debug.Log("������ ��������� �� ����������� � ����� ������ !!!! " + 1);
        CreatureSay packet = new CreatureSay(data);
        if(packet.Message != null)
        {
            if (InitPacketsLoadWord.getInstance().IsInit)
            {
                InitPacketsLoadWord.getInstance().AddPacketsInit(packet);
            }
            else
            {
                SendShowClient(packet.Message);
            }
        }
       

    }

    private void OnNpcSay(byte[] data)
    {
        //SystemMessageInterlude packet = new SystemMessageInterlude(data);
        //Debug.Log("������ ��������� �� ����������� � ����� ������ !!!! " + 1);
        NpcSay packet = new NpcSay(data);
        if (packet.NpcMessage != null)
        {
            if (InitPacketsLoadWord.getInstance().IsInit)
            {
                InitPacketsLoadWord.getInstance().AddPacketsInit(packet);
            }
            else
            {
                SendShowClient(packet.NpcMessage);
            }
        }


    }


    private void ShowMessage(SystemMessagePacket packet)
    {
        SMParam[] smParams = packet.Params;
        int messageId = packet.Id;
        //Debug.Log("��������� TRY ADD" + messageId);
        SystemMessageDat messageData = SystemMessageTable.Instance.GetSystemMessage(messageId);
        OpenMessageWindow(messageId, messageData , smParams);

        if (messageData != null)
        {
                SystemMessage systemMessage = new SystemMessage(smParams, messageData);

                
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                ChatWindow.Instance.ReceiveSystemMessage(systemMessage);
            });
        }
        else
        {
            EventProcessor.Instance.QueueEvent(() => ChatWindow.Instance.ReceiveSystemMessage(new UnhandledMessage()));
        }
    }
    //279 - You do not have enough Adena
    private void OpenMessageWindow(int messageId , SystemMessageDat messageData , SMParam[] smParams)
    {
        if(messageId == (int)StorageVariable.MessageID.NOT_HAVE_ADENA & messageData != null 
            | messageId == (int)StorageVariable.MessageID.ITEM_MISSING_TO_LEARN_SKILL
            | messageId == (int)StorageVariable.MessageID.NOT_ENOUGH_SP_TO_LEARN_SKILL
            | messageId == (int)StorageVariable.MessageID.NO_ITEM_DEPOSITED_IN_WH)
        {
            EventProcessor.Instance.QueueEvent(() => SystemMessageWindow.Instance.ShowWindow(messageData.Message));
        }
        else if (messageId == (int)StorageVariable.MessageID.LEARNED_SKILL_S1 & messageData != null)
        {
            if(smParams[0].Type == SMParamType.TYPE_SKILL_NAME)
            {
                int[] param = smParams[0].GetIntArrayValue();

                if(smParams[0].GetIntArrayValue() != null)
                {
                    int skillId = param[0];
                    int skilllvl = param[1];

                    
                    SkillNameData sNameData = SkillNameTable.Instance.GetName(skillId, skilllvl);
                    string text = messageData.AddSkillName(sNameData.Name);

                    EventProcessor.Instance.QueueEvent(() => SystemMessageWindow.Instance.ShowWindow(text));
                }
          
            }
       
        }

    }

   

    //public void WaitDelayMessage()
   // {

      //  Task.Run(() =>
      //  {
          //  int exit = 0;
          //  while (true)
          //  {
              //  _ewh.WaitOne();
              //  Debug.Log("DELAAAAAAAAAY MESSSSSSSSSSAAAAAAAAGEEEEEEE ");
              //  if (_token.IsCancellationRequested)
              //  {
              //  }

              //  if(exit == 300)
              //  {
               //     Block();
               // }
               // Thread.Sleep(10);
               // if (_delayMessage.Count > 0)
              //  {
                    //int messageId = StorageVariable.getInstance().GetMessageIdResume();

                    //SystemMessage messageDelay;
                    //SystemMessage messageDelay = _delayMessage[messageId];
                    //if (messageDelay != null)
                    //{
                        //SystemMessageDat newText = SystemMessageTable.Instance.GetSystemMessage(messageDelay.MessageDat.Id);
                       // SystemMessage newMessage = new SystemMessage(messageDelay.Params, newText);
                        //EventProcessor.Instance.QueueEvent(() => ChatWindow.Instance.ReceiveSystemMessage(newMessage));
                        //_delayMessage.Remove(messageId , out messageDelay);
                        //Debug.Log("�������� �� ����� � �������!" + messageId);
                       // exit = 0;
                       // Block();
                   // }
                   // else
                   // {
                    //    Debug.Log("GSInterludeMessageHandler DelayMessage: ���� ��������� ��� ������ �� �����!!! ");
                  //  }
               // }
              //  else
              //  {
                  //  Block();
              //  }

             //   exit++;
          //  }
      //  });
   // }

 
    private void Block()
    {
        _ewh.Reset();
       // _refreshMessageId = -1;
    }

    private void SendShowClient(SystemMessage message)
    {
        if (message != null)
        {
            EventProcessor.Instance.QueueEvent(() =>
            {
                try
                {
                    ChatWindow.Instance.ReceiveSystemMessage(message);
                }
                catch (Exception)
                {
                    Debug.LogError("NpcSay: Critical error now show SystemMessage");
                }
                
            }); 
        }
        else
        {
            EventProcessor.Instance.QueueEvent(() =>
            {
                try
                {
                    EventProcessor.Instance.QueueEvent(() => ChatWindow.Instance.ReceiveSystemMessage(new UnhandledMessage()));
                }
                catch (Exception)
                {
                    Debug.LogError("NpcSay: Critical error now show UnhandledMessage");
                }

            });
          
        }
    }

    
    protected override byte[] DecryptPacket(byte[] data)
    {
        throw new System.NotImplementedException();
    }

    public void Dispose()
    {
        if (!_cancelTokenSource.IsCancellationRequested)
        {
            _ewh.Set();
            _cancelTokenSource.Cancel();

        }
    }
}
