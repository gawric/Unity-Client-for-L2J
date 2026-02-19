using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class MapWindow : L2PopupWindow
{
    private static MapWindow _instance;
    private ICreatorSimpleTab _creatorSimpleTab;
    private const string _tabName = "Aden";
    private VisualTreeAsset _tabTemplate;
    private VisualTreeAsset _tabHeaderTemplate;
    private const string _mapTemplateName = "Data/UI/_Elements/Game/Map/MapAdenTemplate";
    private MapPanner _mapPanner;
    private MapMarkerFollowCamera _mapMarkerFollowCamera;
    public static MapWindow Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _creatorSimpleTab = new CreatorSimpleTab();
            _mapPanner = new MapPanner();
            _mapMarkerFollowCamera = new MapMarkerFollowCamera();
            _instance = this;

        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Map/MapWindow");
        _tabTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTab");
        _tabHeaderTemplate = LoadAsset("Data/UI/_Elements/Game/Inventory/InventoryTabHeader");
        //id 0 - Map Aden
        _creatorSimpleTab.LoadAsset(LoadAsset, new string[1] { _mapTemplateName });
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);


        var content = (VisualElement)GetElementById("content");

        _creatorSimpleTab.InitContentTabs(new string[1] { _tabName });
        _creatorSimpleTab.CreateTabs(content, _tabTemplate, _tabHeaderTemplate);
        _creatorSimpleTab.SetContent(0);

        Button button = (Button)GetElementById("MyLocation");
        var elements = _creatorSimpleTab.GetVisualElements(new string[3] { "viewport" , "map", "minimapPos" });
        _mapPanner.SetElements(elements[0], elements[1] , elements[2] , button);
        _mapPanner.RegisterCallback();

        _mapMarkerFollowCamera.SetElement(elements[2]);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

        yield return new WaitForEndOfFrame();

  
    }

    public void LateUpdate()
    {
        _mapPanner.UpdateMarker(_isWindowHidden);
    }

    public void TurnMarker(Camera targetCamera)
    {
        _mapMarkerFollowCamera.TurnUpdate(targetCamera , _isWindowHidden);
    }

    public override void ShowWindow()
    {
        _mapPanner.SetDisabled(false);
        _mapPanner.SetManualUpdate(true);
        _mapMarkerFollowCamera.ManualTurnUpdate();
        base.ShowWindow();

    }

    public override void HideWindow()
    {
        _mapPanner.SetDisabled(true);
        _mapPanner.MoveMarkerToOrigin();
        base.HideWindow();
    }
}

