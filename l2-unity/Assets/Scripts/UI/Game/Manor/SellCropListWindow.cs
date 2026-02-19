using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class SellCropListWindow : L2PopupWindow
{
    private static SellCropListWindow _instance;
    private ICreatorTables _creatorTableWindows;
    private VisualElement _content;
    public static SellCropListWindow Instance { get { return _instance; } }
    private List<TableColumn> _defaultColumns;

    private void Awake()
    {
        if (_instance == null)
        {
            _creatorTableWindows = new CreatorTableWindows();
            _instance = this;
        }
        else
        {
            _creatorTableWindows.DestroyTable();
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Manor/SellCropListWindow");

    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {


        InitWindow(root);


        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");
        _content = GetElementByClass("quest-content");
        var closeButton = GetElementById("CloseButton");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        _creatorTableWindows.InitTable(_content);
        _creatorTableWindows.LoadAsset(LoadAsset);



        closeButton.RegisterCallback<MouseUpEvent>(OnCloseButtonMouseUp);
        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        CreateEmptyTable();
        OnCenterScreen(root);

    }


    public void SetDataTable(ExShowSellCropList showSellCropList)
    {

        List<CastleCrop> list = new List<CastleCrop>();

        var harvestsList = new List<string>();
        var authList = new List<string>();
        var buyList = new List<string>();
        var priceList = new List<string>();
        var rewardList = new List<string>();
        var propList = new List<string>();
        var salesList = new List<string>();

        if(showSellCropList.List.Count > 0)
        {
            foreach (CastleCrop crop in showSellCropList.List)
            {
                harvestsList.Add(crop.GetName());
                authList.Add(crop.Level.ToString());
                buyList.Add(crop.Amount.ToString());
                priceList.Add(crop.Price.ToString());
                rewardList.Add(crop.RewardCrop.ToString());
                propList.Add(crop.RewardIdSeed1.ToString());
                salesList.Add(crop.RewardIdSeed2.ToString());
            }

        }
        else
        {
            //Add Test Data row 1
            harvestsList.Add("Cold code");
            authList.Add("3");
            buyList.Add("5");
            priceList.Add("7");
            rewardList.Add("Varnish");
            propList.Add("Suede");
            salesList.Add("coke");

            //Add Test Data row 2
            harvestsList.Add("Blue code");
            authList.Add("2");
            buyList.Add("1");
            priceList.Add("4");
            rewardList.Add("Varnish");
            propList.Add("Suede");
            salesList.Add("coke");


            //Add Test Data row 3
            harvestsList.Add("Golden code");
            authList.Add("2");
            buyList.Add("2");
            priceList.Add("3");
            rewardList.Add("Varnish");
            propList.Add("Suede");
            salesList.Add("coke");
        }



        _defaultColumns[0].SetData(harvestsList);
        _defaultColumns[1].SetData(authList);
        _defaultColumns[2].SetData(buyList);
        _defaultColumns[3].SetData(priceList);
        _defaultColumns[4].SetData(rewardList);
        _defaultColumns[5].SetData(propList);
        _defaultColumns[6].SetData(salesList);

        _creatorTableWindows.UpdateTableData(_defaultColumns);

        //_creatorTableWindows.CreateTable(new List<TableColumn> { new TableColumn(false, "Mission Name", 13 ,  new List<string> { "Letters of Love" , "What Women Want", "Will the Seal Be Broken" } , 13) ,
        //  new TableColumn(false, "Conditions", 0, new List<string> { "No Requirements" , "Elf,Human", "Dark Elf" } ,13),
        //  new TableColumn(true, "Level", 0, new List<string> { "2-5" , "2-5" , "16-26" } , 0),
        //  new TableColumn(true, "Repeatable", 0 , new List<string> { "1"  , "1"  , "1" } , 0),
        //  new TableColumn(false, "Source", 0 , new List<string> { "Darin" , "Arujien", "Talloth" }, 18)});
    }

    private void CreateEmptyTable()
    {
        _defaultColumns = GetColumnWindow();
        _creatorTableWindows.CreateTable(_defaultColumns);
    }

    private List<TableColumn> GetColumnWindow()
    {
 
        var harvest = new TableColumn(false, "Harvest Name", 0, new List<string>(), 13);
        var auth = new TableColumn(true, "Lvl", 0, new List<string>(), 0);
        var buy = new TableColumn(true, "Buy Remaining", 0, new List<string>(), 0);
        var price = new TableColumn(true, "Purchase Price", 0, new List<string>(), 0);
        var reward = new TableColumn(true, "Reward", 0, new List<string>(), 13);
        var prop = new TableColumn(false, "My Prop.", 0, new List<string>(), 13);
        var sales = new TableColumn(false, "Sales", 0, new List<string>(), 13);


       return  new List<TableColumn> { harvest, auth, buy, price, reward, prop, sales };
    }

    private void OnCloseButtonMouseUp(MouseUpEvent evt)
    {
        _creatorTableWindows.ClearTable();
        base.HideWindow();
    }


}
