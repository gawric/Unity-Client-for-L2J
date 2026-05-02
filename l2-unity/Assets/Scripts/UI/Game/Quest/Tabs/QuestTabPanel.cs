using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class QuestTabPanel : AbstractToggle, IContent
{
    private VisualElement _container;
    private VisualElement _insideContent;
    private const string ContentSingleName = "inside_content";
    private VisualTreeAsset _templateToggleButton;
    private const string DfButtonName = "DF_Button";
    private const string DataElementName = "ListData";
    private int[] _selectedData;
    private List<QuestInstance> _questList = new();
    private List<EventCallback<ClickEvent>> _questListCallback = new();
    private List<VisualElement> _questListButtons = new();
    private LastClickModel _lastClick;
    public event System.Action<int, QuestInstance> OnButtonClick;

    public void SetTemplateContent(VisualTreeAsset templateContainer, List<VisualTreeAsset> otherElement)
    {
        var templateInsideContent = otherElement[0];
        _container = ToolTipsUtils.CloneOne(templateContainer);
        _insideContent = ToolTipsUtils.CloneOne(templateInsideContent);
        _templateToggleButton = otherElement[1];
        _questList.Clear();
        _questListCallback.Clear();
        _questListButtons.Clear();
    }



    public void AddElementsToContent<T>(params T[] elements)
    {
        UnregisterCallbacks();
        _questList.Clear();

        _questList = GetList(elements);
        _selectedData = new int[_questList.Count];
        _insideContent.Clear();

        var countQuestLabel = _container.Q<Label>("LabelCountQuest");
        if (countQuestLabel != null)
            countQuestLabel.text = $"({_questList.Count}/40)";

        if (_questList.Count == 0) return;

        for (int i = 0; i < _questList.Count; i++)
        {
            
            var quest = _questList[i];

            if(!quest.IsComplete())
            {
                var buttonElement = ToolTipsUtils.CloneOne(_templateToggleButton);
                var nameLabel = buttonElement.Q<Label>("NameLabel");
                var dataLabel = buttonElement.Q<Label>("DataLabel");
                var data = buttonElement.Q<VisualElement>(DataElementName);
                var button = buttonElement.Q<Button>(DfButtonName);

                var eventButton = RegisterClickButton(button, data, i);
                _questListCallback.Add(eventButton);
                _questListButtons.Add(buttonElement);
                SetData(quest, nameLabel, dataLabel);
                SetInitDf(button, data, i);


                _insideContent.Add(buttonElement);
            }


        }
    }
    private void SetInitDf(Button button , VisualElement data , int i)
    {
        if (i == 0)
        {
            _lastClick = new LastClickModel(button, data, 0);
            OnButtonClick?.Invoke(_selectedData[0], _questList[0]);
        }
        else
        {
            _selectedData[i] = 0;
            ClickDf(button, _selectedData, data, i);
        }
    }
    private void UnregisterCallbacks()
    {
        for (int i = 0; i < _questListButtons.Count && i < _questListCallback.Count; i++)
        {
            var button = _questListButtons[i].Q<Button>(DfButtonName);
            var callback = _questListCallback[i];
            button?.UnregisterCallback(callback);
        }
        _questListCallback.Clear();
        _questListButtons.Clear();
    }

    private void SetData(QuestInstance quest, Label nameLabel, Label dataLabel)
    {
        if (nameLabel == null || dataLabel == null) return;
        nameLabel.text = quest.QuestName();
        dataLabel.text = quest.GetQuestSource();
    }

    private EventCallback<ClickEvent> RegisterClickButton(Button button, VisualElement dataElement, int indexButton)
    {
        if (button == null || dataElement == null) return null;
        EventCallback<ClickEvent> callback = evt =>
        {
            var btn = (Button)evt.target;
            ClickDf(btn, _selectedData, dataElement, indexButton);
            ToggleLastClick(indexButton);
            SetLastClick(btn, dataElement, indexButton);
            OnButtonClick?.Invoke(_selectedData[indexButton], _questList[indexButton]); 
        };
        button.RegisterCallback(callback);
        return callback;
    }

    private void SetLastClick(Button button, VisualElement dataElement, int indexButton)
    {
        if (_lastClick == null || _lastClick.IndexButton != indexButton)
            _lastClick = new LastClickModel(button, dataElement, indexButton);
    }

    private void ToggleLastClick(int currentIndexButton)
    {
        if (_lastClick != null && _lastClick.IndexButton != currentIndexButton)
        {
            _selectedData[_lastClick.IndexButton] = 0;
            ClickDf(_lastClick.Button, _selectedData, _lastClick.DataElement, _lastClick.IndexButton);
        }
    }

    private List<QuestInstance> GetList<T>(params T[] elements)
    {
        return elements
            .SelectMany(e => e is IEnumerable<QuestInstance> enumerable ? enumerable : new[] { e as QuestInstance })
            .Where(q => q != null)
            .ToList();
    }

    public VisualElement GetOrCreateTab(VisualElement content)
    {
        if (_container != null && _insideContent != null)
        {
            content.Clear();
            AddContent(_container);
            content.Add(_container);
        }
        return _container;
    }

    private void AddContent(VisualElement container)
    {
        var insideContent = GetElementById(container, ContentSingleName);
        if (insideContent != null && _insideContent != null)
            insideContent.Add(_insideContent);
    }

    private VisualElement GetElementById(VisualElement content, string id)
    {
        var btn = content.Q<VisualElement>(id);
        if (btn == null)
            Debug.LogError($"{id} can't be found.");
        return btn;
    }
}

public class LastClickModel
{
    public Button Button { get; set; }
    public VisualElement DataElement { get; set; }
    public int IndexButton { get; set; }

    public LastClickModel(Button button, VisualElement dataElement, int indexButton)
    {
        Button = button;
        DataElement = dataElement;
        IndexButton = indexButton;
    }
}