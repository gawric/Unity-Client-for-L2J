using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;


public class SeedInfoWindow : L2PopupWindow
{
    private ICreator _creatorWindow;
    private ICreatorTables _creatorTable;
    private static SeedInfoWindow _instance;
    public static SeedInfoWindow Instance { get { return _instance; } }
    private VisualTreeAsset _tabTemplate;
    private VisualTreeAsset _tabHeaderTemplate;
    private VisualTreeAsset _buttonTemplate;


    private List<TableColumn> _defaultColumns0;
    private List<TableColumn> _defaultColumns1;
    private List<TableColumn> _defaultColumns2;
    private readonly string[] _tabsName = new string[] { "Seeds", "Fruit", "Bas.Info" };
    private VisualElement _footerContainer;


    private const string commandButton0 = "manor_menu_select?ask=2&state=-1&time=0";
    private const string commandButton1 = "manor_menu_select?ask=1&state=-1&time=0";

    private const string commandTab0 = "manor_menu_select?ask=3&state=1&time=0";
    private const string commandTab1 = "manor_menu_select?ask=4&state=1&time=0";
    private const string commandTab2 = "manor_menu_select?ask=5&state=1&time=0";
    private Button _buttonSeeds;
    private Button _buttonHarvst;
    private Button _buttonAll1;
    private Button _buttonAll2;

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Manor/SeedInfoWindow");
        _tabTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTab");
        _buttonTemplate = LoadAsset("Data/UI/_Elements/Template/Elements/Buttons/DefaultButton");
        _tabHeaderTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTabHeader");
    }


    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _creatorWindow =  new CreatorTabsWindows();
            _creatorTable = new CreatorTableWindows();
        }
        else
        {
            Destroy(this);
        }
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);
        var content = (VisualElement)GetElementById("content");

        _footerContainer = content.Q<VisualElement>("footerContent");
        _creatorTable.LoadAsset(LoadAsset);

        _defaultColumns0 = GetColumnWindowTab0();
        _defaultColumns1 = GetColumnWindowTab1();
        _defaultColumns2 = GetColumnWindowTab2();

        _creatorWindow.InitContentTabs(_tabsName);
        _creatorWindow.CreateTabs(content, _tabTemplate, _tabHeaderTemplate);
        _creatorWindow.InsertTablesIntoContent(_creatorTable, _defaultColumns0, true);
        _creatorWindow.EventSwitchTabByIndexOfTab += OnSwitchTab;
        AddContent0Footer(_creatorWindow , _footerContainer);



        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

    }

    private void UnRegisterAllButtons()
    {
        if(_buttonSeeds != null) _buttonSeeds.UnregisterCallback<ClickEvent>(OnClickButtonSeed);
        if (_buttonHarvst != null) _buttonSeeds.UnregisterCallback<ClickEvent>(OnClickButtonSeed);
        if (_buttonAll1 != null) _buttonSeeds.UnregisterCallback<ClickEvent>(OnClickButtonSeed);
        if (_buttonAll2 != null) _buttonSeeds.UnregisterCallback<ClickEvent>(OnClickButtonSeed);
    }
    private void AddContent0Footer(ICreator creatorWindow , VisualElement footerContainer)
    {
        if (footerContainer != null)
        {
            if(_buttonSeeds == null)
            {
                _buttonSeeds = (Button)ToolTipsUtils.CloneOne(_buttonTemplate);
                _buttonSeeds.Q<Label>("ButtonLabel").text = "Buy seeds";
            }


            UnRegisterAllButtons();

            _buttonSeeds.RegisterCallback<ClickEvent>(OnClickButtonSeed);

            creatorWindow.InsertFooterIntoContent(_buttonSeeds, footerContainer);
        }
    }

    private void AddContent1Footer(ICreator creatorWindow, VisualElement footerContainer)
    {
        if (footerContainer != null)
        {
            if(_buttonHarvst == null)
            {
                _buttonHarvst = (Button)ToolTipsUtils.CloneOne(_buttonTemplate);
                _buttonHarvst.Q<Label>("ButtonLabel").text = "Sale of harvest";
            }
            UnRegisterAllButtons();

            _buttonHarvst.RegisterCallback<ClickEvent>(OnClickButtonHarvest);

            creatorWindow.InsertFooterIntoContent(_buttonHarvst, footerContainer);
        }
    }

    private void AddContent2Footer(ICreator creatorWindow, VisualElement footerContainer)
    {
        if (footerContainer != null)
        {
            if(_buttonAll1 == null)
            {
                _buttonAll1 = (Button)ToolTipsUtils.CloneOne(_buttonTemplate);
                _buttonAll1.Q<Label>("ButtonLabel").text = "Buy seeds";
            }



            if (_buttonAll2 == null)
            {
                _buttonAll2 = (Button)ToolTipsUtils.CloneOne(_buttonTemplate);
                _buttonAll2.Q<Label>("ButtonLabel").text = "Sale of harvest";
            }

            UnRegisterAllButtons();

            // Create a container for the buttons
            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;

            buttonContainer.Add(_buttonAll1);
            buttonContainer.Add(_buttonAll2);


            _buttonAll1.RegisterCallback<ClickEvent>(OnClickAllButton1);
            _buttonAll2.RegisterCallback<ClickEvent>(OnClickAllButton2);

            creatorWindow.InsertFooterIntoContent(buttonContainer, footerContainer);
        }
    }


    private List<TableColumn> GetColumnWindowTab0()
    {

        var harvest = new TableColumn(false, "Name Of The Seed", 0, new List<string>() {  }, 13 , 200);
        var auth = new TableColumn(true, "In Stock", 0, new List<string>() {  }, 0);
        var buy = new TableColumn(true, "For Sale", 0, new List<string>() { }, 0);
        var price = new TableColumn(true, "Price", 0, new List<string>() {  }, 0);

        return new List<TableColumn> { harvest, auth, buy, price};
    }

    private List<TableColumn> GetColumnWindowTab1()
    {

        var harvest = new TableColumn(false, "Name Of Fruits", 0, new List<string>() {  }, 13, 200);
        var auth = new TableColumn(true, "In Stock", 0, new List<string>() {  }, 0);
        var buy = new TableColumn(true, "For Sale", 0, new List<string>() {  }, 0);
        var price = new TableColumn(true, "Price", 0, new List<string>() {  }, 0);
        var reward = new TableColumn(true, "Reward", 0, new List<string>() {  }, 0);

        return new List<TableColumn> { harvest, auth, buy, price , reward };
    }

    private List<TableColumn> GetColumnWindowTab2()
    {

        var harvest = new TableColumn(false, "Name Of Fruits", 0, new List<string>() { }, 13, 200);
        var auth = new TableColumn(true, "Lvl", 0, new List<string>() {  }, 0);
        var buy = new TableColumn(true, "Price Seed", 0, new List<string>() {  }, 0);
        var price = new TableColumn(true, "Price Fruits", 0, new List<string>() {  }, 0);
        var reward1 = new TableColumn(true, "Reward1", 0, new List<string>() {  }, 0);
        var reward2 = new TableColumn(true, "Reward2", 0, new List<string>() {  }, 0);

        return new List<TableColumn> { harvest, auth, buy, price , reward1, reward2 };
    }

    public void SetDataSeedInfo(List<SeedProduction> listSeedProduction)
    {
        List<string> harvest = new List<string>();
        List<string> auth = new List<string>();
        List<string> buy = new List<string>();
        List<string> price = new List<string>();


        foreach (var seedProduction in listSeedProduction)
        {
            harvest.Add(""+seedProduction.GetSeedId());
            auth.Add("" + seedProduction.GetAmount());
            buy.Add("" + seedProduction.GetStartAmount());
            price.Add("" + seedProduction.GetPrice());
        }

        _defaultColumns0[0].SetData(harvest);
        _defaultColumns0[1].SetData(auth);
        _defaultColumns0[2].SetData(buy);
        _defaultColumns0[3].SetData(price);

        _creatorTable.UpdateTableData(_defaultColumns0);

    }

    public void SetDataCropInfo(List<CropProcure> listCropInfo)
    {
        List<string> harvest = new List<string>();
        List<string> auth = new List<string>();
        List<string> buy = new List<string>();
        List<string> price = new List<string>();
        List<string> reward = new List<string>();

        foreach (var seedProduction in listCropInfo)
        {
            harvest.Add("" + seedProduction.GetSeedId());
            auth.Add("" + seedProduction.GetAmount());
            buy.Add("" + seedProduction.GetStartAmount());
            price.Add("" + seedProduction.GetPrice());
            reward.Add("" + seedProduction.GetReward());
        }

        _defaultColumns1[0].SetData(harvest);
        _defaultColumns1[1].SetData(auth);
        _defaultColumns1[2].SetData(buy);
        _defaultColumns1[3].SetData(price);
        _defaultColumns1[4].SetData(reward);

        _creatorTable.UpdateTableData(_defaultColumns1);

    }

    public void SetDataDefaultManorInfo(List<Seed> listSeed)
    {
        List<string> harvest = new List<string>();
        List<string> auth = new List<string>();
        List<string> buy = new List<string>();
        List<string> price = new List<string>();
        List<string> reward1 = new List<string>();
        List<string> reward2 = new List<string>();

        foreach (var seed in listSeed)
        {
            ItemName name = ItemNameTable.Instance.GetItemName(seed.CropId);

            if(name != null)
            {
                ItemName reward1name = ItemNameTable.Instance.GetItemName(seed.Reward1ItemId);
                ItemName reward2name = ItemNameTable.Instance.GetItemName(seed.Reward2ItemId);
                harvest.Add("" + name.Name);
                auth.Add("" + seed.SeedLevel);
                buy.Add("" + seed.SeedReferencePrice);
                price.Add("" + seed.CropReferencePrice);
                reward1.Add("" + reward1name.Name);
                reward2.Add("" + reward2name.Name);
            }
        }

        _defaultColumns2[0].SetData(harvest);
        _defaultColumns2[1].SetData(auth);
        _defaultColumns2[2].SetData(buy);
        _defaultColumns2[3].SetData(price);
        _defaultColumns2[4].SetData(reward1);
        _defaultColumns2[5].SetData(reward2);

        _creatorTable.UpdateTableData(_defaultColumns2);
    }

    public void OnSwitchTab(int indexTab)
    {
      
        if (!ArrayUtils.IsValidIndexArray(_tabsName, indexTab))
        {
            return;
        }

        switch (indexTab)
        {
            case 0:
                Debug.Log("Event index 0 ");
                HtmlWindow.Instance.UseActionCommand(commandTab0);
                _creatorTable.SetSelectRow(-1);
                _creatorWindow.RefreshDataColumns(_defaultColumns0);
                AddContent0Footer(_creatorWindow, _footerContainer);
                break;
            case 1:
                Debug.Log("Event index 1 ");
                HtmlWindow.Instance.UseActionCommand(commandTab1);
                _creatorTable.SetSelectRow(-1);
                _creatorWindow.RefreshDataColumns(_defaultColumns1);
                AddContent1Footer(_creatorWindow, _footerContainer);
                break;
            case 2:
                Debug.Log("Event index 2 ");
                HtmlWindow.Instance.UseActionCommand(commandTab2);
                _creatorTable.SetSelectRow(-1);
                _creatorWindow.RefreshDataColumns(_defaultColumns2);
                AddContent2Footer(_creatorWindow, _footerContainer);
                break;
        }
    }


    public void OnClickButtonSeed(ClickEvent evt)
    {
        Debug.Log("OnClickButtonSeed 1");
    }

    public void OnClickButtonHarvest(ClickEvent evt)
    {
        Debug.Log("OnClickButtonHarvest 1");
    }

    public void OnClickAllButton1(ClickEvent evt)
    {

        HtmlWindow.Instance.UseActionCommand(commandButton1);
    }

    public void OnClickAllButton2(ClickEvent evt)
    {
        HtmlWindow.Instance.UseActionCommand(commandButton0);
    }

    public void ShowWindowActiveTabSeed()
    {
        _creatorWindow.SwitchTab(0 , false , true);
        AddContent0Footer(_creatorWindow, _footerContainer);
        base.ShowWindow();
    }

    public void ShowWindowActiveTabAllDefault()
    {
        _creatorWindow.SwitchTab(2, false, true);
        AddContent2Footer(_creatorWindow, _footerContainer);
        base.ShowWindow();
    }

}
