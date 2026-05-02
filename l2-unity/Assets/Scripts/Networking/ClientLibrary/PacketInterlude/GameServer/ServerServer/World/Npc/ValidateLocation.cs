using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;


public class ValidateLocation : ServerPacket
{
    private int objectId = 0;
    private Vector3 location;
    private int heading;

    public int ObjectId {  get { return objectId; } }
    public Vector3 Position { get { return location; } }
    public int Heading { get { return heading; } }

    public ValidateLocation(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        objectId = ReadI();
        int x = ReadI();
        int y = ReadI();
        int z = ReadI();
        location = VectorUtils.ConvertPosToUnity(new Vector3(x, y, z));
        heading = ReadI();

    }

       public override bool Equals(object obj)
    {
        if (obj is ValidateLocation other)
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
