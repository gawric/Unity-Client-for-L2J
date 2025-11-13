using System;
using UnityEngine;

public class SimpleToolTipData : IDataTips
{
    private string _name;
    private string _description;
    private string _item_description;
    private Product _product;
    private ItemInstance _itemInstance;
    private SkillInstance _skillInstance;
    private int _enchant;
    public SimpleToolTipData(object data)
    {
        if(data.GetType() == typeof(Product))
        {
            _product = (Product)data;
            _name = _product.GetName();
            _description = _product.GetDescription();
            _item_description = _product.GetItemDescription();
            _enchant = 0;
        }
        else if (data.GetType() == typeof(ItemInstance))
        {
            _itemInstance = (ItemInstance)data;
            _name = _itemInstance.GetName();
            _description = _itemInstance.GetDescription();
            _item_description = _itemInstance.GetItemDescription();
            _enchant = _itemInstance.EnchantLevel;
        }
        else if (data.GetType() == typeof(SkillInstance))
        {
            _skillInstance = (SkillInstance)data;
            _name = _skillInstance.GetName();
            _description = _skillInstance.GetDescription();
            _item_description = _skillInstance.GetDescription();
            _enchant = _skillInstance.Level;
        }
    }
    public string GetName(bool hideCount = false)
    {

        if (hideCount)
        {
            return _name;
        }

        if (_product != null)
        {
            if (_product.Count > 1) _name = _name + " (" + _product.Count + ")";
        }
        else if (_itemInstance != null)
        {
            if(_itemInstance.Category == ItemCategory.Adena)
            {
                if (_itemInstance.Count > 1) _name = _name + " (" + ToolTipsUtils.ConvertToPrice(_itemInstance.Count)+")";
            }
            else
            {
                if (_itemInstance.Count > 1) _name = _name + " (" + _itemInstance.Count + ")";
            }
           
        }

        return _name;
    }

    public Texture2D GetGradeTexture()
    {
        if (_product != null)
        {
            return null;
        }
        else if (_itemInstance != null)
        {
            return _itemInstance.GetGradeTexture();
        }
        return null;
    }

    public ItemName[] GetSets()
    {
        if (_product != null)
        {
            return _product.GetSets();
        }
        else if (_itemInstance != null)
        {
            return _itemInstance.GetSets();
        }
        return null;
        
    }

    public ItemSets[] GetSetsEffect()
    {
        if (_product != null)
        {
            return _product.GetSetsEffects();
        }
        else if (_itemInstance != null)
        {
            return _itemInstance.GetSetsEffects();
        }
        return null;

    }

    public string GetPrice()
    {
        if(_product != null)
        {
            return _product.Price.ToString();
        }

        return "0";
    }

    public string GetDiscription()
    {
        return _description;
    }

    public string GetItemDiscription()
    {
        return _item_description;
    }

    public string GetIcon()
    {
        throw new System.NotImplementedException();
    }

    public int GetEnchant()
    {
        return _enchant;
    }

   
}
