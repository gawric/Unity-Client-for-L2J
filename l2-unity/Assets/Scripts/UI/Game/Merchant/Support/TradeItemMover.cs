using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class TradeItemMover 
{
    private ShopInventoryController _shopCellCreator;
    private ShopItemEvaluator _itemEvalutor;
    private DealerWindow _dealerWindow;
    public TradeItemMover(DealerWindow dealerWindow , ShopInventoryController shopCellCreator , ShopItemEvaluator itemEvaluator)
    {
        _shopCellCreator = shopCellCreator;
        _itemEvalutor = itemEvaluator;
        _dealerWindow = dealerWindow;
    }
   public void MovePanelPriceBuy(int selectCount , Product source , bool _isModifySell , int position)
    {
        if (selectCount == source.Count)
        {
            MovingBuyWithRemoval(_isModifySell, source, position);
        }
        else if (selectCount < source.Count)
        {
            Product clone = source.Clone();
            source.SetCount(source.Count - selectCount);
            clone.SetCount(selectCount);

            MovingBuyWithCopy(true, clone, position);
        }
    }

    public void MovePanelPriceSell(int selectCount, Product source, bool _isModifySell, int position , List<Product> listServer)
    {
        if (selectCount == source.Count)
        {
           var _listSell = MovingSellWithRemoval(_isModifySell, _dealerWindow.GetListSell(), source, listServer, position);
           _dealerWindow.SetListSell(_listSell);
        }
        else if (selectCount < source.Count)
        {
            Product clone = source.Clone();
            source.SetCount(source.Count - selectCount);
            clone.SetCount(selectCount);

            MovingWithCopy(clone, listServer, position);
        }else if (source.GetConsumeCategory() != ConsumeCategory.Normal)
        {
            Product clone = source.Clone();
            clone.SetCount(selectCount);

            MovingWithCopy(clone, listServer, position);
        }
    }


    public void MovingBuyWithRemoval(bool isModifySell, Product source, int position)
    {
        if (isModifySell) ModifyBuy(source, _dealerWindow.GetListSell(), _dealerWindow.GetListBuy());

        ShiftBuy(_dealerWindow.GetTabSell(), position, _dealerWindow.GetInventorySlotsSell(), _dealerWindow.GetInventorySlotsBuy());

        var listSell = _shopCellCreator.RemoveProduct(_dealerWindow.GetListSell(), source, true);
        _dealerWindow.SetListSell(listSell);

        UpdatePriceAndWeight(listSell, _dealerWindow.GetCurrentDataWeight(), _dealerWindow.GetMaxWeight());
    }

    //If we receive a signal that modification of the SELL panel is allowed, then we transfer the product to another panel but do not touch our panel until the quantity is > 0
    private void MovingBuyWithCopy(bool isModifySell, Product source, int position)
    {

        if (isModifySell) ModifyBuy(source , _dealerWindow.GetListSell() , _dealerWindow.GetListBuy());
        if (!isModifySell) ShiftBuy(_dealerWindow.GetTabSell(), position, _dealerWindow.GetInventorySlotsSell(), _dealerWindow.GetInventorySlotsBuy());

        //_listSell = _shopCellCreator.RemoveProduct(_listSell, source, false);
        var listSell = _shopCellCreator.RemoveProduct(_dealerWindow.GetListSell(), source, false);
        _dealerWindow.SetListSell(listSell);

        UpdatePriceAndWeight(listSell , _dealerWindow.GetCurrentDataWeight() , _dealerWindow.GetMaxWeight());
    }


    public List<Product> MovingSellWithRemoval(bool isModifySell, List<Product> listSell, Product source, List<Product> listServer, int position)
    {
        listSell = _shopCellCreator.AddProduct(listSell, source);

        RefreshToolTips(listSell);

        if (listSell.Count != 0)
        {
            Move(_dealerWindow.GetInventorySlotsBuy(), _dealerWindow.GetTabBuy(), GetNewPosition(listSell, source), source);
        }

        ModifySell(isModifySell, position, listServer, source, true);

        UpdatePriceAndWeight(listSell, _dealerWindow.GetCurrentDataWeight(), _dealerWindow.GetMaxWeight());

        return listSell;
    }

    private void MovingWithCopy(Product source, List<Product> listServer, int position)
    {
        var listSell = _shopCellCreator.AddProduct(_dealerWindow.GetListSell(), source);
        _dealerWindow.SetListSell(listSell);
        RefreshToolTips(listSell);

        if (listSell.Count != 0)
        {
            Move(_dealerWindow.GetInventorySlotsBuy(), _dealerWindow.GetTabBuy(), GetNewPosition(listSell, source), source);
        }

        ModifySell(false, position, listServer, source, false);

        UpdatePriceAndWeight(listSell , _dealerWindow.GetCurrentDataWeight(), _dealerWindow.GetMaxWeight());
    }


    private void ModifySell(bool isModify, int position, List<Product> listServer, Product source, bool isRemoval)
    {
        if (isModify)
        {
            ShiftSell(_dealerWindow.GetTabBuy() ,  position, _dealerWindow.GetInventorySlotsSell(), _dealerWindow.GetInventorySlotsBuy());
            var _listBuy = _shopCellCreator.RemoveProduct(listServer, source, isRemoval);
            _dealerWindow.SetListBuy(_listBuy);
        }

    }

    private void ModifyBuy(Product source , List<Product> listSell ,  List<Product> listBuy)
    {
        if (listSell.Count != 0)
        {
            listBuy = _shopCellCreator.AddProduct(listBuy, source);
            Move(_dealerWindow.GetInventorySlotsSell(), _dealerWindow.GetTabSell(), GetNewPosition(listBuy, source), source);
        }
    }

    private void UpdatePriceAndWeight(List<Product> listSell , int _currentDataWeight, int _maxWeight)
    {
        _itemEvalutor.UpdateAllPrice(listSell);
        _itemEvalutor.UpdateWeight(listSell, _currentDataWeight, _maxWeight);
    }

    private void RefreshToolTips(List<Product> listSell)
    {
        ToolTipManager.GetInstance().SetBuyData(listSell);
    }

    public int GetNewPosition(List<Product> list, Product moveItem)
    {
        //example: stem/cord - count 6 pcs
        if (moveItem.GetTypeItem() == EnumType2.TYPE2_OTHER)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Product product = list[i];
                if (product.ItemId == moveItem.ItemId)
                {
                    return i;
                }
            }
        }
        return list.Count - 1;
    }

    private void Move(InventorySlot[] inventorySlots, InventoryTab tab, int newPosition, Product product)
    {
        inventorySlots[newPosition].AssignProduct(product);
        tab.UpdateInventorySlots(inventorySlots);
    }

    private void ShiftSell(InventoryTab tabBuy , int newPosition , InventorySlot[] inventorySlotsSell, InventorySlot[] inventorySlotsBuy)
    {
        ShiftElements.ShiftElementsLeft(inventorySlotsSell, newPosition);
        tabBuy.UpdateInventorySlots(inventorySlotsBuy);
    }
    //We do not move the target from sell -> buy because we believe that there is already something there and re-updating the cell is not required.We simply clear it
    //and shift all positions to the left if we delete an element
    public void ShiftBuy(InventoryTab tabSell, int newPosition, InventorySlot[] inventorySlotsSell, InventorySlot[] inventorySlotsBuy)
    {
        ShiftElements.ShiftElementsLeft(inventorySlotsBuy, newPosition);
        tabSell.UpdateInventorySlots(inventorySlotsSell);
    }

}
