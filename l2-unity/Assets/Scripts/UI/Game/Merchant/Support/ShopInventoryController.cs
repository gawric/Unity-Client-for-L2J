using System.Collections.Generic;
using static L2Slot;
using UnityEngine.UIElements;
using System;
using System.Linq;

public class ShopInventoryController
{
    private DealerWindow _dealerWindow;
    private int _defaultCount;
    public ShopInventoryController(DealerWindow dealerWindow, int defaultCount)
    {
        _dealerWindow = dealerWindow;
        _defaultCount = defaultCount;
    }
    public void CreateSell(List<Product> listBuy , InventoryTab tabSell , VisualElement contentSell , VisualElement windowEle)
    {
        if (tabSell == null)
        {
            tabSell = new InventoryTab();
            _dealerWindow.SetTabSell(tabSell);
            tabSell.Initialize(windowEle, contentSell, contentSell , false , false);
        }

        contentSell.Clear();
        

        ToolTipManager.GetInstance().SetSellData(listBuy);

        var inventorySlotSell = _dealerWindow.GetInventorySlotsSell();
        var inventorySlotTemplate = _dealerWindow.GetInventorySlotTemplate();

        if (listBuy == null | listBuy.Count == 0)
        {
            var inventorySlotsSell = CreateEmptyTab(tabSell , contentSell, inventorySlotSell, inventorySlotTemplate);
            _dealerWindow.SetInventorySlotsSell(inventorySlotsSell);
        }
        else
        {
            CreateNoEmptySell(listBuy, tabSell , inventorySlotSell, inventorySlotTemplate,  contentSell );
        }
    }

    public void CreateBuy(List<Product> listBuy, InventoryTab tabBuy, VisualElement contentBuy, VisualElement windowEle)
    {
        if (tabBuy == null)
        {
            tabBuy = new InventoryTab();
            _dealerWindow.SetTabBuy(tabBuy);
            tabBuy.Initialize(windowEle, contentBuy, contentBuy , false, false);
        }

        contentBuy.Clear();

        ToolTipManager.GetInstance().SetBuyData(new List<Product>());

        var inventorySlotBuy = _dealerWindow.GetInventorySlotsBuy();
        var inventorySlotTemplate = _dealerWindow.GetInventorySlotTemplate();

        if (listBuy == null | listBuy.Count == 0)
        {
            var inventorySlotsBuy  = CreateEmptyTab(tabBuy, contentBuy, inventorySlotBuy, inventorySlotTemplate);
            _dealerWindow.SetInventorySlotsBuy(inventorySlotsBuy);
        }
        else
        {
            CreateNoEmptyBuy(listBuy, tabBuy, inventorySlotBuy, inventorySlotTemplate, contentBuy);
        }
    }

  

    private InventorySlot[] CreateEmptyTab(InventoryTab tabSell, VisualElement contentSell , InventorySlot[] inventorySlotsSell , VisualTreeAsset inventorySlotTemplate)
    {
        inventorySlotsSell = new InventorySlot[_defaultCount];

        for (int i = 0; i < inventorySlotsSell.Length; i++)
        {
            VisualElement slotElement = inventorySlotTemplate.Instantiate()[0];
            contentSell.Add(slotElement);

            InventorySlot slot = new InventorySlot(i, slotElement, tabSell, SlotType.PriceSell , false);
            inventorySlotsSell[i] = slot;
        }
        
        tabSell.UpdateInventorySlots(inventorySlotsSell);
        return inventorySlotsSell;
    }

    private void CreateNoEmptySell(List<Product> listBuy, InventoryTab tabSell , InventorySlot[] inventorySlotsSell, VisualTreeAsset inventorySlotTemplate, VisualElement contentSell)
    {
            int count =  GetCountCells(listBuy.Count);
            inventorySlotsSell = InitEmpty(count, tabSell, contentSell, inventorySlotTemplate ,_dealerWindow.GetInventorySlotsSell() , SlotType.PriceSell);
            tabSell.UpdateInventorySlots(inventorySlotsSell);

            for (int i = 0; i < listBuy.Count; i++)
            {
                inventorySlotsSell[i].AssignProduct(listBuy[i]);
            }
 
        _dealerWindow.SetInventorySlotsSell(inventorySlotsSell);

    }

    private void CreateNoEmptyBuy(List<Product> listBuy, InventoryTab tabBuy, InventorySlot[] inventorySlots, VisualTreeAsset inventorySlotTemplate, VisualElement contentBuy)
    {
        int count = GetCountCells(listBuy.Count);
        inventorySlots = InitEmpty(count, tabBuy, contentBuy, inventorySlotTemplate, _dealerWindow.GetInventorySlotsBuy() , SlotType.PriceBuy);
        tabBuy.UpdateInventorySlots(inventorySlots);
        _dealerWindow.SetInventorySlotsBuy(inventorySlots);
    }



    private InventorySlot[] InitEmpty(int count, InventoryTab tab , VisualElement contentSell , VisualTreeAsset inventorySlotTemplate , InventorySlot[]  inventorySlots , SlotType type)
    {
        

        if (inventorySlots == null)
        {

            inventorySlots = CreateNewArray(count);

            for (int i = 0; i < inventorySlots.Length; i++)
            {
                var slotElement = CreateSlotElement(inventorySlotTemplate, contentSell);
                var slot = CreateSlotInventory(slotElement, tab, i, type);
                inventorySlots[i] = slot;
            }
        }
        else
        {

            inventorySlots =  ReCreateElseChangeCount(inventorySlots, count);

            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if(inventorySlots[i] == null)
                {
                    var slotElement  = CreateSlotElement(inventorySlotTemplate, contentSell);
                    var slot = CreateSlotInventory(slotElement, tab, i , type);
                    inventorySlots[i] = slot;
                }
                else
                {
                    inventorySlots[i].AssignEmpty();
                    contentSell.Add(inventorySlots[i].SlotElement);
                }
            }
        }

        

        return inventorySlots;
    }

    private InventorySlot CreateSlotInventory(VisualElement slotElement , InventoryTab tabSell , int i , SlotType type)
    {
        InventorySlot slot = new InventorySlot(i, slotElement, tabSell, type , false);
        return slot;
    }

    private VisualElement CreateSlotElement(VisualTreeAsset inventorySlotTemplate , VisualElement contentSell)
    {
        VisualElement slotElement = inventorySlotTemplate.Instantiate()[0];
        contentSell.Add(slotElement);
        return slotElement;
    }

    private InventorySlot[] CreateNewArray(int count)
    {
            return  new InventorySlot[count];       
    }

    private InventorySlot[] ReCreateElseChangeCount(InventorySlot[] _inventorySlotsSell, int count)
    {
        if (count > _inventorySlotsSell.Length)
        {
            InventorySlot[] newArray = new InventorySlot[count];
            return newArray;
        }else if (count < _inventorySlotsSell.Length)
        {
            InventorySlot[] newArray = new InventorySlot[count];
            return newArray;
        }

        return _inventorySlotsSell;
    }



    public static InventorySlot[] ExpandArray(InventorySlot[] originalArray, int additionalCount)
    {

        if (originalArray == null)
        {
            throw new ArgumentNullException(nameof(originalArray), "Original array cannot be null.");
        }

        if (additionalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(additionalCount), "Additional count cannot be negative.");
        }


        InventorySlot[] newArray = new InventorySlot[originalArray.Length + additionalCount];

        return newArray;
    }

    public int GetNextMultipleOfSix(int number)
    {

        int remainder = number % 6;


        if (remainder == 0)
        {
            return number;
        }


        return number + (6 - remainder);
    }

    public SlotType DetectedClickPanel(int type)
    {

        if (type == (int)SlotType.PriceBuy)
        {
            return SlotType.PriceBuy;
        }
        else if (type == (int)SlotType.PriceSell)
        {
            return SlotType.PriceSell;
        }
        return SlotType.Other;
    }


    public List<Product> AddProduct(List<Product> listSell, Product source)
    {
        if (listSell == null)
        {
            return new List<Product>() { source };
        }

        bool isUpdate = false;

        if(source.GetTypeItem() == EnumType2.TYPE2_OTHER)
        {
            
            isUpdate = UpdateList(isUpdate, listSell, source , true);
        }

        if(!isUpdate) listSell.Add(source);

        return listSell;
    }

  

    public List<Product> RemoveProduct(List<Product> listSell, Product source , bool isRemoval)
    {
        if (listSell == null)
        {
            return new List<Product>() { source };
        }

        bool isUpdate = false;

        if (source.GetTypeItem() == EnumType2.TYPE2_OTHER && isRemoval == false)
        {
            isUpdate = UpdateList(isUpdate, listSell, source , false);
        }

        if (!isUpdate || isRemoval || source.Count == 0) listSell.Remove(source);

        return listSell;
    }


    private bool UpdateList(bool isUpdate , List<Product> listSell, Product source , bool plus)
    {
        foreach (Product product in listSell)
        {
            if (product.ItemId == source.ItemId)
            {
               
                if (plus)
                {
                    UpdateProductPlus(product, source);
                }
                else
                {
                    //UpdateProductMinus(product, source);
                }

                isUpdate = true;
            }
        }

        return isUpdate;
    }

    private void UpdateProductPlus(Product item, Product source)
    {
        item.SetCount(item.Count + source.Count);
    }

    private void UpdateProductMinus(Product item, Product source)
    {
        item.SetCount(item.Count - source.Count);
    }


    public void ClearSell(InventoryTab tabSell)
    {
        if (tabSell != null)
        {
            tabSell.UnSelectSlot();
            tabSell.ResetSelectSlot();
        }

    }

    public void ClearBuy(InventorySlot[] inventorySlotsBuy, InventoryTab tabBuy)
    {
        if (inventorySlotsBuy != null && inventorySlotsBuy.Count() > 0)
        {
            for (int i = 0; i < inventorySlotsBuy.Count(); i++)
            {
                InventorySlot i_slot = inventorySlotsBuy[i];
                i_slot.AssignEmpty();
            }
        }
        tabBuy.UnSelectSlot();
        tabBuy.ResetSelectSlot();
        tabBuy.UpdateInventorySlots(inventorySlotsBuy);

    }

    public InventorySlot[] CreateEmptyBuy(InventoryTab tabBuy , int defaultCount , VisualElement contentBuy , VisualTreeAsset InventorySlotTemplate)
    {
        var inventorySlotsBuy = new InventorySlot[defaultCount];
        for (int i = 0; i < inventorySlotsBuy.Count(); i++)
        {
            VisualElement slotElement = InventorySlotTemplate.Instantiate()[0];
            contentBuy.Add(slotElement);

            InventorySlot slot = new InventorySlot(i, slotElement, tabBuy, SlotType.PriceBuy , false);
            inventorySlotsBuy[i] = slot;
        }

        tabBuy.UpdateInventorySlots(inventorySlotsBuy);

        return inventorySlotsBuy;
    }

    //24 minimal cells count
    private int GetCountCells(int countBuy)
    {
        return (countBuy <= _defaultCount) ? _defaultCount : GetNextMultipleOfSix(countBuy);
    }

}
