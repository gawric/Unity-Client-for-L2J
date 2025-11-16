using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class DescriptionSkillWindow : L2PopupWindow
{
    private static DescriptionSkillWindow _instance;
    public static DescriptionSkillWindow Instance { get { return _instance; } }
    private DataProviderWindows _dataProvider;
    private VisualElement _content;
    private Label _spLabel;
    private Button _backButton;
    private Button _okButton;
    private AcquireSkillInfo _packet;
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _dataProvider = new DataProviderWindows();
        }
        else
        {
            Destroy(this);
        }
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/SkillLearn/Learn/DescriptionSkillWindow");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);

        _content = GetElementById("Content");
        _spLabel = (Label)GetElementById("spCount");
        _backButton = (Button)GetElementById("BackButton");
        _okButton = (Button)GetElementById("LearnButton");
        _backButton = (Button)GetElementById("BackButton");
        _backButton.RegisterCallback<ClickEvent>(OnBack);
        _okButton.RegisterCallback<ClickEvent>(OnLearnSkill);

        RegisterCloseWindowEvent("btn-close-frame");
        RegisterClickWindowEvent(_windowEle, dragArea);
        OnCenterScreen(_root);

    }

    public void AddData(AcquireSkillInfo packet)
    {
        _packet = packet;
        _dataProvider.AddDescriptionSkill(packet.GetId(), packet.GetSpCoast(), packet.GetLevel(), _content);
        _dataProvider.AddRequiredSkillInfo(packet.RequiredSkillInfo, _content);
    }

    public void ShowWindow()
    {
        UserInfo user = StorageNpc.getInstance().GetFirstUser();
        _spLabel.text = user.PlayerInfoInterlude.Stats.Sp.ToString();
        base.ShowWindow();
    }

    private void OnBack(ClickEvent evt)
    {
        base.HideWindow();
        SkillLearnWindow.Instance.ShowWindow();
    }

    private void OnLearnSkill(ClickEvent evt)
    {
        // 0 - GeneralSkills
        // 1 - Common skills.
        // 2 - Pledge skills
        if (_packet != null)
        {
            RequestAcquireSkill sendPaket = CreatorPacketsUser.CreateRequestAcquireSkill(_packet.GetId(), _packet.GetLevel(), 0);
            SendGameDataQueue.Instance().AddItem(sendPaket, GameClient.Instance.IsCryptEnabled(), GameClient.Instance.IsCryptEnabled());
        }
        base.HideWindow();
    }

    private void OnDestroy()
    {
        _instance = null;
    }
}
