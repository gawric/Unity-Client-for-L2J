using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassIdTemplate
{
    private int _pId;
    private bool _mage;
    private InterludeRace _race;
    private ClassIdTemplate _pParent;
    private int sex;
    public ClassIdTemplate(int pId , bool mage , InterludeRace race , ClassIdTemplate pParent)
    {
        _pId = pId;
        _mage = mage;
        _race = race;
        _pParent = pParent;
    }


    public int GetClassId()
    {
        return _pId;
    }
}
