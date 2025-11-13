using UnityEngine;

public class SkillInstance : AbstractSkill
{
    public SkillInstance(int pId, int pLevel, bool pPassive, bool pDisabled)
    {
        SkillID = pId;
        Level = pLevel;
        IsPassive = pPassive;
        IsDisabled = pDisabled;
    }

    public void RefreshData(int pId, int pLevel, bool pPassive, bool pDisabled)
    {
        SkillID = pId;
        Level = pLevel;
        IsPassive = pPassive;
        IsDisabled = pDisabled;
    }

    public string GetTypeName()
    {
        return IsPassive ? "Passive Skill" : "Active Skill";
    }

    public int GetHp()
    {
        return SkillgrpTable.Instance.GetSkill(SkillID, Level).HpConsume;
    }

    public int GetMp()
    {
        return SkillgrpTable.Instance.GetSkill(SkillID, Level).MpConsume;
    }

    public int GetRange()
    {
        return SkillgrpTable.Instance.GetSkill(SkillID, Level).CastRange;
    }
    public double GetReuseTime()
    {
        return SkillgrpTable.Instance.GetSkill(SkillID, Level).ReuseDelay;
    }

    public double GetHitTime()
    {
        return SkillgrpTable.Instance.GetSkill(SkillID, Level).HitTime;
    }
    public bool IsMagic()
    {
        var skill = SkillgrpTable.Instance.GetSkill(SkillID, Level);
        if (skill == null)
            return false;
        return skill.IsMagic == 1;
    }

    public string Icon()
    {
        return SkillgrpTable.Instance.GetSkill(SkillID, Level)?.Icon ?? "";
    }
    public string GetName()
    {
        return SkillNameTable.Instance.GetName(SkillID, Level)?.Name ?? "";
    }

    public string GetDescription()
    {
        return SkillNameTable.Instance.GetName(SkillID, Level)?.Desc ?? "";
    }


}
