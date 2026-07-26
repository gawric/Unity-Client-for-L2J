using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class L2TransparentTooltip : L2PopupWindow //todo: кажется не нужен отдельный, можно передавать через конфиг ассет. но подтянется ли он на лету? 
{

    private Label _title;
    private VisualElement _tooltipTarget;
    private Coroutine _updateStyleCoroutine;

    private static L2TransparentTooltip _instance;
    public static L2TransparentTooltip Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        _instance = null;
    }

    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Template/TransparentTooltip");
    }

    protected override IEnumerator BuildWindow(VisualElement root)
    {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        _title = GetLabelById("Title");
    }

    public void UpdateTooltip(string title, VisualElement target)
    {
        _windowEle.style.left = -1000;
        _windowEle.style.opacity = 0;

        _tooltipTarget = target;

        ShowWindow();

        if (_updateStyleCoroutine != null)
        {
            StopCoroutine(_updateStyleCoroutine);
        }

        _updateStyleCoroutine = StartCoroutine(UpdateToolTipCoroutine(title, target));
    }

    IEnumerator UpdateToolTipCoroutine(string title, VisualElement target)
    {
        while (true)
        {
            _title.text = title;

            yield return new WaitForEndOfFrame();

            _windowEle.style.left = target.worldBound.x;
            _windowEle.style.top = target.worldBound.y - _windowEle.resolvedStyle.height;

            _windowEle.style.opacity = 1;
        }
    }

    public void HideWindow(VisualElement exitElement = null)
    {
        if (exitElement == null || exitElement == _tooltipTarget)
        {
            base.HideWindow();

            if (_updateStyleCoroutine != null)
            {
                StopCoroutine(_updateStyleCoroutine);
                _updateStyleCoroutine = null;
            }

            _tooltipTarget = null;
        }
    }

    public void UpdateTooltipWorld(string title, Vector3 worldPos, Camera cam)
    {
        _windowEle.style.left = -1000;
        _windowEle.style.opacity = 0;

        _tooltipTarget = null;

        ShowWindow();

        if (_updateStyleCoroutine != null)
        {
            StopCoroutine(_updateStyleCoroutine);
        }

        _updateStyleCoroutine = StartCoroutine(UpdateToolTipWorldCoroutine(title, worldPos, cam));
    }

    private IEnumerator UpdateToolTipWorldCoroutine(string title, Vector3 worldPos, Camera cam)
    {
        while (true)
        {
            _title.text = title;

            yield return new WaitForEndOfFrame();

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            float panelHeight = _windowEle.panel.visualTree.worldBound.height;
            float x = screenPos.x;
            float y = panelHeight - screenPos.y;

            _windowEle.style.left = x - _windowEle.resolvedStyle.width / 2f;
            _windowEle.style.top = y - _windowEle.resolvedStyle.height;

            _windowEle.style.opacity = 1;
        }
    }
}
