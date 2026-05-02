using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractToggle
{
    private string[] fillBackgroundDf = { "Data/UI/Window/Skills/QuestWndPlusBtn_v2", "Data/UI/Window/Skills/Button_DF_Skills_Down_v3" };

    protected void ClickDf(Button btn, int[] arrDfSelect , VisualElement dataElement , int indexButton)
    {
        bool show = arrDfSelect[indexButton] == 0;
        ChangeDfBox(btn, fillBackgroundDf[show ? 0 : 1]);
        arrDfSelect[indexButton] = show ? 1 : 0;
        ToogleHideElement(dataElement, show);
    }

    protected void ToogleHideElement(VisualElement element, bool isHide)
    {
        if (!isHide)
            element.style.display = DisplayStyle.Flex;
        else
            element.style.display = DisplayStyle.None;
    }

    protected void ChangeDfBox(Button btn, string texture)
    {
        IEnumerable<VisualElement> children = btn.Children();
        var e = children.First();
        e.style.display = DisplayStyle.Flex;
        Texture2D iconDfNoraml = LoadTextureDF(texture);
        SetBackgroundDf(btn, iconDfNoraml);
    }

    private void SetBackgroundDf(Button btn, Texture2D iconDfNoraml)
    {
        btn.style.backgroundImage = new StyleBackground(iconDfNoraml);
    }

    private Texture2D LoadTextureDF(string path)
    {
        return Resources.Load<Texture2D>(path);
    }
}
