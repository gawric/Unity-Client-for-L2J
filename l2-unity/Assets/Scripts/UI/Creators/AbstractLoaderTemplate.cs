using System;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractLoaderTemplate : AbstractCreator
{
    protected VisualTreeAsset[] _templates;

    protected VisualElement GetTemplateById(int idTemplate)
    {

        if (!ArrayUtils.IsValidIndexArray(_templates, idTemplate))
        {
            return null;
        }

        VisualTreeAsset template = _templates[idTemplate];
        return ToolTipsUtils.CloneOne(template);
    }


    protected void LoadAsset(Func<string, VisualTreeAsset> loaderFunc, string[] loadTemplate)
    {
        _templates = new VisualTreeAsset[loadTemplate.Length];

        for (int i = 0; i < loadTemplate.Length; i++)
        {
            _templates[i] = loaderFunc(loadTemplate[i]);
        }
    }

}
