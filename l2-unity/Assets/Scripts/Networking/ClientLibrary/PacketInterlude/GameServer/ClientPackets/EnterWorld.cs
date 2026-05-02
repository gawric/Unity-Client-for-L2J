using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterWorld : ClientPacket
{

    //readBytes(32); // Unknown Byte Array
    /// <summary>
    /// /readInt(); // Unknown Value
    /// </summary>
    //readInt(); // Unknown Value
    //readInt(); // Unknown Value
    //readInt(); // Unknown Value
    //readBytes(32); // Unknown Byte Array
    //readInt(); // Unknown Value
		//for (int i = 0; i< 5; i++)
		//{
		//	for (int o = 0; o< 4; o++)
		//	{
			//	_tracert[i][o] = readUnsignedByte();
//}
		//}
    public EnterWorld() : base((byte)GameInterludeClientPacketType.EnterWorld)
    {
        WriteB(new byte[32]);
        WriteI(1);
        WriteI(1);
        WriteI(1);
        WriteI(1);
        WriteB(new byte[32]);
        WriteI(1);
        for(int i = 0; i < 5; i++)
        {
            for (int o = 0; o < 4; o++)
            {
                WriteB((byte)1);
            }
        }

        BuildPacket();
    }
}
