using System;
using System.Collections.Generic;

public class Animation
{
    public string Value { get; }

    public Animation(string value)
    {
        Value = value;
    }

    public string Concat(string name)
    {
        return Value + name;
    }

    public override string ToString()
    {
        return Value;
    }
}

public static class AnimationNames
{
    public static readonly Animation ATK_WAIT = new Animation("atkwait_");
    public static readonly Animation WAIT = new Animation("wait_");
    public static readonly Animation WALK = new Animation("walk_");
    public static readonly Animation RUN = new Animation("run_");
    public static readonly Animation DEAD = new Animation("death");
    public static readonly Animation REBIRTH = new Animation("rebirth");
    public static readonly Animation ATK01 = new Animation("jatk01_");
    public static readonly Animation ATK02 = new Animation("jatk02_");
    public static readonly Animation ATK03 = new Animation("jatk03_");

    public static readonly Animation MONSTER_WAIT = new Animation("wait");
    public static readonly Animation MONSTER_WALK = new Animation("walk");
    public static readonly Animation MONSTER_RUN = new Animation("run");
    public static readonly Animation MONSTER_ATK01 = new Animation("atk01");
}

