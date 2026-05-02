using FMOD;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

using UnityEngine.UIElements;


public class ShowListWindow : L2PopupWindow
{
    private static ShowListWindow _instance;
    public static ShowListWindow Instance { get { return _instance; } }
    private Button _СЃancelButton;
    private Button _okButton;
    private DropdownField _dropdown;
    private Dictionary<string, int> _players;
    private string _select = "";
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

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/ShowList/ShowListWindow");

    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);
        yield return new WaitForEndOfFrame();

        _СЃancelButton = _windowEle.Q<Button>("CancelButton");
        _okButton = _windowEle.Q<Button>("StartButton");
        var content = _windowEle.Q<VisualElement>("content");

        _dropdown = content.Q<DropdownField>("comboBox");

        RegisterCloseWindowEvent("btn-close-frame");
        var dragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(dragArea, _windowEle);
        dragArea.AddManipulator(drag);
        DisableEventOnOver(_dropdown);


        _dropdown.RegisterValueChangedCallback(OnDropdownValueChanged);

        _okButton.RegisterCallback<ClickEvent>((evt) => OnClick(evt));
        _СЃancelButton.RegisterCallback<ClickEvent>((evt) => OnCancel(evt));

        RegisterClickWindowEvent(_windowEle, null);
        OnCenterScreen(_root);
    }


    public void AddList(Dictionary<string, int> players)
    {
        _dropdown.value = "";
        var list = players.Keys.ToList();
        _dropdown.choices = list;
        _players = players;
    }



    private void OnDropdownValueChanged(ChangeEvent<string> evt)
    {
        string playeName = evt.newValue;
        _select = playeName;
    }

    private async void OnClick(ClickEvent evt)
    {

        if (_players.ContainsKey(_select))
        {
            int objectId = _players[_select];
            var sendPaket = CreatorPacketsUser.CreateSendableItemList(objectId);
            SendGameDataQueue.Instance().AddItem(sendPaket, GameClient.Instance.IsCryptEnabled(), GameClient.Instance.IsCryptEnabled());
            HideWindow();
        }
    }


    private async void OnCancel(ClickEvent evt)
    {
        HideWindow();
    }





    private void OnDestroy()
    {
        _instance = null;
    }

}
