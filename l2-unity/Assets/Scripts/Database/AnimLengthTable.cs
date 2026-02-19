using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimLeghtTable
{
    private static AnimLeghtTable _instance;

    public static AnimLeghtTable Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnimLeghtTable();
            }

            return _instance;
        }
    }

    private Dictionary<string, float> _leght;

    public void Initialize()
    {
        if(_leght != null) _leght.Clear();
        Read();

    }

    public float GetLeghtMs(string key)
    {
        if (_leght.ContainsKey(key))
        {
            return _leght[key] * 1000;
        }
        return 0f;
    }
    public float GetLeghtSec(string key)
    {
        if (_leght.ContainsKey(key))
        {
            return _leght[key];
        }
        return 0f;
    }

    public float GetAllLeghtMs(string[] keys)
    {
        float allLeght = 0f;

        foreach (string name in keys)
        {
            float sec = GetLeghtMs(name);
            allLeght = allLeght + sec;
        }
        return allLeght;
    }

    private void Read()
    {
        _leght = new Dictionary<string, float>();
        _leght.Add("CastMid" , 2.250f);
        _leght.Add("MagicShot" , 1.875f);
        _leght.Add("CastEnd" , 0.125f);
    }
   

}
