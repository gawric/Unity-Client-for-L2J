
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UIElements;


public class SkillLearnWindow : L2PopupWindow
{
    private static SkillLearnWindow _instance;
    protected VisualTreeAsset _rowsTemplate;
    private ICreatorVerticalScroll _creatorWindow;
    private Label _spLabel;
    private List<OtherModel> _list;


    public static SkillLearnWindow Instance { get { return _instance; } }
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _creatorWindow = new CreatorVerticalScrollWindows();
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/SkillLearn/Learn/SkillLearnWindow");
        _rowsTemplate = LoadAsset("Data/UI/_Elements/Template/Skills/ItemSkillRow");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);
        var content = (VisualElement)GetElementById("content");
        _spLabel = (Label)GetElementById("spCount");


        //_creatorWindow.InitTradeTabs(new string[] { "ALL", "Other" });
        _creatorWindow.CreateTabs(content, null, _rowsTemplate);
        _creatorWindow.EventLeftClick += ClickItem;

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

    }

    public void AddData(List<OtherModel> list)
    {
        if(_list != null ) _list.Clear();
        _list = list;
        _creatorWindow.AddOtherData(list);
    }
    public void ClickItem(int skillId , ItemCategory category , int position)
    {

       AcquireData model = FilterBySKillId(_list, skillId);

       if(model != null)
       {
            //base.OnlyHideWindow();
            RequestAcquireSkillInfo sendPaket = CreatorPacketsUser.CreateRequestAcquireSkillInfo(model.GetId(), model.GetValue1(), model.GetAcqType());
            SendGameDataQueue.Instance().AddItem(sendPaket, GameClient.Instance.IsCryptEnabled(), GameClient.Instance.IsCryptEnabled());
        }

    }

    

    public AcquireData FilterBySKillId(List<OtherModel> list , int skillId)
    {
        foreach (var item in list)
        {
            AcquireData data = (AcquireData)item.GetOtherModel();
            if (data.GetId() == skillId) return data;
        }
        return null;

    }
    public void ShowWindow()
    {
        UserInfo user = StorageNpc.getInstance().GetFirstUser();
        _spLabel.text = user.PlayerInfoInterlude.Stats.Sp.ToString();
        base.ShowWindow();
    }
}
