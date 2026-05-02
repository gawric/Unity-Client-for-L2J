using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface IContent 
{
    void SetTemplateContent(VisualTreeAsset templateContainer, List<VisualTreeAsset> otherElement);
    VisualElement GetOrCreateTab(VisualElement content);
    void AddElementsToContent<T>(params T[] data);
    public event System.Action<int, QuestInstance> OnButtonClick;
}
