using UnityEngine;

[System.Serializable]
public class Status {


     private double _hp;
     private double _mp;
     private int _myDamage;
     private double _remainingHp = 1;

    public double GetHp()
    {
         return _hp;
    }

    public void SetHp(double hp)
    {
       _hp = hp;
    }

    public double GetMp()
    {
        return _mp;
    }

    public void SetMp(double mp)
    {
        _mp = mp;
    }

    public void SetDamage(int damage)
    {
        _myDamage = damage;
        _remainingHp = _hp - _myDamage;
    }


    public double GetRemainingHp()
    {
        return _remainingHp;
    }

}