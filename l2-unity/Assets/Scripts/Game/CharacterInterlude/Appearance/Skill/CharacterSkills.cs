using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class CharacterSkills
{
    private Dictionary<int, SkillInstance> _skills;
    private List<SkillInstance>_skillsList;

    public CharacterSkills()
    {
        _skills = new Dictionary<int, SkillInstance>();
    }

    public void AddSkillsList(List<SkillInstance> skills)
    {
        ClearDict();
        _skillsList = skills;
        foreach (SkillInstance skill in skills)
        {
            _skills.Add(skill.SkillID, skill);  
        }
    }

    private void ClearDict()
    {
        if(_skills.Count > 0)
        {
            _skills.Clear();
        }
    }


    public SkillInstance GetSkill(int id)
    {
       // lock (_sync)
        ////{
            return (_skills.ContainsKey(id)) ? _skills[id] : null;
        //}
    }

    public List<SkillInstance> GetSkills()
    {
        return _skillsList;
    }
}
