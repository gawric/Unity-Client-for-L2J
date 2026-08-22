using System;
using UnityEngine;
using static SMParam;

public sealed class MessagePacketApply
{
    private readonly ChatWindow _chat;
    private readonly SystemMessageWindow _systemMessageUi;
    private readonly PacketApplyQueue _queue;
    private readonly SystemMessageTable _systemMessages;
    private readonly SkillNameTable _skillNames;

    public MessagePacketApply(
        ChatWindow chat,
        SystemMessageWindow systemMessageUi,
        PacketApplyQueue queue,
        SystemMessageTable systemMessages,
        SkillNameTable skillNames)
    {
        _chat = chat;
        _systemMessageUi = systemMessageUi;
        _queue = queue;
        _systemMessages = systemMessages;
        _skillNames = skillNames;
    }

    public void SystemMessage(SystemMessageDto packet)
    {
        SMParam[] smParams = packet.Params;
        int messageId = packet.Id;
        SystemMessageDat messageData = _systemMessages.GetSystemMessage(messageId);
        OpenMessageWindow(messageId, messageData, smParams);

        if (messageData != null)
            ReceiveSystem(new SystemMessage(smParams, messageData));
        else
            ReceiveSystem(new UnhandledMessage());
    }

    public void CreatureSay(CreatureMessage message)
    {
        _queue.Queue(() =>
        {
            try
            {
                _chat.ReceiveChatMessage(message);
            }
            catch (Exception)
            {
                ReceiveSystem(message);
            }
        });
    }

    public void NpcSay(SystemMessage message)
    {
        ReceiveSystem(message);
    }

    public void ReceiveSystem(SystemMessage message)
    {
        _queue.Queue(() =>
        {
            try
            {
                _chat.ReceiveSystemMessage(message ?? new UnhandledMessage());
            }
            catch (Exception)
            {
                Debug.LogError("NpcSay: failed to show message");
            }
        });
    }

    private void OpenMessageWindow(int messageId, SystemMessageDat messageData, SMParam[] smParams)
    {
        if (_systemMessageUi == null)
            return;

        if (messageId == (int)MessageID.NOT_HAVE_ADENA && messageData != null
            || messageId == (int)MessageID.ITEM_MISSING_TO_LEARN_SKILL
            || messageId == (int)MessageID.NOT_ENOUGH_SP_TO_LEARN_SKILL
            || messageId == (int)MessageID.NO_ITEM_DEPOSITED_IN_WH)
        {
            _queue.Queue(() => _systemMessageUi.ShowWindow(messageData.Message));
        }
        else if (messageId == (int)MessageID.LEARNED_SKILL_S1 && messageData != null
                 && smParams[0].Type == SMParamType.TYPE_SKILL_NAME
                 && smParams[0].GetIntArrayValue() != null)
        {
            int[] param = smParams[0].GetIntArrayValue();
            SkillNameData sNameData = _skillNames.GetName(param[0], param[1]);
            string text = messageData.AddSkillName(sNameData.Name);
            _queue.Queue(() => _systemMessageUi.ShowWindow(text));
        }
    }
}
