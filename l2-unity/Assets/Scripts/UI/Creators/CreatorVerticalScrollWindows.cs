using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CreatorVerticalScrollWindows : ICreatorVerticalScroll
{
    public event Action<int, ItemCategory, int> EventLeftClick;
    public event Action<int> EventSwitchTabByIndexOfTab;

    private ScrollView _scrollView;
    private VisualElement _content;
    //private string[] _nameTabs;
    private VisualTreeAsset _templateItems;
    private DataProviderWindows _dataProvider;

    public CreatorVerticalScrollWindows()
    {
        _dataProvider = new DataProviderWindows();
    }



    public void CreateTabs(VisualElement content, VisualTreeAsset empty, VisualTreeAsset templateItems)
    {
        _scrollView = (ScrollView)content.Q<ScrollView>("ScrollView");

        if(_scrollView == null & templateItems == null)
        {
            Debug.LogError("CreatorHorizontalWindows> Not found ScrollView in content element");
        }

        _content = _scrollView.contentContainer;
        _templateItems = templateItems;
    }


    //public void InitTradeTabs(string[] nameTabs)
    //{
    //    _nameTabs = nameTabs ?? new string[0];
   // }



    private VisualElement CloneTemplate(VisualTreeAsset templateItems)
    {
        return ToolTipsUtils.CloneOne(templateItems);
    }



    public void AddOtherData(List<OtherModel> allItems)
    {
        if (_content != null && allItems != null)
        {
            _content.Clear();
            for (int i = 0; i < allItems.Count; i++)
            {
                object model = allItems[i].GetOtherModel();
                VisualElement slotElement = CloneTemplate(_templateItems);

                switch (model)
                {
                    case AcquireData acquireData:
                        _dataProvider.AddLearnSkill(acquireData.GetId(), acquireData.GetCost(), acquireData.GetValue1(), slotElement);
                        slotElement.RegisterCallback<MouseDownEvent>(evt => HandleClickDown(evt, acquireData), TrickleDown.TrickleDown);
                        break;

                    case ModelQuestDemoReward questData:
                        _dataProvider.AddRewardItem(questData.NameReward, questData.DecReward, questData.Icon, slotElement);
                        break;
                    default:
                        Debug.LogWarning($"Unknown model type: {model?.GetType().Name}");
                        break;
                }

                _content.Add(slotElement);
            }
        }
    }

    public void HandleClickDown(MouseDownEvent evt , AcquireData item)
    {
        if (evt.button == 0)
        {
            OnLeftClick(item.GetId(), ItemCategory.None , -1);
        }
    }

    private void OnLeftClick(int itemId, ItemCategory category, int position)
    {
        EventLeftClick?.Invoke(itemId, category, position);
    }
}
