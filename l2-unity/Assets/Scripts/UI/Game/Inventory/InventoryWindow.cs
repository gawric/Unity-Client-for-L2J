using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryWindow : L2PopupWindow
{
    public static int PLAYER_INVENTORY_SIZE = 81;

    private VisualTreeAsset _tabTemplate;
    private VisualTreeAsset _tabHeaderTemplate;
    private VisualTreeAsset _inventorySlotTemplate;
    private VisualTreeAsset _minimizedTemplate;
    private VisualElement _inventoryTabView;
    private VisualElement _minimizedInventoryBtn;
    private InventoryTab _activeTab;


    private MouseOverDetectionManipulator _minimizedInventoryMouseOverManipulator;
    private DragManipulator _minimizedInventoryDragManipulator;
    [SerializeField] private bool _expanded = false;
    private VisualElement _expandButton;

    [SerializeField] private InventoryGearTab _gearTab;
    [SerializeField] private List<InventoryTab> _tabs;

    public List<ItemInstance> _playerItems;

    [SerializeField] private int _usedSlots;
    [SerializeField] private int _slotCount;
    [SerializeField] private int _currentWeight;
    [SerializeField] private int _maximumWeight;
    [SerializeField] private int _adenaCount;

    private Label _inventoryCountLabel;
    private Label _weightLabel;
    private Label _adenaCountLabel;

    private VisualElement _weiBar;
    private VisualElement _weiBarBg;
    protected bool _isWindowHiddenMain = true;
    public VisualTreeAsset InventorySlotTemplate { get { return _inventorySlotTemplate; } }
    public bool Expanded { get { return _expanded; } }
    public int UsedSlots { get { return _usedSlots; } }
    public int SlotCount { get { return _slotCount; } }

    private static InventoryWindow _instance;
    public static InventoryWindow Instance
    {
        get { return _instance; }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }

      
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryWindow");
        _tabTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTab");
        _tabHeaderTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTabHeader");
        _inventorySlotTemplate = LoadAsset("Data/UI/_Elements/Template/Slot");
        _minimizedTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryMin");
    }

    protected override void InitWindow(VisualElement root)
    {
        base.InitWindow(root);

        _expanded = false;

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);

        _expandButton = (Button)GetElementByClass("expand-btn");
        _expandButton.AddManipulator(new ButtonClickSoundManipulator(_expandButton));
        _expandButton.RegisterCallback<MouseDownEvent>(OnExpandButtonPressed, TrickleDown.TrickleDown);

        Button adenaDistribution = (Button)GetElementById("AdenaDistribBtn");
        adenaDistribution.AddManipulator(new ButtonClickSoundManipulator(adenaDistribution));
        adenaDistribution.AddManipulator(new TooltipManipulator(adenaDistribution, "Adena distribution"));

        Button compoundBtn = (Button)GetElementById("CompoundBtn");
        compoundBtn.AddManipulator(new ButtonClickSoundManipulator(compoundBtn));
        compoundBtn.AddManipulator(new TooltipManipulator(adenaDistribution, "Compound"));

        Button trashBtn = (Button)GetElementById("TrashBtn");
        trashBtn.AddManipulator(new ButtonClickSoundManipulator(trashBtn));
        trashBtn.AddManipulator(new TooltipManipulator(adenaDistribution, "Trash"));
        L2Slot trashSlot = new L2Slot(trashBtn, 100 , L2Slot.SlotType.Trash);

        _inventoryCountLabel = GetLabelById("InventoryCount");

        _weiBar= GetElementById("WeightBg");
        _weiBarBg = GetElementById("WeightGauge");

        _adenaCountLabel = GetLabelById("AdenaCount");
        _weightLabel = GetLabelById("CurrentWeight");
    }

    protected void RegisterCloseWindowEvent(string closeButtonClass)
    {
        Button closeButton = (Button)GetElementByClass(closeButtonClass);
        if (closeButton == null)
        {
            Debug.LogWarning($"Cant find close button with className: {closeButtonClass}.");
            return;
        }

        ButtonClickSoundManipulator buttonClickSoundManipulator = new ButtonClickSoundManipulator(closeButton);
        closeButton.AddManipulator(buttonClickSoundManipulator);

        closeButton.RegisterCallback<MouseUpEvent>(evt =>
        {
            AudioManager.Instance.PlayUISound("window_close");
            HideWindow();
        });
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        CreateTabs();

        CreateMinimizedWindow();

        yield return new WaitForEndOfFrame();

       // UpdateItemList(_playerItems);
    }

    private void OnExpandButtonPressed(MouseDownEvent evt)
    {
        if (!_expanded)
        {
            if (!_windowEle.ClassListContains("expanded"))
            {
                _windowEle.AddToClassList("expanded");
            }
            if (!_expandButton.ClassListContains("expanded"))
            {
                _expandButton.AddToClassList("expanded");
            }
            _expanded = true;

            //UpdateItemList(_playerItems);
        }
        else
        {
            if (_windowEle.ClassListContains("expanded"))
            {
                _windowEle.RemoveFromClassList("expanded");
            }
            if (_expandButton.ClassListContains("expanded"))
            {
                _expandButton.RemoveFromClassList("expanded");
            }
            _expanded = false;

           //UpdateItemList(_playerItems);
        }

        evt.PreventDefault();
    }

    private void CreateMinimizedWindow()
    {

        // Header button
        Button minimizeWindowButton = (Button)GetElementByClass("minimize-btn");
        minimizeWindowButton.AddManipulator(new ButtonClickSoundManipulator(minimizeWindowButton));
        minimizeWindowButton.RegisterCallback<MouseUpEvent>(OnMinimizeInventoryClick, TrickleDown.TrickleDown);

        // Minized inventory button
        _minimizedInventoryBtn = _minimizedTemplate.Instantiate()[0];
        _minimizedInventoryMouseOverManipulator = new MouseOverDetectionManipulator(_minimizedInventoryBtn);
        _minimizedInventoryBtn.AddManipulator(_minimizedInventoryMouseOverManipulator);
        _minimizedInventoryMouseOverManipulator.Disable();

        _minimizedInventoryBtn.style.left = new StyleLength(Screen.width / 2);
        _minimizedInventoryBtn.style.top = new StyleLength(Screen.height / 2);

        _minimizedInventoryBtn.RegisterCallback<ClickEvent>(OnMinimizedInventoryClick, TrickleDown.TrickleDown);
        _minimizedInventoryDragManipulator = new DragManipulator(_minimizedInventoryBtn, _minimizedInventoryBtn);
        _minimizedInventoryBtn.AddManipulator(_minimizedInventoryDragManipulator);

        _root.Add(_minimizedInventoryBtn);
        _minimizedInventoryBtn.style.display = DisplayStyle.None;
    }

    private void OnMinimizeInventoryClick(MouseUpEvent evt)
    {
        _minimizedInventoryBtn.style.display = DisplayStyle.Flex;
        _minimizedInventoryMouseOverManipulator.Enable();
        HideWindow();
    }

    private void OnMinimizedInventoryClick(ClickEvent evt)
    {
        if (!_minimizedInventoryDragManipulator.dragged)
        {
            AudioManager.Instance.PlayUISound("click_01");
            _minimizedInventoryBtn.style.display = DisplayStyle.None;
            _minimizedInventoryMouseOverManipulator.Disable();
            ShowWindow();
        }
    }

    private void CreateTabs()
    {
        _inventoryTabView = GetElementById("InventoryTabView");

        VisualElement tabHeaderContainer = _inventoryTabView.Q<VisualElement>("tab-header-container");
        if (tabHeaderContainer == null)
        {
            Debug.LogError("tab-header-container is null");
        }
        VisualElement tabContainer = _inventoryTabView.Q<VisualElement>("tab-content-container");

        if (tabContainer == null)
        {
            Debug.LogError("tab-content-container");
        }

        for (int i = _tabs.Count - 1; i >= 0; i--)
        {

            VisualElement tabElement = _tabTemplate.CloneTree()[0];
            tabElement.name = _tabs[i].TabName;
            tabElement.AddToClassList("unselected-tab");

            VisualElement tabHeaderElement = _tabHeaderTemplate.CloneTree()[0];
            tabHeaderElement.name = _tabs[i].TabName;
            tabHeaderElement.Q<Label>().text = _tabs[i].TabName;

            tabHeaderContainer.Add(tabHeaderElement);
            tabContainer.Add(tabElement);

            _tabs[i].Initialize(_windowEle, tabElement, tabHeaderElement , true , true);
        }

        if (_tabs.Count > 0)
        {
            SwitchTab(_tabs[0]);
        }

        _tabs[0].MainTab = true;

        _gearTab = new InventoryGearTab();
        _gearTab.Initialize(_windowEle, null, null , true , true);

        _maximumWeight = 10000;
        _weightLabel.text = "00.00%";
    }

    public int GetEmptySlot()
    {
        return _tabs[0].GetEmptySlot();
    }
    public bool SwitchTab(InventoryTab switchTo)
    {
        
        if (_activeTab != switchTo)
        {
            if (_activeTab != null)
            {
                _activeTab.TabContainer.AddToClassList("unselected-tab");
                _activeTab.TabHeader.RemoveFromClassList("active");
            }

            switchTo.TabContainer.RemoveFromClassList("unselected-tab");
            switchTo.TabHeader.AddToClassList("active");
            //ScrollDown(switchTo.Scroller);
            SwitchItemList(switchTo);
            _activeTab = switchTo;
            return true;
        }
        
        return false;
    }

    private void SwitchItemList(InventoryTab switchTo)
    {
        //Equip
        //Supplies
        //Quest
        if (!switchTo.TabName.Equals("All"))
        {
            List<ItemInstance> filterList = PlayerInventory.Instance.FilterInventory(switchTo.GetFilterCategories);
            if (filterList != null && filterList.Count > 0)
            {
                switchTo.SetItemList(filterList);
            }
            else
            {
                switchTo.AddAllEmptyInventory(0);
            }
        }
        else
        {
            List<ItemInstance> items_collect = PlayerInventory.Instance.GetPlayerInvetoryToList();
            switchTo.SetItemList(items_collect);
        }
    }

    public void UpdateStats(PlayerInterludeStats stats)
    {
        if(stats != null)
        {
            _currentWeight = stats.CurrWeight;
            _maximumWeight = stats.MaxWeight;
            _weightLabel.text = stats.WeightPercent()+"%";

            if(_weiBar != null & _weiBarBg != null)
            {
                float bgWidth = 138;
                //float bgWidth = _expBarBg.resolvedStyle.width;
                double expRatio = (double)stats.CurrWeight / stats.MaxWeight;
                double barWidth = bgWidth * expRatio;
                if (stats.MaxWeight == 0)
                {
                    barWidth = 0;
                }
                //_weiBar.style.width = Convert.ToSingle(barWidth);
                _weiBarBg.style.width = Convert.ToSingle(barWidth);
            }
  
        }

    }

    public void SetItemList(List<ItemInstance> allItems , List<ItemInstance> quipAllItems, int adenaCount, int usedSlots , int userId)
    {
        //_adenaCountLabel.text = adenaCount.ToString();
        SetAdenaCountLabel(adenaCount);
        //_tabs[0].ForEach((tab) =>
        //{
        _tabs[0].SetItemList(allItems);
        _gearTab.SetEquipList(quipAllItems , userId);
        _slotCount = PLAYER_INVENTORY_SIZE;
        _inventoryCountLabel.text = $"({usedSlots}/{_slotCount})";
        // });
        SwitchTab(_tabs[0]);

}
    public void UpdateItemList(List<ItemInstance> removeAndAdd , List<ItemInstance> modified , List<ItemInstance> listEquipModified,  int adenaCount , int usedSlots , ChangeInventoryData changeInventoryData , int userId)
    {
        if (_activeTab.MainTab)
        {

            // Slot count
            _slotCount = PLAYER_INVENTORY_SIZE;
            _inventoryCountLabel.text = $"({usedSlots}/{_slotCount})";
            SetAdenaCountLabel(adenaCount);
            _tabs[0].UpdateItemList(removeAndAdd, modified , changeInventoryData);
            _gearTab.UpdateEquipList(listEquipModified , changeInventoryData , userId);
        }
        else
        {

            _slotCount = PLAYER_INVENTORY_SIZE;
            _inventoryCountLabel.text = $"({usedSlots}/{_slotCount})";
            SetAdenaCountLabel(adenaCount);
            _activeTab.SetItemList(PlayerInventory.Instance.FilterInventory(_activeTab.GetFilterCategories));
            _gearTab.SetEquipList(PlayerInventory.Instance.GetPlayerEquipToList(), userId);
        }

    }

    public void RemoveOldEquipItemOrNoQuipItem(ChangeInventoryData changeInventoryData , int userId)
    {
        List<ItemInstance> obsoleteItemsInventory = changeInventoryData.ItemsInventory();
        List<ItemInstance> obsoleteItemsGear = changeInventoryData.ItemsGears();
        List<ReplaceGearData> obsoleteItemsGearReplace = changeInventoryData.ItemsReplace();

        foreach (ItemInstance item in obsoleteItemsInventory)
        {
            if (!obsoleteItemsGearReplace.Any(replaceData => replaceData.GetGear().EqualsBodyPart(item.BodyPart , item.IsBow())))
            {
                _tabs[0].ModifiedRemove(item);
            }
        }

        obsoleteItemsGear.ForEach(item =>
        {
            _gearTab.ModifiedRemove(item.BodyPart, item);
            EventBus.Instance.UnEquipped(item , userId);
        });


        foreach (ReplaceGearData item in obsoleteItemsGearReplace)
        {

            ItemInstance source = item.GetSource();
            ItemInstance gearItem = item.GetGear();

            InventorySlot source_item_slot = _tabs[0].GetInventorySlot(source.ObjectId);

            if (source_item_slot != null && source_item_slot.ItemInstance != null)
            {
                _gearTab.ModifiedReplace(source_item_slot.ItemInstance , userId);
            }



            if(gearItem != null & source_item_slot != null)
            {
                EventBus.Instance.UnEquipped(gearItem , userId);
                source_item_slot.AssignItem(gearItem);
            }
            //if there are 2 items in LHand and Rhand, we move 1 to the position of the one who is trying to change, and insert the second item into the inventory
            else if (gearItem != null & source_item_slot == null)
            {
                EventBus.Instance.UnEquipped(gearItem, userId);
                _tabs[0].AddItem(gearItem);
            }

        }


    }


    private void SetAdenaCountLabel(int count )
    {
        _adenaCountLabel.text = ToolTipsUtils.ConvertToPrice(count);
    }

 
    public void ToggleHideWindowManual()
    {

        Dictionary<int, ItemInstance> allItems = GetInventoryItems();
        Dictionary<int, ItemInstance> equipItems = GetInventoryEquipItems();
        int adenaCount = GetAdenaCount(allItems.Values.ToList());


        if (_isWindowHidden)
        {

             PlayerInventory.Instance.SetInventory(allItems, equipItems , true, adenaCount , allItems.Count + equipItems.Count);
        }
        else
        {
            HideWindow();
        }
    }

    private Dictionary<int, ItemInstance> GetInventoryItems()
    {
       return  PlayerInventory.Instance.GetPlayerInventory();
    }

    private Dictionary<int, ItemInstance> GetInventoryEquipItems()
    {
        return PlayerInventory.Instance.GetPlayerEquipInventory();
    }

    private int GetAdenaCount(List<ItemInstance> modified)
    {
        ItemInstance item = modified.FirstOrDefault(o => o.Category == ItemCategory.Adena);
        return (item != null) ? item.Count : 0;
    }


    public ItemInstance GetItemByPosition(int position)
    {
        if(_activeTab != null)
        {
            return _activeTab.GetInventorySlotByPosition(position);
        }

        return null;
    }

    public GearItem GetGearPosition(int position)
    {
       return _gearTab.ConvertIdToGearType(position);
    }


}
