using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;


public class ValidateLocationDto : IWireDto
{
    private int objectId = 0;
    private Vector3 location;
    private int heading;

    public int ObjectId {  get { return objectId; } }
    public Vector3 Position { get { return location; } }
    public int Heading { get { return heading; } }

    

    public void ReadFrom(PacketReader reader)
    {
        objectId = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        location = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
        heading = reader.ReadI();

    }

       public override bool Equals(object obj)
    {
        if (obj is ValidateLocationDto other)
        {
            return this.objectId == other.objectId &&
                   this.location == other.location &&
                   this.heading == other.heading;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(objectId, location, heading);
    }
}
