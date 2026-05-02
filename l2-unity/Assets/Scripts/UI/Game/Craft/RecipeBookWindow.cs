using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static L2Slot;

public class RecipeBookWindow : L2PopupWindow
{
    private VisualTreeAsset _tabTemplate;
    private VisualTreeAsset _tabHeaderTemplate;
    private VisualElement _inventoryTabView;
    private Label _sizelabel;
    private static RecipeBookWindow _instance;
    private RecipeBookItemList _packet;
    public static RecipeBookWindow Instance { get { return _instance; } }
    private ICreatorTradeTab _creatorWindow;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _creatorWindow = new CreatorTradingWindowsWithTabs();

        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Craft/RecipeBookWindow");
        _creatorWindow.LoadAsset(LoadAsset);
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        _creatorWindow.InitTradeTabs(new string[] { "ALL" });
        _creatorWindow.CreateTradeTabs(GetElementById("InventoryTabView") , L2Slot.SlotType.Recipe , true);

        _sizelabel = (Label)GetElementById("sizeLabel");
        Button trashBtn = (Button)GetElementById("TrashBtn");
        trashBtn.AddManipulator(new ButtonClickSoundManipulator(trashBtn));
        trashBtn.AddManipulator(new TooltipManipulator(trashBtn, "Trash"));
        L2Slot trashSlot = new L2Slot(trashBtn, 100, L2Slot.SlotType.Trash);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

        yield return new WaitForEndOfFrame();
    }

    public void AddData(RecipeBookItemList packet)
    {
        if(packet.ListRecipes != null)
        {

            if (_packet != null) _creatorWindow.ClearSlots(_packet.ListItemInstance);

            _packet = packet;
            _creatorWindow.AddData(packet.ListItemInstance);
            UpdateSizeLabel(_sizelabel, packet);
        }
    }


    public void EventDoubleClick(VisualElement slotElement)
    {
        string[] ids = ToolTipsUtils.GetUniquePosition(slotElement);
        int position = Int32.Parse(ids[0]);
        int type = Int32.Parse(ids[1]);
        SlotType slot = ToolTipsUtils.DetectedClickPanel(type);

        if(SlotType.Recipe == slot)
        {
            int recipeId = GetRecipeIdByPosition(position);
            SendRequestOpenCraftWindow(recipeId);
        }
    }


    private void SendRequestOpenCraftWindow(int recipeId)
    {
        SendGameDataQueue.Instance().AddItem(
            CreatorPacketsUser.CreateRequestRecipeItemMakeInfo(recipeId),
            GameClient.Instance.IsCryptEnabled(),
            GameClient.Instance.IsCryptEnabled()
        );
    }

    private void UpdateSizeLabel(Label sizeLabel , RecipeBookItemList packet)
    {
        if (sizeLabel != null & packet != null)
        {
            sizeLabel.text = "(" + _packet.ListItemInstance.Count + "/" + "0" + ")";
        }
    }

    public int GetRecipeIdByPosition(int position)
    {
        return _packet.ListRecipes.FirstOrDefault(x => x.GetPosition() == position + 1)?.GetIDMK() ?? 0;
    }


    public ItemInstance GetRecipeInstanceByPosition(int position)
    {
        return   _packet.ListRecipes.FirstOrDefault(x => x.GetPosition() == position + 1)?.GetRecipeItemInstance();
    }
}
