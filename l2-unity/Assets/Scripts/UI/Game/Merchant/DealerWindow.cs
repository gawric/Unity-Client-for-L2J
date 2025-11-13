using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;
using Debug = UnityEngine.Debug;


public class DealerWindow : L2PopupWindow
{


    private static DealerWindow _instance;
    private VisualElement _contentBuy;
    private VisualElement _contentSell;
    private VisualTreeAsset _inventorySlotTemplate;
    private int _defaultCount = 24;
    private Label _adena;
    private Label _price;
    private Label _currentWeight;
    private List<Product> _listBuy;
    private List<Product> _listSell;
    private InventoryTab _tabSell;
    private InventoryTab _tabBuy;
    private InventorySlot[] _inventorySlotsBuy;
    private InventorySlot[] _inventorySlotsSell;
    private VisualElement _weiBar;
    private VisualElement _weiBarBg;
    private int _currentDataWeight = 0;
    private int _maxWeight = 0;
    private ProductType _productType;

    private bool _isModifySell = false;

    public VisualTreeAsset InventorySlotTemplate { get { return _inventorySlotTemplate; } }
    private ShopItemEvaluator _itemEvalutor;
    private ShopInventoryController _shopCellCreator;
    private TradeItemMover _tradeItemMover;

    private Label _panelSellHeaderName;
    private Label _panelBuyHeaderName;
    private int _listId;
    private Label _windowName;
    public static DealerWindow Instance
    {
        get { return _instance; }
    }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _itemEvalutor = new ShopItemEvaluator(_instance);
            _shopCellCreator = new ShopInventoryController(_instance , _defaultCount);
            _tradeItemMover = new TradeItemMover(this , _shopCellCreator, _itemEvalutor);
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        _instance = null;
        _itemEvalutor = null;
        _inventorySlotsBuy = null;
        _inventorySlotsSell = null;
        _tradeItemMover = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Merchant/DealerWindow");
        _inventorySlotTemplate = LoadAsset("Data/UI/_Elements/Template/Slot");
    }

    protected override void InitWindow(VisualElement root)
    {
        base.InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        _contentBuy = GetElementByClass("content-buy");
        _contentSell = GetElementByClass("content-sell");

        _price = (Label)GetElementById("PriceCount");
        _adena = (Label) GetElementById("AdenaCount");
        _currentWeight = (Label)GetElementById("CurrentWeight");

        _panelSellHeaderName = (Label)GetElementById("SellH1");
        _panelBuyHeaderName = (Label)GetElementById("BuyH1");

        var succesBtn = (Button)GetElementById("SuccessButton");
        succesBtn.RegisterCallback<ClickEvent>((evt) => HandleSuccessButtonClick());
        RegisterCloseWindowEventByName("CloseButton");
        _panelSellHeaderName.text = "Sell";
        _panelSellHeaderName.text = "Buy";

        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        _weiBar = GetElementById("WeightBg");
        _weiBarBg = GetElementById("WeightGauge");
        OnCenterScreen(root);
        CreateInitBuy();
        _windowName = (Label)GetElementById("windows-name-label");
        _productType = ProductType.NONE;
    }


   public void UpdateBuyData(List<Product>listBuy ,bool  isModifySell , int listId)
   {
        _listId = listId;
        _listBuy = listBuy;
        _isModifySell = isModifySell;
        if(_listSell != null) _listSell.Clear();

        //UnityEngine.Debug.Log("Server size list buy " + listBuy.Count);

        _shopCellCreator.ClearSell(_tabSell);
        _shopCellCreator.CreateSell(listBuy, _tabSell, _contentSell , _windowEle);
        _shopCellCreator.ClearBuy(_inventorySlotsBuy, _tabBuy);
        _shopCellCreator.CreateBuy(listBuy, _tabBuy, _contentBuy, _windowEle);
        _itemEvalutor.UpdateAllPrice(_listSell);
   }

   public void SetHeaderNameSellPanel(string headerName)
   {
        _panelSellHeaderName.text = headerName;
   }

    public void SetWindowName(string windowName)
    {
        _windowName.text = windowName;
    }

    public void SetHeaderNameBuyPanel(string headerName)
    {
        _panelBuyHeaderName.text = headerName;
    }

    public void SetProductType(ProductType type)
    {
        _productType = type;
    }

    public void EventDoubleClick(VisualElement slotElement)
    {
        string[] ids = ToolTipsUtils.GetUniquePosition(slotElement);
        int position = Int32.Parse(ids[0]);
        int type = Int32.Parse(ids[1]);
        SlotType slot = ToolTipsUtils.DetectedClickPanel(type);
        Move—ellElsePriceType(slot , _listBuy , position);
        RefreshToolTips(slotElement , slot);
    }

    public void RefreshToolTips(VisualElement slotElement , SlotType slot)
    {
        if(SlotType.PriceBuy == slot)
        {
            ToolTipManager.GetInstance().EventLeftClickSlot(slotElement);
        }
        
    }
   


    private List<Product> _listServer;
    private int _position;
    private SlotType _type;
  
    private void Move—ellElsePriceType(SlotType type , List<Product> listServer, int position)
    {

        if (type == SlotType.PriceBuy)
        {

            if (_listSell == null) return;
            if (!IsIndexValid(_listSell, position)) return;

            Product source = _listSell[position];

            if (source.Count > 1 | source.GetConsumeCategory() != ConsumeCategory.Normal)
            {
                RefreshOpacity(0.7f);
                RefreshSelectData(listServer, position, type);
                QuantityInput.Instance.ShowWindow(source.Count);
                QuantityInput.Instance.OnButtonOk += OnSelectedQuantity;
                QuantityInput.Instance.OnButtonClose += OnCloseQuantity;
            }
            else
            {
                source.SetCount(1);
                _tradeItemMover.MovingBuyWithRemoval(_isModifySell  ,source, position);
            }


        }
        else if (type == SlotType.PriceSell)
        {
            if (!IsIndexValid(listServer, position)) return;
            Product source = listServer[position];

            if (source == null) return;

            if (source.Count > 1 | source.GetConsumeCategory() != ConsumeCategory.Normal)
            {
                RefreshOpacity(0.7f);
                RefreshSelectData(listServer, position, type);
                QuantityInput.Instance.ShowWindow(source.Count);
                QuantityInput.Instance.OnButtonOk += OnSelectedQuantity;
                QuantityInput.Instance.OnButtonClose += OnCloseQuantity;
            }
            else
            {
                source.SetCount(1);
                _listSell = _tradeItemMover.MovingSellWithRemoval(_isModifySell , _listSell , source, listServer, position);
            }
        }
    }


    private void RefreshSelectData(List<Product> listServer , int position , SlotType type)
    {
        _listServer = listServer;
        _position = position;
        _type = type;
    }

    private void OnSelectedQuantity(string value)
    {
        QuantityInput.Instance.OnButtonOk -= OnSelectedQuantity;
        UseSuccessInParentWindow(this, value, _listServer);
    }

    private void OnCloseQuantity()
    {
        QuantityInput.Instance.OnButtonClose -= OnCloseQuantity;
        UseCloseInParentWindow();
    }

    private void UseCloseInParentWindow()
    {
       RefreshOpacity(1);
    }

    private void UseSuccessInParentWindow(DealerWindow _dealerWindow, string value, List<Product> listServer)
    {
         _dealerWindow.RefreshOpacity(1);
        Debug.Log("Move value 1 " + value);
         _dealerWindow.Move—ellElseQuantitySelected(_type, listServer, _position, int.Parse(value));
    }

    public void Move—ellElseQuantitySelected(SlotType type, List<Product> listServer, int position , int selectCount)
    {

        if (type == SlotType.PriceBuy)
        {
            if (_listSell == null) return;
            if (!IsIndexValid(_listSell, position)) return;

            Product source = _listSell[position];

            _tradeItemMover.MovePanelPriceBuy(selectCount, source, _isModifySell, position);

        }
        else if (type == SlotType.PriceSell)
        {
            if (!IsIndexValid(listServer, position)) return;
            Product source = listServer[position];
            if (source == null) return;

            _tradeItemMover.MovePanelPriceSell(selectCount, source, _isModifySell, position, listServer);

        }
    }





  

    public bool IsIndexValid(List<Product> _listSell, int index)
    {
        return index >= 0 && index < _listSell.Count;
    }

 


    
 

   

    //_weiBar = GetElementById("WeightBg");
    //_weiBarBg = GetElementById("WeightGauge");

    public VisualElement GetWeiBar()
    {
        return _weiBar;
    }

    public VisualElement GetWeiBarBg()
    {
        return _weiBarBg;
    }

    public void UpdatePrice(int allPrice)
    {
        _price.text = ToolTipsUtils.ConvertToPrice(allPrice);
    }
    
    public void UpdateDataWeight(int data)
    {
        _currentDataWeight = data;
    }

    public int GetCurrentDataWeight()
    {
        return _currentDataWeight;
    }

    public int GetMaxWeight()
    {
        return _maxWeight;
    }

    public void UpdateAdena(int adena)
    {
        _adena.text = ToolTipsUtils.ConvertToPrice(adena);
    }

    public void UpdateWeight(double weightPercent)
    {
        _currentWeight.text = weightPercent.ToString() + "%";
    }

    public void UpdateAllWeight(int currentWeight, int maxWeight)
    {
        _itemEvalutor.UpdateWeightBar(this , currentWeight, maxWeight);
    }

    public void UpdateDataMaxWeight(int data)
    {
        _maxWeight = data;
    }

    public InventorySlot[] GetInventorySlotsSell()
    {
        return _inventorySlotsSell;
    }

    public InventorySlot[] GetInventorySlotsBuy()
    {
        return _inventorySlotsBuy;
    }

    public void SetInventorySlotsSell(InventorySlot[] inventorySlotSell)
    {
        _inventorySlotsSell = inventorySlotSell;
    }

    public void SetInventorySlotsBuy(InventorySlot[] inventorySlotBuy)
    {
        _inventorySlotsBuy = inventorySlotBuy;
    }

    public VisualTreeAsset GetInventorySlotTemplate()
    {
        return _inventorySlotTemplate;
    }

    public VisualElement GetContentSell()
    {
        return _contentSell;
    }

    public void SetTabSell(InventoryTab tabSell)
    {
        _tabSell = tabSell;
    }

    public void SetTabBuy(InventoryTab tabBuy)
    {
        _tabBuy = tabBuy;
    }

    public InventoryTab GetTabBuy()
    {
        return _tabBuy;
    }

    public InventoryTab GetTabSell()
    {
        return _tabSell;
    }

    public List<Product> GetListSell()
    {
        return _listSell;
    }

    public List<Product> GetListBuy()
    {
        return _listBuy;
    }

    public void SetListBuy(List<Product> listBuy)
    {
        _listBuy = listBuy;
    }

    public void SetListSell(List<Product> listSell)
    {
        _listSell = listSell;
    }

    public void UpdateDataForm(int adena, double weightPercent, int currentWeight, int maxWeight)
    {
        _itemEvalutor.UpdateDataForm(adena, weightPercent, currentWeight, maxWeight);
    }

 

    private void CreateInitBuy()
    {
        _tabBuy = new InventoryTab();
        _contentBuy.Clear();
        _tabBuy.Initialize(_windowEle, _contentBuy, _contentBuy , false , false);
        _inventorySlotsBuy = _shopCellCreator.CreateEmptyBuy(_tabBuy, _defaultCount, _contentBuy, GetInventorySlotTemplate());
    }

  
    private void HandleSuccessButtonClick()
    {
        HideWindow();

        if (!_isModifySell)
        {
            switch (_productType)
            {
                case ProductType.BUY:
                    var packetBuy = CreatorPacketsUser.CreateRequestBuyItem(_listId, _listSell);
                    SendServer(packetBuy);
                    break;

                case ProductType.WEAR:
                    SystemMessageWindow.Instance.OnButtonOk += OkWear;
                    SystemMessageWindow.Instance.OnButtonClosed += On—ancel;
                    SystemMessageWindow.Instance.ShowWindowDialogYesOrNot("The try-on state lasts only 5 seconds. When the character's state changes, it can be canceled.");
                    break;
                case ProductType.BUY_SEED:
                    Debug.LogWarning("ÕÂ Â‡ÎËÁÓ‚‡Ì Ô‡ÍÂÚ ÓÚÔ‡‚Í‡ BUY_SEED ");
                    break;
            }
        }
        else
        {
            switch (_productType)
            {
                case ProductType.SELL:
                    var packetSell = CreatorPacketsUser.CreateRequestSellItem(_listId, _listSell);
                    SendServer(packetSell);
                    break;
                case ProductType.WHDepositList:
                    var packetWhDp = CreatorPacketsUser.CreateWHDepositList(_listSell);
                    SendServer(packetWhDp);
                    break;
                case ProductType.WHWithdrawList:
                    var packetWhWd = CreatorPacketsUser.CreateWHWithdrawList(_listSell);
                    SendServer(packetWhWd);
                    break;
                case ProductType.PackageSendableList:
                    int playerObjectId = _listId;
                    var packetSendable = CreatorPacketsUser.RequestPackageSend(playerObjectId , _listSell);
                    SendServer(packetSendable);
                    break;
                case ProductType.BUY_SEED:
                    Debug.LogWarning("ÕÂ Â‡ÎËÁÓ‚‡Ì Ô‡ÍÂÚ ÓÚÔ‡‚Í‡ BUY_SEED ");
                    break;
            }
        }

    }

    private void SendServer(ClientPacket packet)
    {
        SendGameDataQueue.Instance().AddItem(packet, GameClient.Instance.IsCryptEnabled(), GameClient.Instance.IsCryptEnabled());
    }

    private void OkWear()
    {
        var packet = CreatorPacketsUser.CreateRequestPreviewList(_listId, _listSell);
        SendServer(packet);
        CancelEvent();
    }

    private void On—ancel()
    {
        _listSell.Clear();
        _listId = 0;
        CancelEvent();
    }

    private void CancelEvent()
    {
        SystemMessageWindow.Instance.OnButtonOk -= OkWear;
        SystemMessageWindow.Instance.OnButtonClosed -= On—ancel;
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");

    }


}
