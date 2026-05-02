using UnityEngine;

public class SetupGauge : ServerPacket
{
    private int _color = 0;
    private int _time1 = 0; //currentTime
    private int _time2 = 0; //maxTime

    public int Color { get => _color; }

    public int CurrentTime { get => _time1; }

    public int MaxTime { get => _time2; }
    public SetupGauge(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {

        _color = ReadI(); // color 0-blue 1-red 2-cyan 3-green
        _time1 = ReadI(); 
        _time2 = ReadI(); 

    }
}
