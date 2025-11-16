using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;

public class MoveBackwardToLocation : ClientPacket
{
    //l2j
   // _targetX = readInt();
    //_targetY = readInt();
    //_targetZ = readInt();
    //_originX = readInt();
    //_originY = readInt();
    //_originZ = readInt();
    public MoveBackwardToLocation(Vector3 position , Vector3 target) : base((byte)GameInterludeClientPacketType.MoveToLocation)
    {
        //Debug.Log("POSITION DIST SEND SERVER " + VectorUtils.Distance2D(position , target));
        //Debug.Log("POSITION DIST SEND SERVER l2j" + Distance2D(position.x , position.z, target.x , target.z));
        Vector3 l2jpos = VectorUtils.ConvertPosUnityToL2j(position);
        Vector3 l2jtar = VectorUtils.ConvertPosUnityToL2j(target);
        int x = (int)Math.Round(l2jpos.x);
        int y = (int)Math.Round(l2jpos.y);
        int z = (int)Math.Round(l2jpos.z);

        int tarX = (int)Math.Round(l2jtar.x);
        int tarY = (int)Math.Round(l2jtar.y);
        int tarZ = (int)Math.Round(l2jtar.z);


        //Debug.Log("POSITION DIST SEND SERVER l2jcovert" + Distance2D(x, y, tarX, tarY));

         WriteI(tarX);
         WriteI(tarY);
         WriteI(tarZ);
         WriteI(x);
         WriteI(y);
         WriteI(z);
         WriteI(1);// is 0 if cursor keys are used 1 if mouse is used

         BuildPacket();
    }

    public double Distance2D(float tx, float ty, float ox, float oy)
    {
        double dx = (double)tx - ox;
        double dy = (double)ty - oy;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
