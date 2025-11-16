using System;
using UnityEngine;
using static SMParam;

public class SystemMessage
{
    private readonly SMParam[] _params;
    private readonly SystemMessageDat _messageData;

    public SystemMessage(SMParam[] smParams, SystemMessageDat messageData)
    {
        _params = smParams;
        _messageData = messageData;
    }

    public SystemMessageDat MessageDat => _messageData;
    public SMParam[] Params => _params;

    public override string ToString()
    {
        string value = String.Copy(_messageData.Message);
        ProcessMessageParameters(ref value);
        return $"<color=#{_messageData.Color}>{value}</color>";
    }

    private void ProcessMessageParameters(ref string value)
    {
        for (int i = 1; i <= _params.Length; i++)
        {
            SMParam param = _params[i - 1];
            string replacement = GetParameterReplacement(param, i);

            if (replacement != null)
            {
                value = ReplacePlaceholders(value, replacement, i);
            }
        }
    }

    private string GetParameterReplacement(SMParam param, int index)
    {
        switch (param.Type)
        {
            case SMParamType.TYPE_TEXT:
            case SMParamType.TYPE_PLAYER_NAME:
                return param.GetStringValue();

            case SMParamType.TYPE_LONG_NUMBER:
            case SMParamType.TYPE_INT_NUMBER:
            case SMParamType.TYPE_CASTLE_NAME:
            case SMParamType.TYPE_NPC_NAME:
            case SMParamType.TYPE_ELEMENT_NAME:
            case SMParamType.TYPE_SYSTEM_STRING:
            case SMParamType.TYPE_INSTANCE_NAME:
            case SMParamType.TYPE_DOOR_NAME:
                return param.GetIntValue().ToString();

            case SMParamType.TYPE_ITEM_NAME:
                return GetItemName(param.GetIntValue());

            case SMParamType.TYPE_SKILL_NAME:
                return GetSkillName(param.GetIntArrayValue());

            default:
                return null;
        }
    }

    private string GetItemName(int itemId)
    {
        try
        {
            return ItemNameTable.Instance.GetItemName(itemId).Name;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get item name for ID {itemId}: {e.Message}");
            return "Unknown Item";
        }
    }

    private string GetSkillName(int[] skillData)
    {
        try
        {
            if (skillData == null || skillData.Length < 2)
            {
                Debug.LogError("Invalid skill data array");
                return "Unknown Skill";
            }

            var skill = SkillNameTable.Instance.GetName(skillData[0], skillData[1]);

            if(skill != null)
            {
                return skill.Name;
            }

            return "Unknown Skill Skill ID " + skillData[0] + " Level " + skillData[1];
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get skill name: {e.Message}");
            return "Unknown Skill";
        }
    }

    private string ReplacePlaceholders(string value, string replacement, int index)
    {
        value = value.Replace($"$s{index}", replacement);

        // Only replace $c placeholders for text and player name types
        if (_params[index - 1].Type == SMParamType.TYPE_TEXT ||
            _params[index - 1].Type == SMParamType.TYPE_PLAYER_NAME)
        {
            value = value.Replace($"$c{index}", replacement);
        }

        return value;
    }
}
