
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MultiSellWindow : L2PopupWindow
{
    private static MultiSellWindow _instance;
    private ICreatorTradeTab _creatorWindow;

    private ScrollView _scrollView1;
    private ScrollView _scrollView2;
    private VisualElement _contentPanel1;
    private VisualElement _contentPanel2;

    private VisualTreeAsset _windowTemplateWeapon;
    private VisualTreeAsset _windowTemplateAcccesories;
    private VisualTreeAsset _windowTemplateArmor;
    private VisualTreeAsset _itemTemplateIngredient;
    private MultiSellList _listMultisell;
    private TooltipDataProvider _dataProvider;
    private MultiSellToolTips _toolTips;
    private TextField _userInput;
    private int _listId;
    private int _entryId = -1;
    // private CreateScroller _createScroller;
    private VisualElement _selectContainer;
    public static MultiSellWindow Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _creatorWindow = new CreatorTradingWindowsWithTabs();
            _dataProvider = new TooltipDataProvider();
            _toolTips = new MultiSellToolTips(_dataProvider);

        }
        else
        {
            Destroy(this);
        }
    }


    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Merchant/MultiSellWindow");
        //_tabTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTab");
        //_tabHeaderTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTabHeader");

        _windowTemplateWeapon = LoadAsset("Data/UI/_Elements/Game/ToolTips/ToolTipWeapon");
        _windowTemplateAcccesories = LoadAsset("Data/UI/_Elements/Game/ToolTips/ToolTipAccessories");
        _windowTemplateArmor = LoadAsset("Data/UI/_Elements/Game/ToolTips/ToolTipArmor");
        _itemTemplateIngredient = LoadAsset("Data/UI/_Elements/Template/Merchant/ItemsIngredient");

        _creatorWindow.LoadAsset(LoadAsset);
    }


    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        _userInput = (TextField)GetElementById("UserInputField");
         var deleteButton = (Button)GetElementById("DeleteButton");
         var exchangeButton = (Button)GetElementById("ExchangeButton");
        //_inventoryTabView = GetElementById("InventoryTabView");

        _creatorWindow.InitTradeTabs(new string[] { "ALL" , "Other" });
        _creatorWindow.CreateTradeTabs(GetElementById("InventoryTabView"));

        _scrollView1 = (ScrollView)GetElementById("1Panel-ScrollView");
        _scrollView2 = (ScrollView)GetElementById("2Panel-ScrollView");
        

             //_createScroller.RegisterPlayerScrollEvent(_scrollView1, _scrollView1.verticalScroller);
        _contentPanel1 = _scrollView1.contentContainer;
        _contentPanel2 = _scrollView2.contentContainer;

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);
        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        RegisterCloseWindowEventByName("CloseButton");

        _userInput.value = "1";
        _userInput.RegisterValueChangedCallback(evt => OnValueChanged(evt));
        deleteButton.RegisterCallback<ClickEvent>((evt) => OnClick(evt));
        exchangeButton.RegisterCallback<ClickEvent>((evt) => OnSendServer(evt));

        OnCenterScreen(_root);

        _creatorWindow.EventLeftClick += OnEventLeftClick;
        _toolTips.SetTemplate(_windowTemplateWeapon, _windowTemplateAcccesories, _windowTemplateArmor, _itemTemplateIngredient);

        yield return new WaitForEndOfFrame();
    }


    public void AddData(List<ItemInstance> allItems , MultiSellList listMultisell)
    {
        if(allItems.Count > 0 && listMultisell != null)
        {
            _listMultisell = listMultisell;
            _listId = listMultisell.GetListId();
            _creatorWindow.AddData(allItems);
        }

        _creatorWindow.SetClickActiveTab(0);
        _entryId = 1;
    }

    public void ShowWindow()
    {
        _userInput.value = "1";
        base.ShowWindow();
    }

    public void OnEventLeftClick(int itemId , ItemCategory category, int position)
    {
        ItemInstance itemInstance = new ItemInstance(0, itemId, ItemLocation.Trade, 0, 1, category, false, ItemSlot.none, 0, 999);
        itemInstance.SetMultiSell(true);
        
        _selectContainer = _toolTips.GetContainer(itemInstance);

        if(_selectContainer != null)
        {
            _contentPanel1.Clear();
            _toolTips.UseItem(itemInstance, _selectContainer);

            _contentPanel1.Add(_selectContainer);
            _selectContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);


            _contentPanel2.Clear();

            List<MultiSellData> data = _listMultisell.GetMultiSell();
            List<Ingredient> ingredients = null;
            ingredients = GetIngredient(data  , itemId, itemInstance, position);

            if (ingredients != null)
            {
                _toolTips.AddIngredient(ingredients, _contentPanel2);
            }

        }
        else
        {
            _contentPanel1.Clear();
            _contentPanel2.Clear();
            _entryId = -1;
        }
    }

    public ItemInstance GetItemByPosition(int position)
    {
        return _creatorWindow.GetActiveByPosition(position);
    }


    private List<Ingredient> GetIngredient(List<MultiSellData> data , int itemId , ItemInstance itemInstance , int position)
    {
        if (!IfMore1Item(data, itemId))
        {
            return  GetIngredient(data, itemInstance.ItemId);
        }
        else
        {
            return  GetIngredientByIndex(data, position);
        }
    }

    public void OnGeometryChanged(GeometryChangedEvent evt)
    {
        _contentPanel1.style.height = evt.newRect.height;
        _selectContainer.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
    }

    private List<Ingredient> GetIngredient(List<MultiSellData> data , int itemId)
    {
        int index = 1;
        foreach (MultiSellData item in data)
        {
            List<ItemInstance> listItem = item.ProductList;
            bool result =  listItem.Any(x => x.ItemId == itemId);
            if (result)
            {
                _entryId = index;
                return item.IngredientList;
            }
            index++;
        }

        

        return null;
    }

    public bool IfMore1Item(List<MultiSellData> data, int itemId)
    {
        List<MultiSellData>  list = data.Where(x => x.ProductList[0].ItemId == itemId).ToList();

        if(list.Count > 1)
        {
            return true;
        }
        return false;
    }

    private List<Ingredient> GetIngredientByIndex(List<MultiSellData> data, int index)
    {
        if(ArrayUtils.IsValidIndexList(data, index))
        {
            _entryId = index;
            return data[index].IngredientList;
        }
        return null;
    }

  

    private void OnValueChanged(ChangeEvent<string> evt)
    {

        _userInput.UnregisterValueChangedCallback(OnValueChanged);

        if (evt.newValue.Length == 0 & evt.previousValue.Length >= 1)
        {
            _userInput.SetValueWithoutNotify("0");
            _userInput.RegisterValueChangedCallback(OnValueChanged);
            return;
        }


        string digitsOnly = ToolTipsUtils.ConvertPriceToNormal(evt.newValue);
        if (digitsOnly.Length > 20) return;
        if (string.IsNullOrEmpty(digitsOnly) || !long.TryParse(digitsOnly, out long newValue))
        {
            _userInput.SetValueWithoutNotify(evt.previousValue);
            _userInput.RegisterValueChangedCallback(OnValueChanged);
            return;
        }

        digitsOnly = digitsOnly.Trim();

        string formattedValue = ToolTipsUtils.ConvertToPrice(long.Parse(digitsOnly));

        _userInput.SetValueWithoutNotify(formattedValue);

        if (formattedValue.Length > 1) SetCursorToEnd(formattedValue);
        _userInput.RegisterValueChangedCallback(OnValueChanged);
    }

    private void SetCursorToEnd(string formattedValue)
    {
        _userInput.cursorIndex = formattedValue.Length + 1;
    }

    private void OnClick(ClickEvent evt)
    {
        _userInput.value = "1";
    }

    private void OnSendServer(ClickEvent evt)
    {

        SystemMessageWindow.Instance.OnButtonOk += OkExchange;
        SystemMessageWindow.Instance.OnButtonClosed += OnCancel;
        SystemMessageWindow.Instance.ShowWindowDialogYesOrNot("Are you sure you want to exchange?");
    }

    private void OkExchange()
    {
        string value = ToolTipsUtils.ConvertPriceToNormal(_userInput.value);
        var sendPaket = CreatorPacketsUser.CreateMultiSellChoose(_listId, _entryId, int.Parse(value));
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
        CancelEvent();
    }

    public void OnCancel()
    {
        CancelEvent();
    }
    private void CancelEvent()
    {
        SystemMessageWindow.Instance.OnButtonOk -= OkExchange;
        SystemMessageWindow.Instance.OnButtonClosed -= OnCancel;
    }

    private void OnDestroy()
    {
        _instance = null;
    }


}
