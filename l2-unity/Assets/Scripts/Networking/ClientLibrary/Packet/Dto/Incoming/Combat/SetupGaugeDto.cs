using UnityEngine;

public class SetupGaugeDto : IWireDto
{
    private int _color = 0;
    private int _time1 = 0; //currentTime
    private int _time2 = 0; //maxTime

    public int Color { get => _color; }

    public int CurrentTime { get => _time1; }

    public int MaxTime { get => _time2; }
    

    public void ReadFrom(PacketReader reader)
    {

        _color = reader.ReadI(); // color 0-blue 1-red 2-cyan 3-green
        _time1 = reader.ReadI(); 
        _time2 = reader.ReadI(); 

    }
}
