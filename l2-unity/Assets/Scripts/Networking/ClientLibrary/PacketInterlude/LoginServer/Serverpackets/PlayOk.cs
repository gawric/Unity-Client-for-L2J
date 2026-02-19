using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayOk : ServerPacket
{
    private int _playOk1;
    private int _playOk2;
    public int PlayOk1 { get { return _playOk1; } }
    public int PlayOk2 { get { return _playOk2; } }
    public PlayOk(byte[] d) : base(d)
    {
        Parse();
    }


    public override void Parse()
    {
        _playOk1 = ReadI();
        _playOk2 = ReadI();
        //Debug.Log("PlayOk Parce Success Key1 " + _playOk1);
        //Debug.Log("PlayOk Parce Success Key1 " + _playOk2);
    }
}
