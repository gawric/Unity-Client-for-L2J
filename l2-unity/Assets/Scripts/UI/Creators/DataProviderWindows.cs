using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;

public class DataProviderWindows :AbstractDataProvider
{

    public void AddRewardItem(string demoIconName, string demoDescName, string demoNameTexture, VisualElement container)
    {
        //set icon
        VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
        VisualElement icon = container.Q<VisualElement>("icon");
        Texture2D texture = IconManager.Instance.LoadTextureByName(demoNameTexture);
        AddElementIfNotNull(groupBoxIcon, icon, texture);

        VisualElement groupDesc = container.Q<VisualElement>("GrowDesc");
        Label descLabel = container.Q<Label>("descLabel");
        AddElementIfNotEmpty(groupDesc, descLabel, demoIconName);

        VisualElement groupName = container.Q<VisualElement>("GrowName");
        Label nameLabel = container.Q<Label>("nameLabel");
        AddElementIfNotEmpty(groupName, nameLabel, demoDescName);
    }
   public void AddLearnSkill(int skillId , int correctCost , int level , VisualElement container)
   {
        Skillgrp skill = SkillgrpTable.Instance.GetSkill(skillId , level);
        SkillNameData skillName = SkillgrpTable.Instance.GetSkillName(skillId, level);

        //set icon
        VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
        VisualElement icon = container.Q<VisualElement>("icon");
        Texture2D texture = IconManager.Instance.LoadTextureByName(skill.Icon);
        AddElementIfNotNull(groupBoxIcon, icon, texture);

        Label nameLabel = container.Q<Label>("nameLabel");
        Label levelLabel = container.Q<Label>("settingLevel");
        Label spLabel = container.Q<Label>("settingSp");


        AddElementIfNotEmpty(nameLabel, nameLabel, skillName._name);
        AddElementIfNotEmpty(levelLabel, levelLabel, level.ToString());
        AddElementIfNotEmpty(spLabel, spLabel, correctCost.ToString());
    }

    public void AddDescriptionSkill(int skillId, int correctCost, int level, VisualElement container)
    {
        Skillgrp skill = SkillgrpTable.Instance.GetSkill(skillId, level);
        SkillNameData skillName = SkillgrpTable.Instance.GetSkillName(skillId, level);

        //set icon
        VisualElement groupBoxIcon = container.Q<VisualElement>("GrowIcon");
        VisualElement icon = container.Q<VisualElement>("icon");
        Texture2D texture = IconManager.Instance.LoadTextureByName(skill.Icon);
        AddElementIfNotNull(groupBoxIcon, icon, texture);

        Label skillNameLabel = container.Q<Label>("skillNameLabel");
        Label skillLevelLabel = container.Q<Label>("skillSettingLevel");
        Label spLabel = container.Q<Label>("spLabel");
        Label activeSkillLabel = container.Q<Label>("activeSkillLabel");
        
        AddElementIfNotEmpty(skillNameLabel, skillNameLabel, skillName._name);
        AddElementIfNot0(skillLevelLabel, skillLevelLabel, level);
        AddElementIfNot0(spLabel, spLabel, correctCost);
        AddElementIfNotEmpty(skillNameLabel, skillNameLabel, skillName._name);
        AddElementIfNotEmpty(activeSkillLabel, activeSkillLabel, skill.GetActiveSkillNameOrNot());


        VisualElement groupBoxSkillMp = container.Q<VisualElement>("Requirements");
        Label skillMpLabel = container.Q<Label>("skillMpLabel");
        AddElementIfNotNullVisibleHide(groupBoxSkillMp, skillMpLabel, skill.MpConsume);

        VisualElement groupBoxRang = container.Q<VisualElement>("Range");
        Label skillRangeLabel = container.Q<Label>("skillRangeLabel");
        AddElementIfNotNullVisibleHide(groupBoxRang, skillRangeLabel, skill.CastRange);

        Label skillDescription = container.Q<Label>("skillDescription");
        AddElementIfNotEmpty(skillDescription, skillDescription, skillName.Desc);


    }

    public void AddRequiredSkillInfo(List<RequiredSkillInfo> requiredSkillInfo, VisualElement container)
    {
        if(requiredSkillInfo.Count == 0)
        {
            HideRequiredPanel(container);
            return;
        }

        for(int i =0; i < requiredSkillInfo.Count; i++)
        {
            RequiredSkillInfo itemInfo = requiredSkillInfo[i];
            EtcItemgrp item = EtcItemgrpTable.Instance.GetEtcItem(itemInfo.GetItemId());

            if (item != null)
            {
                ItemName itemName = ItemNameTable.Instance.GetItemName(itemInfo.GetItemId());
                Label itemNamelabel = container.Q<Label>("itemNameLabel");
                AddElementIfNotEmpty(itemNamelabel, itemNamelabel, itemName.Name +" X " + itemInfo.GetCount());

                //set icon
                VisualElement groupBoxIcon = container.Q<VisualElement>("RequiredItem");
                VisualElement icon = container.Q<VisualElement>("iconItem");
                Texture2D texture = IconManager.Instance.LoadTextureByName(item.Icon);
                AddElementIfNotNull(groupBoxIcon, icon, texture);
            }
            else
            {

                HideRequiredPanel(container);
            }
            
        }
    }

    private void HideRequiredPanel(VisualElement container)
    {
        //RequiredItem all Required element
        VisualElement groupBoxIcon = container.Q<VisualElement>("RequiredItem");
        VisualElement icon = container.Q<VisualElement>("iconItem");
        AddElementIfNotNull(groupBoxIcon, icon, null);
    }

 
}
