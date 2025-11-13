using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class ToolTipManager 
{

    private static ToolTipManager _instance;
    private List<Product> _buyProduct;
    private List<Product> _sellProduct;



    public static ToolTipManager GetInstance()
    {
        if (_instance == null)
            _instance = new ToolTipManager();
        return _instance;
    }

    public void SetSellData(List<Product> sellProduct)
    {
        //if(_sellProduct != null) _sellProduct.Clear();
        _sellProduct = sellProduct;
    }

    public Product FindProductInSellList(int positionIndex)
    {

        if(IsIndexValid(_sellProduct, positionIndex))
        {
            return _sellProduct[positionIndex];
        }
        return null;
    }

    public void SetBuyData(List<Product> buyProduct)
    {
        _buyProduct = buyProduct;
    }

    public Product FindProductInBuyList(int positionIndex)
    {
        if (IsIndexValid(_buyProduct, positionIndex))
        {
            if(_buyProduct[positionIndex] != null)
            {
                return _buyProduct[positionIndex];
            }
            
        }
        return null;
    }

    private bool IsIndexValid(List<Product> list, int index)
    {
        if(list != null)
        {
            return index >= 0 && index < list.Count;
        }
        return false;
    }



    public void RegisterSimple(SlotType type , VisualElement list , int position)
    {
        ToolTipSimple.Instance.RegisterCallback(type , list , position);
    }

    public void EventLeftClickSlot(VisualElement slotElement)
    {
        ToolTipSimple.Instance.EventLeftClickSlot(slotElement);
    }
    //public static final int TYPE1_WEAPON_RING_EARRING_NECKLACE = 0;
    //public static final int TYPE1_SHIELD_ARMOR = 1;
    //public static final int TYPE1_ITEM_QUESTITEM_ADENA = 4;

    //public static final int TYPE2_WEAPON = 0;
    //public static final int TYPE2_SHIELD_ARMOR = 1;
    //public static final int TYPE2_ACCESSORY = 2;
    //public static final int TYPE2_QUEST = 3;
    //public static final int TYPE2_MONEY = 4;
    //public static final int TYPE2_OTHER = 5;

    public IDataTips GetProductText(object obj)
    {
        return new SimpleToolTipData(obj);
    }

}
