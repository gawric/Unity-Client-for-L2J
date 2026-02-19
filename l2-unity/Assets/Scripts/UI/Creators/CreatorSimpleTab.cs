using System;
using UnityEngine;
using UnityEngine.UIElements;


public class CreatorSimpleTab : AbstractLoaderTemplate, ICreatorSimpleTab
{
    public event Action<int> EventSwitchTabByIndexOfTab;

    private VisualElement _injectTemplate;
    public void SetContent(int idTemplate)
    {
        VisualElement tabContent =  GetActiveContent();
        _injectTemplate = GetTemplateById(idTemplate);

        if (_injectTemplate == null)
        {
            Debug.LogWarning("CreatorSimpleTab> SetContent: Not Fount Inject Template");
        }

        if (tabContent != null & _injectTemplate != null)
        {
            tabContent.Clear();
            tabContent.Add(_injectTemplate);
        }

    }

    public void SetClickActiveTab(int position)
    {
        throw new NotImplementedException();
    }


    public VisualElement[] GetVisualElements(string[] names)
    {
        VisualElement[] elements = new VisualElement[names.Length];

        for (int i =0; i< names.Length; i++)
        {
            string name = names[i];
            if (name == null) continue;
            elements[i] = _injectTemplate.Q<VisualElement>(name);
        }

        return elements;
    }

    void ICreatorSimpleTab.LoadAsset(Func<string, VisualTreeAsset> loaderFunc, string[] loadTemplate)
    {
        LoadAsset(loaderFunc, loadTemplate);
    }
}
