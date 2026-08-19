using UnityEngine;
using System;
using static SMParam;

public class SystemMessageDto : IWireDto
{
    private SMParam[] _params;
    private int _smId;

    public SMParam[] Params { get { return _params; } }
    public int Id { get { return _smId; } }

    

    public void ReadFrom(PacketReader reader)
    {
        try
        {
            _smId = reader.ReadI();

            int paramCount = reader.ReadI();
            _params = new SMParam[paramCount];

            for (int i = 0; i < paramCount; i++)
            {
                int paramType = reader.ReadI();
                SMParam param = new SMParam((SMParamType)paramType);

                switch ((SMParamType)paramType)
                {
                    case SMParamType.TYPE_TEXT:
                    case SMParamType.TYPE_PLAYER_NAME:
                        param.SetValue(reader.ReadOtherS());
                        break;
                    case SMParamType.TYPE_LONG_NUMBER:
                    case SMParamType.TYPE_ITEM_NAME:
                    case SMParamType.TYPE_CASTLE_NAME:
                    case SMParamType.TYPE_INT_NUMBER:
                    case SMParamType.TYPE_NPC_NAME:
                    case SMParamType.TYPE_ELEMENT_NAME:
                    case SMParamType.TYPE_SYSTEM_STRING:
                    case SMParamType.TYPE_INSTANCE_NAME:
                    case SMParamType.TYPE_DOOR_NAME:
                        param.SetValue(reader.ReadI());
                        break;
                    case SMParamType.TYPE_SKILL_NAME:
                        param.SetValue(new int[] { reader.ReadI(), reader.ReadI() });
                        break;
                    case SMParamType.TYPE_ZONE_NAME:
                        param.SetValue(new float[] { reader.ReadF(), reader.ReadF(), reader.ReadF() });
                        break;
                }

                _params[i] = param;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
