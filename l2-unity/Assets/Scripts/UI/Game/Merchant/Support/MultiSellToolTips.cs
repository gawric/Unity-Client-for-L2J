using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MultiSellToolTips
{
    private TooltipDataProvider _dataProvider;

    private VisualTreeAsset _windowTemplateWeapon;
    private VisualTreeAsset _windowTemplateAcccesories;
    private VisualTreeAsset _windowTemplateArmor;
    private VisualTreeAsset _itemTemplateIngredient;
    
    public MultiSellToolTips(TooltipDataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    public void SetTemplate(VisualTreeAsset windowTemplateWeapon , VisualTreeAsset windowTemplateAcccesories, VisualTreeAsset windowTemplateArmor , VisualTreeAsset itemTemplateIngredient)
    {
        _windowTemplateWeapon = windowTemplateWeapon;
        _windowTemplateAcccesories = windowTemplateAcccesories;
        _windowTemplateArmor = windowTemplateArmor;
        _itemTemplateIngredient = itemTemplateIngredient;
    }
    public void UseItem(ItemInstance item, VisualElement template)
    {
        switch (item.Category)
        {
            case ItemCategory.Weapon:
                _dataProvider.AddDataWeapon(template, item);
                break;
            case ItemCategory.Jewel:
                _dataProvider.AddDataAccessories(template, item);
                break;
            case ItemCategory.Item:
                _dataProvider.AddDataOther(template, item);
                break;
            case ItemCategory.ShieldArmor:
                _dataProvider.AddDataArmor(template, item, null, null);
                break;
        }
    }

    private void UseIngredient(VisualElement template, ItemInstance item)
    {
        if (item != null)
        {
            _dataProvider.AddDataIngredient(template, item);
        }
    }


    private VisualElement SwitchToWeapon( VisualTreeAsset _windowTemplateWeapon)
    {
       
        VisualElement weapon = ToolTipsUtils.CloneOne(_windowTemplateWeapon);
        var boxBackground = weapon.Q<VisualElement>("Box");
        var topBackground = weapon.Q<VisualElement>("Top");
        var centerBackground = weapon.Q<VisualElement>("Center");
        var growIcon = weapon.Q<VisualElement>("GrowIcon");

        ClearBorder(boxBackground, topBackground, centerBackground, growIcon);
        List<VisualElement> listElement = GetReduceElement(weapon);
        ReduceInterval(listElement);
        return weapon;
    }


    private List<VisualElement> GetReduceElement(VisualElement weapon)
    {
        List<VisualElement> listElement = new List<VisualElement>(7);
        var phisAtkText = weapon.Q<VisualElement>("phisAtkText");
        listElement.Add(phisAtkText);
        var magAtkText = weapon.Q<VisualElement>("magAtkText");
        listElement.Add(magAtkText);
        var speedText = weapon.Q<VisualElement>("speedText");
        listElement.Add(speedText);
        var critText = weapon.Q<VisualElement>("critText");
        listElement.Add(critText);
        var soulText = weapon.Q<VisualElement>("soulText");
        listElement.Add(soulText);
        var spiritText = weapon.Q<VisualElement>("spiritText");
        listElement.Add(spiritText);
        var weightText = weapon.Q<VisualElement>("weightText");
        listElement.Add(weightText);
        return listElement;
    }



    private VisualElement SwitchToAccessories(VisualTreeAsset _windowTemplateAcccesories)
    {
        

        var accessories = ToolTipsUtils.CloneOne(_windowTemplateAcccesories);
        var boxBackground = accessories.Q<VisualElement>("Box");
        var topBackground = accessories.Q<VisualElement>("Top");
        var centerBackground = accessories.Q<VisualElement>("Center");
        var growIcon = accessories.Q<VisualElement>("GrowIcon");
        ClearBorder(boxBackground, topBackground, centerBackground, growIcon);

        return accessories;
    }

    private VisualElement SwitchToArmor( VisualTreeAsset _windowTemplateArmor)
    {
      

        var armor = ToolTipsUtils.CloneOne(_windowTemplateArmor);
        var boxBackground = armor.Q<VisualElement>("Box");
        var topBackground = armor.Q<VisualElement>("Top");
        var centerBackground = armor.Q<VisualElement>("Center");
        var growIcon = armor.Q<VisualElement>("GrowIcon");
        ClearBorder(boxBackground, topBackground, centerBackground, growIcon);

        return armor;

    }

    public void AddIngredient(List<Ingredient> listIngredient, VisualElement contentPanel2)
    {
        foreach (Ingredient ingredient in listIngredient)
        {
            VisualElement template = GetIngredientContainer(_itemTemplateIngredient);
            if (template != null)
            {
                UseIngredient(template, ingredient.GetItemInstance());
                contentPanel2.Add(template);
            }

        }
    }

    private void ClearBorder(VisualElement boxBackground, VisualElement topBackground, VisualElement centerBackground, VisualElement growIcon)
    {
        StyleColor currentColor = boxBackground.style.backgroundColor;
        boxBackground.style.backgroundColor = new Color(currentColor.value.r, currentColor.value.g, currentColor.value.b, 0);

        ResetBorder(boxBackground);

        topBackground.style.borderBottomWidth = 0;
        centerBackground.style.borderBottomWidth = 0;

        if (growIcon != null)
        {
            ResetBorder(growIcon);
        }
    }

    private void ReduceInterval(List<VisualElement> list)
    {
        foreach (VisualElement item in list)
        {
            if (item != null)
            {
                if (item.name.Equals("phisAtkText"))
                {
                    item.style.marginTop = -2;
                    Debug.Log("phisAtkText -2 enabled");
                }
                else
                {
                    item.style.marginTop = 2;
                }

            }

        }
    }

    private void ResetBorder(VisualElement element)
    {
        element.style.borderBottomWidth = 0;
        element.style.borderLeftWidth = 0;
        element.style.borderRightWidth = 0;
        element.style.borderTopWidth = 0;
    }

    public VisualElement GetContainer(ItemInstance item)
    {
        switch (item.Category)
        {
            case ItemCategory.Weapon:
                return SwitchToWeapon(_windowTemplateWeapon);
            case ItemCategory.Jewel:
            case ItemCategory.Item:
                return SwitchToAccessories(_windowTemplateAcccesories);
            case ItemCategory.ShieldArmor:
                return SwitchToArmor(_windowTemplateArmor);
        }
        return null;
    }

    public VisualElement GetIngredientContainer(VisualTreeAsset itemTemplateIngredient)
    {
        return ToolTipsUtils.CloneOne(itemTemplateIngredient);
    }

   

}
