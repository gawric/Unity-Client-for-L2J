using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class AbstractButtonEvents
{
    private bool[] _isButtonsSubscribed;
    private Button[] _buttons;
    private EventCallback<ClickEvent>[] _callbacks;

    private bool[] _isInsideButtonsSubscribed;
    private Button[] _insideButtons;
    private EventCallback<ClickEvent>[] _insideCallbacks;
    protected bool GetStatusCallBack(int index){

        return GetStatus(_isButtonsSubscribed, index);
    } 

    protected bool GetStatusInsideCallBack(int index)
    {
        return GetStatus(_isInsideButtonsSubscribed, index);
    }


    protected void RegisterCallBackAllButtons(Button[] buttons , EventCallback<ClickEvent>[] callbacks)
    {
        _buttons = buttons;
        _callbacks = callbacks;
        _isButtonsSubscribed = new bool[_buttons.Length];

        Register(_buttons, _callbacks, _isButtonsSubscribed);
    }

    protected void RegisterInsideCallBackAllButtons(Button[] buttons, EventCallback<ClickEvent>[] callbacks)
    {

        ClearDataInside();

        _insideButtons = buttons;
        _insideCallbacks = callbacks;
        _isInsideButtonsSubscribed = new bool[_buttons.Length];

        Register(_insideButtons, _insideCallbacks, _isInsideButtonsSubscribed);

    }

    private void ClearDataInside()
    {
        _insideButtons = null;
        _insideCallbacks = null;
        _isInsideButtonsSubscribed = null;
    }

    protected void UnregisterCallBackAllButtons()
    {
        Unregister(_buttons, _callbacks, _isButtonsSubscribed);
    }

    protected void UnregisterInsideCallBackAllButtons()
    {
        Unregister(_insideButtons, _insideCallbacks, _isInsideButtonsSubscribed);
    }

    protected void UnregisterCallBackButton(ref bool isSubscrible , Button button , EventCallback<ClickEvent> callback)
    {
        if (isSubscrible)
        {
            if (button != null & callback != null) button?.UnregisterCallback(callback);
            isSubscrible = false;
        }
    }

    protected EventCallback<ClickEvent> SubscribeOnButton(ref bool isSubscrible , Button button , EventCallback<ClickEvent> callback)
    {
        if (button != null && !isSubscrible)
        {
                button.RegisterCallback(callback);
                isSubscrible = true;
        }

        return callback;
    }


    private void Register(Button[] buttons, EventCallback<ClickEvent>[] callbacks, bool[] isButtonsSubscribed)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            EventCallback<ClickEvent> callback = callbacks[i];

            if (callback != null)
            {
                Button button = buttons[i];
                bool isSubscrible = isButtonsSubscribed[i] = false;
                SubscribeOnButton(ref isSubscrible, button, callback);
            }

        }
    }

    private void Unregister(Button[] buttons, EventCallback<ClickEvent>[] callbacks, bool[] isButtonsSubscribed)
    {
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                EventCallback<ClickEvent> callback = callbacks[i];
                Button button = buttons[i];

                if (callback != null & button != null)
                {
                    var isSubscrible = isButtonsSubscribed[i];
                    UnregisterCallBackButton(ref isSubscrible, button, callback);
                }

            }
        }
    }

    private bool GetStatus(bool[] _isButtonsSubscribed, int index)
    {
        if (_isButtonsSubscribed != null)
        {
            if (ArrayUtils.IsValidIndexArray(_isButtonsSubscribed, index)) return _isButtonsSubscribed[index];
        }

        return false;
    }
}
