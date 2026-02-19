using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface ICreatorSkillsPanel 
{
    void LoadAsset(Func<string, VisualTreeAsset> loaderFunc);
    void RefreshData(VisualElement container);

    void CreateSlots(List<SkillInstance> list, int sizeCell);
}
