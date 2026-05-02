using System;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class L2PopupWindow : L2Window
{
    protected void RegisterCloseWindowEvent(string closeButtonClass)
    {
        Button closeButton = (Button)GetElementByClass(closeButtonClass);
        if (closeButton == null)
        {
            Debug.LogWarning($"Cant find close button with className: {closeButtonClass}.");
            return;
        }

        ButtonClickSoundManipulator buttonClickSoundManipulator = new ButtonClickSoundManipulator(closeButton);
        closeButton.AddManipulator(buttonClickSoundManipulator);

        closeButton.RegisterCallback<MouseUpEvent>(evt =>
        {
            AudioManager.Instance.PlayUISound("window_close");
            HideWindow();
        });
    }

    protected void RegisterCloseWindowEventByName(string closeButtonName)
    {
        Button closeButton = (Button)GetElementById(closeButtonName);
        if (closeButton == null)
        {
            Debug.LogWarning($"Cant find close button with className: {closeButtonName}.");
            return;
        }

        ButtonClickSoundManipulator buttonClickSoundManipulator = new ButtonClickSoundManipulator(closeButton);
        closeButton.AddManipulator(buttonClickSoundManipulator);

        closeButton.RegisterCallback<MouseUpEvent>(evt =>
        {
            AudioManager.Instance.PlayUISound("window_close");
            HideWindow();
        });
    }

    public void RegisterClickWindowEvent(VisualElement windowEle, VisualElement dragEle)
    {
        if (windowEle != null)
        {
            windowEle.RegisterCallback<MouseDownEvent>(evt =>
            {

                BringToFront();
                ToolTipSimple.Instance.BringToFront();
            }, TrickleDown.TrickleDown);
        }

        if (dragEle != null)
        {
            dragEle.RegisterCallback<MouseDownEvent>(evt =>
            {
                BringToFront();
            }, TrickleDown.TrickleDown);
        }
    }

    public void UnRegisterClickWindowEvent(VisualElement windowEle, VisualElement dragEle)
    {
        if (windowEle != null)
        {
            windowEle.UnregisterCallback<MouseDownEvent>(evt =>
            {

                BringToFront();
                ToolTipSimple.Instance.BringToFront();
            }, TrickleDown.TrickleDown);
        }

        if (dragEle != null)
        {
            dragEle.UnregisterCallback<MouseDownEvent>(evt =>
            {
                BringToFront();
            }, TrickleDown.TrickleDown);
        }
    }
    public override void ShowWindow()
    {
        base.ShowWindow();
        BringToFront();
    }

    public override void HideWindow()
    {
        base.HideWindow();
    }

    public override void OnlyHideWindow()
    {
        base.OnlyHideWindow();
    }
    public override void BringToFront()
    {
        _windowEle.BringToFront();
    }

    public override void SendToBack()
    {
        _windowEle.SendToBack();
    }


    public void ShowWindowToCenter()
    {
        base.ShowWindow();
        OnCenterScreen(_root);
    }

    public void ShowWindowToCenterAndBringToFront()
    {
        base.ShowWindow();
        OnCenterScreen(_root);
        _windowEle.BringToFront();
    }

    public void ReplaceWindow(VisualTreeAsset template)
    {
        try
        {
            _root.Remove(_windowEle);
            _windowEle.RemoveManipulator(_mouseOverDetection);
            UnRegisterClickWindowEvent(_windowEle, null);

            _windowEle = template.Instantiate()[0];
            _mouseOverDetection = new MouseOverDetectionManipulator(_windowEle);
            _windowEle.AddManipulator(_mouseOverDetection);

            DisableEventOnOver(_windowEle);
            RegisterClickWindowEvent(_windowEle, null);

            if (_isWindowHidden)
            {
                _mouseOverDetection.Disable();
            }

            _isReg = false;
            _root.Add(_windowEle);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    public void RefreshOpacity(float opacity)
    {
        _windowEle.style.opacity = opacity;
    }


}
