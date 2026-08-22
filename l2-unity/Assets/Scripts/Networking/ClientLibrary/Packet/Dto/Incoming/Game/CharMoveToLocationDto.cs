using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CharMoveToLocationDto : IWireDto
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
    public CharMoveToLocationDto()
    {
        CreatedAt = DateTime.Now;
    }

    public void ReadFrom(PacketReader reader)
    {
        ObjId = reader.ReadI();
        

        int xDst = reader.ReadI();
        int yDst = reader.ReadI();
        int zDst = reader.ReadI();
        NewPosition = VectorUtils.ConvertPosToUnity(new Vector3(xDst , yDst , zDst));

        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        OldPosition = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
        
    }

  
}
