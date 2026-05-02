


using UnityEngine;
using UnityEngine.UIElements;


public class CreatorSets
{
    public void AddSetsElement(VisualElement container, VisualTreeAsset setsTemplate, VisualTreeAsset setsEffectsTemplate, ItemName[] setsName , ItemSets[] setsEffects)
    {
        string element = "setsElements";
        string effects = "setsEffect";

        if (setsName != null && setsName.Length > 0)
        {

            RemoveSetsElements(container, element);
            RemoveSetsElements(container, effects);
            int lastIndex = CreateSetsPart(container, setsTemplate, setsName);
            CreateSetsEffect(lastIndex , container, setsEffectsTemplate, setsEffects);
        }
        else
        {
            RemoveSetsElements(container, effects);
            RemoveSetsElements(container, element);
        }
    }

    private void RemoveSetsElements(VisualElement container, string nameElement)
    {
        var elementsToRemove = container.Query<VisualElement>(nameElement).ToList();

        if (elementsToRemove != null && elementsToRemove.Count > 0)
        {
            foreach (var element in elementsToRemove)
            {
                element.RemoveFromHierarchy();

            }
        }
    }


    private int CreateSetsPart(VisualElement container, VisualTreeAsset setsTemplate, ItemName[] setsName)
    {
        int index = 0;
        for (int i = 0; i < setsName.Length; i++)
        {
            ItemName itemName = setsName[i];
            int id = itemName.Id;
            var weapon = WeapongrpTable.Instance.GetWeapon(id);
            var armor = ArmorgrpTable.Instance.GetArmor(id);
            var item = EtcItemgrpTable.Instance.GetEtcItem(id);
            bool isEquip = PlayerInventory.Instance.IsItemEquipByItemId(id);

            if (weapon != null)
            {
                if(setsTemplate != null)
                {
                    container.Insert(i, CreateVisualElement(i, weapon, itemName.Name, setsTemplate, isEquip));
                }
            }
            else if (armor != null)
            {
                if (setsTemplate != null)
                {
                    container.Insert(i, CreateVisualElement(i, armor, itemName.Name, setsTemplate, isEquip));
                }
                    
            }
            else if (item != null)
            {
                if (setsTemplate != null)
                {
                    container.Insert(i, CreateVisualElement(i, item, itemName.Name, setsTemplate, isEquip));
                }
                    
            }

            index = i;
        }

        return index + 1;
    }

    private void CreateSetsEffect(int lastIndex, VisualElement container, VisualTreeAsset effectTemplate, ItemSets[] setsEffect)
    {
        if(effectTemplate != null)
        {
            for (int i = 0; i < setsEffect.Length; i++)
            {

                ItemSets itemName = setsEffect[i];
                string peace = GetTextPeace(itemName);
                container.Insert(lastIndex, CreateVisualElementEffect(peace, itemName.GetDescription(), effectTemplate));
                lastIndex++;
            }
        }
   
    }

    private string GetTextPeace(ItemSets itemName)
    {
        if(itemName.GetArrayId().Length > 1)
        {
            return itemName.GetArrayId().Length.ToString() + "Piece";
        }
        else if (itemName.GetArrayId().Length == 1)
        {
            int id_i = itemName.GetArrayId()[0];
            Weapongrp weapon = WeapongrpTable.Instance.GetWeapon(id_i);
            Armorgrp armor = ArmorgrpTable.Instance.GetArmor(id_i);

            if(weapon != null)
            {
                if(weapon.BodyPart == ItemSlot.lhand_shield)
                {
                    return "Shield";
                }
            }

            if(armor != null)
            {
                if (weapon.BodyPart == ItemSlot.gloves)
                {
                    return "Gloves";
                }
                else if (weapon.BodyPart == ItemSlot.head)
                {
                    return "Helmet";
                }
            }

            return itemName.GetArrayId().Length.ToString() + "Piece";
        }
        return "";
    }

    private TemplateContainer CreateVisualElementEffect(string peace , string text , VisualTreeAsset effectTemplate)
    {
        var template = effectTemplate.CloneTree();
        AddCountPiece(peace, template);
        AddTextEffect(text, template);

        return template;
    }


    private TemplateContainer CreateVisualElement(int i, Abstractgrp grpItem, string name, VisualTreeAsset setsTemplate , bool isEquip)
    {
        var template = setsTemplate.CloneTree();
        CreateHeaderif0Count(i, template);
        SetDataIcon(grpItem, template);
        SetDataLabel(name, template , isEquip);

        return template;
    }

    private void AddCountPiece(string i , TemplateContainer template)
    {
        Label labelName = template.Q<Label>("labelSetName");
        labelName.text = i;
    }

    private void AddTextEffect(string text, TemplateContainer template)
    {
        Label labelText = template.Q<Label>("labelSetText");
        labelText.text = text;
    }


    private void CreateHeaderif0Count(int i , TemplateContainer template)
    {
        if (i == 0)
        {
            Label labelHeader = template.Q<Label>("labelSetHeader");
            labelHeader.text = "<Set Effect>";
        }
    }

    private void SetDataIcon(Abstractgrp grpItem , TemplateContainer template)
    {
        VisualElement iconSet = template.Q<VisualElement>("iconSet");
        if (iconSet != null)
        {
            Texture2D texture = IconManager.Instance.LoadTextureByName(grpItem.Icon);
            iconSet.style.backgroundImage = texture;
        }
    }

    private void SetDataLabel(string name, TemplateContainer template , bool isEquip)
    {
        Label labelSet = template.Q<Label>("labelNameSet");
        if (labelSet != null)
        {
            SetColor(labelSet, isEquip);
            labelSet.text = name;
        }

    }

    private void SetColor(Label labelSet, bool isEquip)
    {
        if (isEquip)
        {
            Color color;
            ColorUtility.TryParseHtmlString("#d9d488", out color);
            labelSet.style.color = color;
        }
        else
        {
            Color color;
            ColorUtility.TryParseHtmlString("#61612E", out color);
            labelSet.style.color = color;
        }
    }

}
