using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class DropdownFieldUtils
{
    public void SubscribeToDropdownOpenClose(VisualElement root, System.Action<bool> onOpen, System.Action<bool> onClose , ref bool isCloseCreate)
    {
        if (root == null) return;

        var panels = root.Query<VisualElement>(null, "unity-base-dropdown__container-inner").ToList();
        foreach (var p in panels) HookPanel(p, onOpen, onClose , ref isCloseCreate);
    }
    public  bool IsDropdownPanelOpen(VisualElement root)
    {
        if (root == null) return false;

        var inner = root.Query<VisualElement>(null, "unity-base-dropdown__container-inner").ToList();
        if (IsAnyVisible(inner))
        {
            return true;
        }


       // var contentWidth = root.Query<VisualElement>(null, "unity-base-dropdown--content-width-menu").ToList();
        //if (IsAnyVisible(contentWidth))
       // {
        //    Debug.Log("Return true 2");
        //    return true;
       // }

        //var items = root.Query<VisualElement>(null, "unity-base-dropdown__item").ToList();
       // if (IsAnyVisible(items)){
         //   Debug.Log("Return true 3");
        //    return true;
       // }



        return false;
    }

    private bool IsAnyVisible(List<VisualElement> elements)
    {
        foreach (var e in elements)
        {
            if (e == null) continue;

            if (e.resolvedStyle.display == DisplayStyle.None) continue;
            if (e.worldBound.width <= 0f || e.worldBound.height <= 0f) continue;

            if (!IsVisibleInHierarchy(e)) continue;

            return true;
        }
        return false;
    }


    private bool IsVisibleInHierarchy(VisualElement element)
    {
        for (var cur = element; cur != null; cur = cur.parent)
        {
            if (cur.resolvedStyle.display == DisplayStyle.None)
                return false;

            // Дополнительно можно проверить opacity (нулевая непрозрачность делает невидимым)
            if (cur.resolvedStyle.opacity == 0f)
                return false;
        }
        return true;
    }


    private void HookPanel(VisualElement panel, System.Action<bool> onOpen, System.Action<bool> onClose , ref bool isCloseCreate)
    {
        if (panel == null) return;


        if (panel.panel != null)
            onOpen?.Invoke(true);

        panel.RegisterCallback<AttachToPanelEvent>(evt => onOpen?.Invoke(true));
        panel.RegisterCallback<DetachFromPanelEvent>(evt => onClose?.Invoke(true));
        isCloseCreate = true;
    }
}