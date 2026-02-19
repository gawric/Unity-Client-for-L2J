using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillServer : AbstractSkill
{
    private int _id;
    private int _level;
    private bool _passive;
    private bool _disabled;

    public SkillServer(int pId, int pLevel, bool pPassive, bool pDisabled)
    {
        _id = pId;
        _level = pLevel;
        _passive = pPassive;
        _disabled = pDisabled;
    }

    public int Id { get { return _id; } }
    public int Level { get { return _level; } }
    public bool Passive { get { return _passive; } }
    public bool Disable { get { return _disabled; } }
}
