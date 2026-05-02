using System.Runtime.CompilerServices;
using UnityEngine;

public class OtherModel
{
    public object _otherData;
    public int _id;
    public string _count;
    public int _price;
    public OtherModel(object otherData)
    {
        _otherData = otherData;
    }

    public OtherModel(int id , string count , int price)
    {
        _id = id;
        _count = count;
        _price = price;
    }

    public int GetId()
    {
        return _id;
    }
    public object GetOtherModel()
    {
        return _otherData;
    }


}
