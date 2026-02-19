using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements; 

[System.Serializable]
public class ChatTab
{
    [SerializeField] string _tabName = "Tab";
    [SerializeField] private ChatType _chatType;

    public ChatType ChatType => _chatType;
    public int TabId => (int)_chatType;

    private bool _autoscroll = true;
    private ScrollView _scrollView;
    private VisualElement _content;
    private Scroller _scroller;
    private VisualElement _tabContainer;
    private VisualElement _tabHeader;
    private VisualElement _chatWindowEle;
    private static int _defaultlabelCount = 161;
    public string TabName { get { return _tabName; } }

    //public Label Content { get { return _content; } }


    private DataLabel[] _labelArray = new DataLabel[_defaultlabelCount];
    public VisualElement TabContainer { get { return _tabContainer; } }
    public VisualElement TabHeader { get { return _tabHeader; } }
    public Scroller Scroller { get { return _scroller; } }
    private VisualTreeAsset _messageLabelTemplate;

    public void SetMessageTemplate(VisualTreeAsset messageLabelTemplate)
    {
        _messageLabelTemplate = messageLabelTemplate;
    }

    public void Initialize(VisualElement chatWindowEle, VisualElement tabContainer, VisualElement tabHeader) {
        _chatWindowEle = chatWindowEle;
        _tabContainer = tabContainer;
        _tabHeader = tabHeader;
        _scrollView = tabContainer.Q<ScrollView>("ScrollView");
        _scroller = _scrollView.verticalScroller;
        _content = tabContainer.Q<VisualElement>("Content");
       // _content.text = "";

        tabHeader.AddManipulator(new ButtonClickSoundManipulator(tabHeader));

        tabHeader.RegisterCallback<MouseDownEvent>(evt => {
            if(ChatWindow.Instance.SwitchTab(this)) {
                AudioManager.Instance.PlayUISound("window_open");
            }
        }, TrickleDown.TrickleDown);
      
        //RegisterAutoScrollEvent();
        RegisterPlayerScrollEvent();
        CreateEmptyLabel();
    }

 


    private void CreateEmptyLabel()
    {
        for(int i = 0; i < _defaultlabelCount; i++){
            var template = _messageLabelTemplate.CloneTree();
            var label = template.Q<Label>("Text");

            if(label != null)
            {
                _labelArray[i] = new DataLabel(i, label, false);
                _content.Add(label);
            }

        }
    }

    public void ConcatMessage(string message)
    {
        DataLabel dl = GetDataLabel();
        Label chatContent = dl.GetLabel();
        SetText(chatContent, message);
        dl.SetVisible(true);
        ChatWindow.Instance.ScrollDown(_scroller);
    }


    private void SetText(Label chatContent , string message)
    {
        if (chatContent.text.Length > 0)
        {
            chatContent.text += "\r\n";
        }
        chatContent.text += message;
    }

    public DataLabel GetDataLabel()
    {
        var hiddenLabel = Array.Find(_labelArray, dl => !dl.IsVisible());
        if (hiddenLabel != null)
        {
            return hiddenLabel;
        }
        else
        {
            return IfDlNotEmpty();
        }
        
    }

 

    private DataLabel IfDlNotEmpty()
    {
        ShiftElements(_labelArray);
        return _labelArray[_labelArray.Length - 1];
    }



    public void ShiftElements(DataLabel[] array)
    {

        for (int i = 0; i < array.Length - 1; i++)
        {
            array[i].SetText(array[i + 1].GetLabel().text);
        }

        array[array.Length - 1].GetLabel().text = "";
    }


    public void SetLabelVisible(DataLabel dl , bool visible)
    {
        dl.SetVisible(visible);
    }
    //private void RegisterAutoScrollEvent() {
       // _content.RegisterValueChangedCallback(evt => {
         //   if(_autoscroll) {
          //      ChatWindow.Instance.ScrollDown(_scroller);
          //  }
        //});    
   // }

    private void RegisterPlayerScrollEvent() {
        var highBtn = _scroller.Q<RepeatButton>("unity-high-button");
        var lowBtn = _scroller.Q<RepeatButton>("unity-low-button");
        var dragger = _scroller.Q<VisualElement>("unity-drag-container");

        highBtn.RegisterCallback<MouseUpEvent>(evt => {
            VerifyScrollValue();
        });
        lowBtn.RegisterCallback<MouseUpEvent>(evt => {
            VerifyScrollValue();
        });
        highBtn.AddManipulator(new ButtonClickSoundManipulator(highBtn));
        lowBtn.AddManipulator(new ButtonClickSoundManipulator(lowBtn));
        dragger.RegisterCallback<MouseUpEvent>(evt => {
            VerifyScrollValue();
        });
        dragger.RegisterCallback<WheelEvent>(evt => {
            VerifyScrollValue();
        });
        
        _chatWindowEle.RegisterCallback<GeometryChangedEvent>(evt => {
            if(_autoscroll) {
                ChatWindow.Instance.ScrollDown(_scroller);
            }
        });
    }

    private void VerifyScrollValue() {
        if(_scroller.highValue > 0 && _scroller.value == _scroller.highValue || _scroller.highValue == 0 && _scroller.value == 0) {
            _autoscroll = true;
        } else {
            _autoscroll = false;
        }
    }
}
