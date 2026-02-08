using UnityEngine;
using UnityEngine.UIElements;

public class AbstractSimpleToolTip
{

    protected string _name;
    protected string _description;
    protected string _item_description;
    protected Product _product;
    protected ItemInstance _itemInstance;
    protected SkillInstance _skillInstance;
    protected ActionData _actionInstance;
    protected int _enchant;

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
            if (_itemInstance.Category == ItemCategory.Adena)
            {
                if (_itemInstance.Count > 1) _name = _name + " (" + ToolTipsUtils.ConvertToPrice(_itemInstance.Count) + ")";
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
        if (_product != null)
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
