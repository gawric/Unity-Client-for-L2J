using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;


public class CraftingItemWindow : L2PopupWindow
{
    private ICreatorSimpleTrade _creatorSimpleTrade;
    private static CraftingItemWindow _instance;
    private RecipeItemMakeInfo _packet;
    private DataProviderCraftItem _dataProvider;
    private VisualElement _progressBar;
    private VisualElement _progressBarBg;
    private const float _defaultWidthProgressbBar = 177;
    
    public static CraftingItemWindow Instance { get { return _instance; } }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _creatorSimpleTrade = new CreatorSimpleTrade();
            _dataProvider = new DataProviderCraftItem();

        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Craft/CraftingItemWindow");
        _creatorSimpleTrade.LoadAsset(LoadAsset);

    }



    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);


        VisualElement containerTradeSlots = GetElementById("BodyBox");
        _creatorSimpleTrade.CreateSlots(containerTradeSlots, L2Slot.SlotType.RecipeCraftItem , 18);
        _progressBar = GetElementById("ProgressGauge");
        _progressBarBg = GetElementById("ProgressBg");

        var closeButton = GetElementById("CloseButton");
        var createButton = GetElementById("CreateButton");
        var backButton = GetElementById("BackButton");

        // Register button callbacks
        closeButton.RegisterCallback<ClickEvent>(OnCloseButtonClick);
        createButton.RegisterCallback<ClickEvent>(OnCreateButtonClick);
        backButton.RegisterCallback<ClickEvent>(OnBackButtonClick);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

        yield return new WaitForEndOfFrame();
    }

    // Button callback methods
    private void OnCloseButtonClick(ClickEvent evt)
    {
       HideWindow();
    }

    private void OnCreateButtonClick(ClickEvent evt)
    {
        if (_packet != null)
        {
            SendGameDataQueue.Instance().AddItem(
                CreatorPacketsUser.CreateRequestRecipeItemMakeSelf(_packet.RecipeId),
                GameClient.Instance.IsCryptEnabled(),
                GameClient.Instance.IsCryptEnabled());
        }
    }

    private void OnBackButtonClick(ClickEvent evt)
    {

        if (_packet != null)
        {
            SendGameDataQueue.Instance().AddItem(
                CreatorPacketsUser.CreateRequestRecipeBookOpen(_packet.IsDwarvenRecipe),
                GameClient.Instance.IsCryptEnabled(),
                GameClient.Instance.IsCryptEnabled());
        }

        HideWindow();
    }

    public void AddData(RecipeItemMakeInfo packet)
    {
        if(packet != null)
        {
            _packet = packet;
            _creatorSimpleTrade.AddData(packet.RequiredItems , true);
            SetProgressBarData(packet.CurrentMp, packet.MaxMp);
            SetDataHeader(_windowEle , _packet.RecipeData);
        }
    }

    private void SetDataHeader(VisualElement container , RecipeData recipeData)
    {
        int itemId = _packet.RecipeData.ItemId;
        int count = _packet.RecipeData.Count;


        ItemInstance createItem = new ItemInstance(itemId, count, 0);
        _dataProvider.SetDataInfo(container , createItem, recipeData);
    }

    private void SetProgressBarData(int currentMp , int maxMp)
    {
        ProgressBarUtils.UpdatePbCurrentFirst(_progressBarBg, _progressBar, _defaultWidthProgressbBar, currentMp, maxMp);
    }

    public ItemInstance GetRequiredInstanceByPosition(int position)
    {
        return _packet.RequiredItems.FirstOrDefault(x => x.GetPosition() == position);
    }

}
