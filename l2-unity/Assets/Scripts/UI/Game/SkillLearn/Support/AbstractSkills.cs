using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class AbstractSkills
{
    protected const float _default7CellWidth = 252;
    protected const float _default6CellWidth = 216;

    protected const string _rowNameInnerPanel = "RowsVirtual";
    protected Dictionary<int, SkillSlot> _allSlots;
    protected int skillsPerPanel = 7;
    public void SetAllSlots(Dictionary<int, SkillSlot> allSlots)
    {
        if (allSlots != null) _allSlots = null;
        _allSlots = allSlots;
    }
    protected void ChangeDfBox(Button btn, string texture)
    {
        IEnumerable<VisualElement> children = btn.Children();
        var e = children.First();
        e.style.display = DisplayStyle.Flex;
        Texture2D iconDfNoraml = LoadTextureDF(texture);
        SetBackgroundDf(btn, iconDfNoraml);
    }

    protected void ToogleHideElement(VisualElement element , bool isHide)
    {
        if(!isHide)
            element.style.display = DisplayStyle.Flex;
        else
            element.style.display = DisplayStyle.None;
    }



    protected void SetBackgroundDf(UnityEngine.UIElements.Button btn, Texture2D iconDfNoraml)
    {
        btn.style.backgroundImage = new StyleBackground(iconDfNoraml);
    }

    protected Texture2D LoadTextureDF(string path)
    {
        return Resources.Load<Texture2D>(path);
    }

    protected VisualElement GetSkillBar(VisualElement content)
    {
        var childreb = content.Children();
        int i = 0;
        foreach (VisualElement item in childreb)
        {
            if (i > 0)
            {
                return item;
            }
            i++;
        }

        return null;
    }

    protected void UpdateSkills(List<SkillInstance> added, List<SkillInstance> removed, List<SkillInstance> allList , List<VisualElement> rowPanels, VisualTreeAsset templateSlotSkill , VisualTreeAsset templatePanel8x1, VisualElement boxPanel)
    {
        UpdateSkillInfo(allList);
        RemoveSkill(removed);

        AddSkill(added, rowPanels, templateSlotSkill, templatePanel8x1 , boxPanel);
        //RemoveEmptyPanels(boxPanel);
        RearrangeSkillSlots(rowPanels,  boxPanel);
        RemoveEmptyPanels(boxPanel);
    }

    private void UpdateSkillInfo(List<SkillInstance> allList)
    {
        foreach (SkillInstance skill in allList)
        {
            SkillSlot slot;

            _allSlots.TryGetValue(skill.SkillID, out slot);
            if (slot != null)
            {
                slot.UpdateData(skill);
            }
        }
    }

    private void RemoveSkill(List<SkillInstance> removed)
    {
       
        if (removed != null && removed.Count > 0)
        {
            for (int i = 0; i < removed.Count; i++)
            {
                int skillId = removed[i].SkillID;
                SkillSlot slot;
                _allSlots.TryGetValue(skillId, out slot);
                
                if (slot != null)
                {
                    slot.AssignDestroy();
                }
                else
                {
                    Debug.Log("Not found slot for remove skill id " + skillId);
                }

                _allSlots.Remove(skillId);

            }
        }
    }

    private void RemoveEmptyPanels(VisualElement boxPanel)
    {
        List<VisualElement> delete = new List<VisualElement>();
        for (int i = boxPanel.childCount - 1; i >= 0; i--)
        {
            var panel = boxPanel[i];
            var rowNameVirtual = panel.Q(_rowNameInnerPanel);

            if (rowNameVirtual == null || rowNameVirtual.childCount == 0)
            {
                //ignore Background and Border Panel design elements
                if (panel.name != "Background" && panel.name != "Border")
                {
                    delete.Add(panel);
                }

            }

        }

        foreach (VisualElement item in delete)
        {
            Debug.Log("Delete box panel " + item.name);
            boxPanel.Remove(item);
        }
    }

    private void RearrangeSkillSlots(List<VisualElement> rowPanels, VisualElement boxPanel)
    {
        //const int skillsPerPanel = 7;
        List<VisualElement> toDelete = new List<VisualElement>();

        // Собираем все SkillSlot из всех панелей
        List<VisualElement> allSlots = new List<VisualElement>();
        foreach (var panel in rowPanels)
        {
            var rowNameVirtual = panel.Q(_rowNameInnerPanel);
            if (rowNameVirtual != null)
            {
                for (int i = 0; i < rowNameVirtual.childCount; i++)
                {
                    allSlots.Add(rowNameVirtual[i]);
                }
                rowNameVirtual.Clear();
            }
        }


        int slotIndex = 0;
        foreach (var panel in rowPanels)
        {
            var rowNameVirtual = panel.Q(_rowNameInnerPanel);
            if (rowNameVirtual != null)
            {
                for (int j = 0; j < skillsPerPanel && slotIndex < allSlots.Count; j++, slotIndex++)
                {
                    rowNameVirtual.Add(allSlots[slotIndex]);
                }
            }
        }

    }


    protected void AddSkill(List<SkillInstance> added, List<VisualElement> rowPanels, VisualTreeAsset templateSlotSkill, VisualTreeAsset templatePanel8x1 , VisualElement boxPanel)
    {
        //const int skillsPerPanel = 7;

        foreach (var skill in added)
        {
            VisualElement targetPanel = null;

            // Если панелей нет, создаём первую
            if (rowPanels.Count == 0)
            {
                targetPanel = ToolTipsUtils.CloneOne(templatePanel8x1);
                var rowNameVirtual1 = targetPanel.Q(_rowNameInnerPanel);
                rowPanels.Add(rowNameVirtual1);
                boxPanel.Add(targetPanel);
            }
            else
            {
                // Берём последнюю панель
                targetPanel = rowPanels[rowPanels.Count - 1];
                // Считаем количество ячеек в RowsVirtual
                var rowNameVirtual = targetPanel.Q(_rowNameInnerPanel);
                int currentCount = rowNameVirtual != null ? rowNameVirtual.childCount : 0;

                // Если панель заполнена, создаём новую
                if (currentCount >= skillsPerPanel)
                {
                    targetPanel = ToolTipsUtils.CloneOne(templatePanel8x1);
                    var rowNameVirtual1 = targetPanel.Q(_rowNameInnerPanel);
                    rowPanels.Add(rowNameVirtual1);
                    boxPanel.Add(targetPanel);
                }
            }

            // Добавляем слот в панель
            var rowNameVirtualAdd = targetPanel.Q(_rowNameInnerPanel);
            var slotElement = ToolTipsUtils.CloneOne(templateSlotSkill);
            var skillSlot = new SkillSlot(slotElement, skill.SkillID, SlotType.SkillWindow);
            skillSlot.AssignSkill(skill);
            //skillSlot.EventLeftClick += OnClickLeftEvent;
            rowNameVirtualAdd.Add(slotElement);

            if (!_allSlots.ContainsKey(skill.SkillID))
            {
                _allSlots.Add(skill.SkillID, skillSlot);
            }
            else
            {
                Debug.Log("AddSkill dublication found " + skill.SkillID);
            }

        }
    }




    protected VisualElement CreateSlots(List<SkillInstance> skillList, VisualTreeAsset templatePanel8x1, VisualTreeAsset templateSlotsSkill, VisualElement boxPanel)
    {
       
        int totalSkills = skillList.Count;
        int panelCount = GetCountPanel(totalSkills, skillsPerPanel);

        int skillIndex = 0;

       // if(_allSlots != null) _allSlots.Clear();

        for (int i = 0; i < panelCount; i++)
        {
            var panel = ToolTipsUtils.CloneOne(templatePanel8x1);
            var rowNameVirtual = panel.Q(_rowNameInnerPanel);

            for (int j = 0; j < skillsPerPanel && skillIndex < totalSkills; j++, skillIndex++)
            {
                SkillInstance instance = skillList[skillIndex];

                if(instance.SkillID != -1)
                {
                    var slotElement = ToolTipsUtils.CloneOne(templateSlotsSkill);
                    var skillSlot = new SkillSlot(slotElement, instance.SkillID, SlotType.SkillWindow);
                    skillSlot.AssignSkill(instance);
                    rowNameVirtual.Add(slotElement);
                    AddSlot(skillSlot);
                }

            }

            boxPanel.Add(panel);
 
        }

        return boxPanel;
    }

    private int GetCountPanel(int totalSkills , int skillsPerPanel)
    {
        if (totalSkills == 0)
        {
            return  1;
        }
        else
        {
            return  (int)Math.Ceiling((double)totalSkills / skillsPerPanel);
        }
    }

    private void AddSlot(SkillSlot skillSlot)
    {
        if (!_allSlots.ContainsKey(skillSlot.Id))
        {
            _allSlots.Add(skillSlot.Id, skillSlot);
        }
        else
        {
            Debug.Log("AbstractSkills->AddSlot: Dublication id slot");
        }

    }

    protected int CalculatePanelCount(List<SkillInstance> list)
    {
        //int skillsPerPanel = 7;
        int totalSkills = list.Count;
        int panelCount = (int)Math.Ceiling((double)totalSkills / skillsPerPanel);
        return panelCount;
    }

    protected void CompareSkillLists(List<SkillInstance> newList, List<SkillInstance> oldList, out List<SkillInstance> added, out List<SkillInstance> removed)
    {

        var oldIds = oldList?.Select(s => s.SkillID).ToHashSet() ?? new HashSet<int>();
        var newIds = newList?.Select(s => s.SkillID).ToHashSet() ?? new HashSet<int>();

        added = newList?.Where(s => !oldIds.Contains(s.SkillID)).ToList() ?? new List<SkillInstance>();
        removed = oldList?.Where(s => !newIds.Contains(s.SkillID)).ToList() ?? new List<SkillInstance>();
    }


    protected void ShowPanelIfCount1(List<SkillInstance> list, VisualElement panel, VisualElement allContent)
    {
        if (list != null | list.Count > 0)
        {
            panel.Clear();
            allContent.style.display = DisplayStyle.Flex;
        }
    }

    protected void HidePanelIfCount0(List<SkillInstance> list, VisualElement panel, VisualElement allContent)
    {
        if (list == null | list.Count == 0)
        {
            //panel.Clear();
            allContent.style.display = DisplayStyle.None;
        }
    }

    public void SetWidthBoxPanel(VisualElement _templateBoxPanel , int sizeCell)
    {
        float  width = (sizeCell == 6) ? _default6CellWidth : _default7CellWidth;

        if (_templateBoxPanel != null)
        {
           
            _templateBoxPanel.style.width = width;
        }
        else
        {
            Debug.LogWarning("ActiveSkillsHide > _templateBoxPanel is null, cannot set width");
        }
    }

}
