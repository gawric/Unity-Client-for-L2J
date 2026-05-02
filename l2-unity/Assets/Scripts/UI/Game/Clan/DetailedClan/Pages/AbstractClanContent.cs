using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractClanContent : AbstractButtonEvents
{
    protected DataProviderClanInfo _dataProvider;
    public VisualTreeAsset template;

    public const string USS_STYLE_YELLOW = "button-label-yellow";
    public const string USS_STYLE_DISABLED = "button-label-disabled";

    protected VisualElement content;
    public Action<int> OnClickHide;
    public Action<int> OnClickApply;
    protected VisualElement LoadContent(VisualElement content, VisualElement detailedInfoElement)
    {
        if (content == null)
        {
            return detailedInfoElement.Query<VisualElement>("content");
        }

        return content;
    }

    protected void ClearContent(VisualElement content)
    {
        if (content != null)
        {
            content.Clear();
        }
    }

    protected void SubscribeCloseButton(Button cancelButton, VisualElement detailedInfoElement)
    {
        if (cancelButton != null)
        {
            void OnClick()
            {
                detailedInfoElement.style.display = DisplayStyle.None;
                cancelButton.clicked -= OnClick;  // Unsubscribe here
                OnClickHide?.Invoke(-1);
            }

            cancelButton.clicked += OnClick;
        }
    }

    protected void SubscribeApplyButton(Button applylButton, VisualElement detailedInfoElement , int useRank)
    {
        if (applylButton != null)
        {
            void OnClick()
            {
                detailedInfoElement.style.display = DisplayStyle.None;
                applylButton.clicked -= OnClick;  // Unsubscribe here
                OnClickApply?.Invoke(useRank);
            }

            applylButton.clicked += OnClick;
        }
    }

    protected void HideElement(VisualElement element)
    {
        if (element != null)
        {
            element.style.display = DisplayStyle.None;
        }
    }

    protected void ShowElement(VisualElement element)
    {
        if (element != null)
        {
            element.style.display = DisplayStyle.Flex;
        }
    }
}
