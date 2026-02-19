using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharMoveToLocation : ServerPacket
{

    //l2j
    // buffer.writeInt(_objectId);
	//	buffer.writeInt(_xDst);
	//	buffer.writeInt(_yDst);
	//	buffer.writeInt(_zDst);
	//	buffer.writeInt(_x);
	//	buffer.writeInt(_y);
	//	buffer.writeInt(_z);
    public Vector3 NewPosition { get; private set; }
    public Vector3 OldPosition { get; private set; }
    public int ObjId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public CharMoveToLocation(byte[] d) : base(d)
    {
        CreatedAt = DateTime.Now;
        Parse();
    }

    public override void Parse()
    {
        ObjId = ReadI();
        

        int xDst = ReadI();
        int yDst = ReadI();
        int zDst = ReadI();
        NewPosition = VectorUtils.ConvertPosToUnity(new Vector3(xDst , yDst , zDst));

        int x = ReadI();
        int y = ReadI();
        int z = ReadI();
        OldPosition = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
        
    }

  
}
