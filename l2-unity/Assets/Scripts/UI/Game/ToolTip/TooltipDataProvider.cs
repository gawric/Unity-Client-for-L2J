using UnityEngine;
using UnityEngine.UIElements;


public class TooltipDataProvider : AbstractDataProvider
{
    private CreatorSets _creator;
    public TooltipDataProvider()
    {
        _creator = new CreatorSets();
    }

    public void AddDataSkill(VisualElement container, object item)
    {
        if (item.GetType() == typeof(SkillInstance))
        {
            SkillInstance skill = (SkillInstance)item;
            IDataTips text = ToolTipManager.GetInstance().GetProductText(skill);
            SetDataSkillInTemplate(container, skill, text , 0);
        }
    }
    public  void AddDataWeapon(VisualElement container, object item)
    {

        if (item != null)
        {
            if(item.GetType() == typeof(Product))
            {
                Product product = (Product)item;
                Weapongrp weapon = product.GetWeapon();
                IDataTips text = ToolTipManager.GetInstance().GetProductText(product);
                int price = int.Parse(text.GetPrice());
                SetDataWeaponInTemplate(container, weapon, price, text);
            }else if (item.GetType() == typeof(ItemInstance))
            {
                ItemInstance item_inventory = (ItemInstance)item;
                Weapongrp weapon = item_inventory.GetWeapon();
                IDataTips text = ToolTipManager.GetInstance().GetProductText(item_inventory);
                SetDataWeaponInTemplate(container, weapon, 0 , text);
            }
        }

    }

 

    public void AddDataAccessories(VisualElement container, object item)
    {
        if (item != null)
        {
            if (item.GetType() == typeof(Product))
            {
                Product product = (Product)item;
                Armorgrp weapon = product.GetArmor();
                IDataTips text = ToolTipManager.GetInstance().GetProductText(product);
                SetDataAccessoriesInTemplate(container, weapon, text , product.Price , product.GetTypeAccessoriesName());
            }
            else if (item.GetType() == typeof(ItemInstance))
            {
                ItemInstance item_inventory = (ItemInstance)item;
                Armorgrp weapon = item_inventory.GetArmor();
                IDataTips text = ToolTipManager.GetInstance().GetProductText(item_inventory);
                SetDataAccessoriesInTemplate(container, weapon, text , 0 , item_inventory.GetTypeAccessoriesName());
            }
        }
    }

    public void AddDataOther(VisualElement container, object item)
    {
        if (item != null)
        {
            if (item.GetType() == typeof(Product))
            {
                Product product = (Product)item;
                EtcItemgrp etcItem = product.GetEtcItem();
                IDataTips text = ToolTipManager.GetInstance().GetProductText(product);
                int price = int.Parse(text.GetPrice());
                SetOther(container, text, etcItem.Icon, price, product.GetTypeAccessoriesName(), etcItem.Weight.ToString());
            }
            else if (item.GetType() == typeof(ItemInstance))
            {
                ItemInstance item_inventory = (ItemInstance)item;
                EtcItemgrp etcItem = item_inventory.GetEtcItem();
                IDataTips text = ToolTipManager.GetInstance().GetProductText(item_inventory);
                SetOther(container, text, etcItem.Icon, 0, item_inventory.GetTypeAccessoriesName(), etcItem.Weight.ToString());
            }
        }
        
    }

    public void AddRequiredData(VisualElement container, object item)
    {
        ItemInstance item1 = (ItemInstance)item;
        EtcItemgrp etcItem = item1.GetEtcItem();
        IDataTips text = ToolTipManager.GetInstance().GetProductText(item1);
        int countInventory = PlayerInventory.Instance.GetItemByItemId(item1.ItemId) != null ? PlayerInventory.Instance.GetItemByItemId(item1.ItemId).Count : 0;
        SetRequired(container, text, etcItem.Icon, 0, item1.GetTypeAccessoriesName(), etcItem.Weight.ToString() , item1.GetRequiredCount() , countInventory);
    }


    public void AddDataArmor(VisualElement container, object item , VisualTreeAsset setsElements, VisualTreeAsset setsEffect)
    {
        if (item != null)
        {
            Abstractgrp data = null;
            int price = 0;
            string armorTypeName = "";

            SetDataInfo(item, ref price, ref armorTypeName, ref data);



            if (data != null)
            {
                IDataTips text = ToolTipManager.GetInstance().GetProductText(item);

                if (data.GetType() == typeof(Armorgrp))
                {
                    Armorgrp armor = (Armorgrp)data;

                    if(armor != null)
                    {
                        AddArmor(container, price, armorTypeName, text, armor , setsElements , setsEffect,  _creator);
                    }
                }
                else if (data.GetType() == typeof(Weapongrp))
                {

                    Weapongrp weapon = (Weapongrp)data;
                    if (weapon.WeaponType == WeaponType.shield)
                    {
                        armorTypeName = GetWeaponTypeName(item);
                        AddArmorShield(container, price, armorTypeName, text, weapon);
                    }

                }
            }
        }
    }

    public void AddDataIngredient(VisualElement container , ItemInstance item)
    {
        //set icon
        IDataTips text = ToolTipManager.GetInstance().GetProductText(item);

        if (item.GetArmor() != null)
        {
            Armorgrp armor = item.GetArmor();
            SetDataIngredient(container, text, armor.Icon , item.Count , item.GetName());
        }
        else if (item.GetWeapon() != null)
        {
            Weapongrp weapon = item.GetWeapon();
            SetDataIngredient(container, text, weapon.Icon, item.Count, item.GetName());
        }
        else if (item.GetEtcItem() != null)
        {
            EtcItemgrp etcItem = item.GetEtcItem();
            SetDataIngredient(container, text, etcItem.Icon, item.Count, item.GetName());
        }


    }

    

    private void  SetDataInfo(object item , ref int price , ref string armorTypeName , ref Abstractgrp data)
    {
        if (item.GetType() == typeof(Product))
        {
            Product product = (Product)item;
            price = product.Price;
            armorTypeName = product.GetTypeArmorName();
            data = product.GetOAbstractItem();
        }
        else if (item.GetType() == typeof(ItemInstance))
        {
            ItemInstance itemInstance = (ItemInstance)item;
            armorTypeName = itemInstance.GetTypeArmorName();
            data = itemInstance.GetOAbstractItem();
        }
    }

    private string GetWeaponTypeName(object item)
    {
        if (item.GetType() == typeof(Product))
        {
            Product product = (Product)item;
            return product.GetTypeWeaponName();
        }
        else if (item.GetType() == typeof(ItemInstance))
        {
            ItemInstance itemInstance = (ItemInstance)item;
            return itemInstance.GetTypeWeaponName();
        }

        return "";
    }

    

    



   

 


 




}
