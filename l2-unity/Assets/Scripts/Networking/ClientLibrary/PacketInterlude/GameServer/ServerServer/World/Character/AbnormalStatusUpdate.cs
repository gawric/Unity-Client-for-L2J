using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class AbnormalStatusUpdate : ServerPacket
{

    public List<EffectHolder> ListEffect { get; set; }
    public AbnormalStatusUpdate(byte[] d) : base(d)
    {
        ListEffect = new List<EffectHolder>();
        Parse();
    }

    public override void Parse()
    {
        int size = ReadSh();
        for(int i=0; i < size; i++)
        {
            
            int id = ReadI();
            int value = ReadSh();
            int duration = ReadI();
            EffectHolder holder = new EffectHolder(id, value, duration);
            ListEffect.Add(holder);
        }
        //Debug.Log("AbnormalStatusUpdate update data");
    }

}

public class EffectHolder
{
    public int _id;
    public int _value;
    //sec
    public int _duration;

    public EffectHolder(int id , int value , int duration)
    {
        _id = id;
        _value = value;
        _duration = duration;
    }

    public int Id
    {
        get { return _id; }
    }

    public int Value
    {
        get { return _value; }
    }

    public int Duration
    {
        get { return _duration; }
    }
}