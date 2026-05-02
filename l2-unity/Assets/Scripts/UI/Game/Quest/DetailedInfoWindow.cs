using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class DetailedInfoWindow : L2PopupWindow
{

    private static DetailedInfoWindow _instance;

    public static DetailedInfoWindow Instance
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
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Quest/DetailedInfo");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        var bg = (VisualElement)GetElementById("Darkener");

        if (bg != null)
        {
            bg.style.opacity = new StyleFloat(0.4f);
        }


        // var dragArea = GetElementByClass("drag-area");
        //DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        // dragArea.AddManipulator(drag);


        //RegisterCloseWindowEvent("btn-close-frame");
        //RegisterCloseWindowEventByName("CloseButton");
        //RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

        HideWindow();
    }

}
