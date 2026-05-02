using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FilterData
{
    private Dictionary<int, DataCell> _dictElement;

    public FilterData(Dictionary<int, DataCell> dictElement)
    {
        this._dictElement = dictElement;
    }


    public bool IsContain(int skillId)
    {
        var data = _dictElement.FirstOrDefault(kvp => kvp.Value.GetSkillId() == skillId).Value;

        if (data != null) return true;

        return false;
    }

    public DataCell GetEmptyCell()
    {
        return _dictElement
            .FirstOrDefault(kvp => !kvp.Value.IsBusy()).Value;
    }

    public DataCell GetCellByPosition(int position)
    {
        return _dictElement
            .FirstOrDefault(kvp => kvp.Value.GetPosition() == position)
            .Value;
    }


    public List<DataCell> GetListActiveAndBusy()
    {
        return _dictElement
                .Where(kvp => !kvp.Value.IsPassiveSkill() & kvp.Value.IsBusy())
                .Select(kvp => kvp.Value)
                .ToList();
    }

    public bool HasElements()
    {
        var element = _dictElement
                        .FirstOrDefault(kvp => kvp.Value.IsBusy()).Value;
        if (element != null) return true;
        return false;
    }

    public DataCell FilterBySkillId(int skillId)
    {
        return _dictElement
            .FirstOrDefault(kvp => kvp.Value.GetSkillId() == skillId).Value;
    }

    public List<DataCell> GetAllPassive(int startPosition, int endPosition)
    {
        return _dictElement
                        .Where(kvp => kvp.Value.IsPassiveSkill() &&
                        kvp.Key >= startPosition &&
                        kvp.Key <= endPosition)
                       .Select(kvp => kvp.Value)
                       .ToList();
    }

    public DataCell GetEmptyCell(int strtPosition, int endPosition)
    {
        return _dictElement
            .FirstOrDefault(kvp => !kvp.Value.IsBusy() &
             kvp.Key >= strtPosition & kvp.Key <= endPosition)
            .Value;
    }

    public DataCell IsPassiveFirstRows()
    {
        return _dictElement.FirstOrDefault(kvp => kvp.Value.IsPassiveSkill() & kvp.Key <= 12).Value;
    }


}
