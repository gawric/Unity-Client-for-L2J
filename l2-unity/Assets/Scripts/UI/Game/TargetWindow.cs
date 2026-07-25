using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Security.Principal;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TargetWindow : L2Window {
    private Label _nameLabel;
    private VisualElement _HPBar;
    private VisualElement _HPBarBG;

    [SerializeField] private float _targetWindowMinWidth = 175.0f;
    [SerializeField] private float _targetWindowMaxWidth = 300.0f;

    private static TargetWindow _instance;
    public static TargetWindow Instance { get { return _instance; } }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else {
            Destroy(this);
        }
    }

    private void OnDestroy() {
        _instance = null;
    }

    protected override void LoadAssets() {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/TargetWindow");
    }

    protected override IEnumerator BuildWindow(VisualElement root) {
        InitWindow(root);

        yield return new WaitForEndOfFrame();

        var statusWindowDragArea = GetElementByClass("drag-area");
        DragManipulator drag = new DragManipulator(statusWindowDragArea, _windowEle);
        statusWindowDragArea.AddManipulator(drag);

        var horizontalResizeHandle = GetElementByClass("hor-resize-handle");
        HorizontalResizeManipulator horizontalResize = new HorizontalResizeManipulator(
            horizontalResizeHandle, _windowEle, _targetWindowMinWidth, _targetWindowMaxWidth);
        horizontalResizeHandle.AddManipulator(horizontalResize);

        var closeBtnHandle = (Button) GetElementById("CloseBtn");
        closeBtnHandle.AddManipulator(new ButtonClickSoundManipulator(closeBtnHandle));
        closeBtnHandle.RegisterCallback<MouseUpEvent>(evt => {
            TargetManager.Instance.ClearTarget();
        });

        horizontalResizeHandle.AddManipulator(horizontalResize);

        _nameLabel = (Label)GetElementById("TargetName");
        if(_nameLabel == null) {
            Debug.LogError("Target window target name label is null.");
        }

        _HPBar = GetElementById("HPBar");
        if(_HPBar == null) {
            Debug.LogError("Target window HPBar is null");
        }

        _HPBarBG = GetElementById("HPBarBG");
        if(_HPBarBG == null) {
            Debug.LogError("Target window HPBarBG is null");
        }

        _windowEle.style.position = Position.Absolute;
        _windowEle.style.left = Screen.width / 2f - _windowEle.resolvedStyle.width / 2f;
        _windowEle.style.top = 0;
    }

    private void FixedUpdate()
     {
        if(_windowEle == null) {
            return;
        }

        if(TargetManager.Instance.HasTarget())
        {
            ShowWindow();

            TargetData targetData = TargetManager.Instance.Target;
            if(_nameLabel != null) {
                _nameLabel.text = targetData.Identity.Name;
                _nameLabel.style.color = targetData.GetColorName();
            }
            if(_HPBarBG != null && _HPBar != null) {

                // 1. Проверяем MaxHp, чтобы избежать деления на ноль или NaN
                float maxHp = targetData.Stats.MaxHp;
                float hpRatio = (maxHp > 0) ? Mathf.Clamp01((float)targetData.Status.GetHp() / maxHp) : 0f;

                // 2. Получаем ширину подложки и проверяем, что она корректна (больше нуля и не NaN)
                float bgWidth = _HPBarBG.resolvedStyle.width;
                if (float.IsNaN(bgWidth) || bgWidth <= 0)
                {
                    bgWidth = _targetWindowMinWidth; // Временный дефолтный размер, пока UI не просчитался
                }

                // 3. Считаем финальную ширину полоски
                float barWidth = bgWidth * hpRatio;

                // 4. Если ширина 0, принудительно ставим 0, иначе Unity UI может сбросить её в "Auto" (весь экран)
                _HPBar.style.width = (barWidth > 0) ? barWidth : 0f;

                TooogleHide(targetData.Identity.IsHideHpBar);
            }
        } else {
            if (!_isWindowHidden) {
                AudioManager.Instance.PlayUISound("window_close");
            }
            HideWindow();
        }
    }
    private void TooogleHide(bool isHideHpBar)
    {
        if (isHideHpBar){
            HideBar();
        }
        else{
            ShowBar();
        }
    }
  
    private void HideBar()
    {
        _HPBarBG.style.display = DisplayStyle.None;
        _HPBar.style.display = DisplayStyle.None;
    }
    private void ShowBar()
    {
        _HPBarBG.style.display = DisplayStyle.Flex;
        _HPBar.style.display = DisplayStyle.Flex;
    }
}
