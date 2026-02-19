
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

using static L2Slot;




public class QuantityInput : L2PopupWindow
{
    private static QuantityInput _instance;
    private object _parentWindow;
    private VisualElement _root;
    private TextField _userInput;

    public static QuantityInput Instance { get { return _instance; } }
    public event Action<string> OnButtonOk; 
    public event Action OnButtonClose;
    private int _count;

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



    protected override void LoadAssets()
    {
        _windowTemplate = LoadAsset("Data/UI/_Elements/Game/Calculator/QuantityInput");
    }



    protected override IEnumerator BuildWindow(VisualElement root)
    {


        InitWindow(root);

        _userInput = (TextField)GetElementById("UserInputField");
        VisualElement inputBg = (VisualElement)GetElementById("UserInputBg");
        Button close = (Button)GetElementById("CloseButton");
        Button success = (Button)GetElementById("SuccessButton");


        close.RegisterCallback<ClickEvent>(OnClickClose);
        success.RegisterCallback<ClickEvent>(OnClickSuccess);
        RegisterClickWindowEvent(_windowEle, null);


        RegisterAllNumbers();

        _userInput.RegisterValueChangedCallback(evt => OnValueChanged(evt));
        _userInput.RegisterCallback<ClickEvent>((evt) => OnClick(evt, inputBg));

        OnCenterScreen(root);
        yield return new WaitForEndOfFrame();

        
    }

    private void RegisterAllNumbers()
    {
        for(int i = 0; i < 10; i++)
        {
            var btn = (Button)GetElementById("Button"+i);
            btn.RegisterCallback<ClickEvent>(OnPlus);
        }


        Button buttonBs = (Button)GetElementById("ButtonBs");
        buttonBs.RegisterCallback<ClickEvent>(Back);
        Button buttonC = (Button)GetElementById("ButtonC");
        buttonC.RegisterCallback<ClickEvent>(OnClear);
        Button buttonALL = (Button)GetElementById("ButtonALL");
        buttonALL.RegisterCallback<ClickEvent>(OnAll);

    }


    private void OnPlus(ClickEvent evt)
    {
        Button clickedElement = evt.currentTarget as Button;
        string number = clickedElement.name.Replace("Button", "");
        if (_userInput.value.Length > 20) return;
        if (long.TryParse(number, out _))
        {
            string value1 = ToolTipsUtils.ConvertPriceToNormal(_userInput.value + number);
            long numberInt = long.Parse(value1);
            _userInput.value = ToolTipsUtils.ConvertToPrice(numberInt);
        }
    }

    private void OnClear(ClickEvent evt)
    {
        _userInput.value = 0.ToString();
    }

    private void Back(ClickEvent evt)
    {
        string value1 = ToolTipsUtils.ConvertPriceToNormal(_userInput.value);
        if(value1.Length == 1) _userInput.value = "0";
        if (value1.Length > 0)
        {
            string modifiedString = value1.Substring(0, value1.Length - 1);
            _userInput.value = modifiedString;
        }

    }

    private void OnAll(ClickEvent evt)
    {
        if (_count > 0)
        {
            _userInput.value = _count.ToString();
        }
    }


    private void OnClick(ClickEvent evt ,  VisualElement inputBg)
    {
        if (!inputBg.ClassListContains("calculator_field_click_bg"))
        {
            inputBg.AddToClassList("calculator_field_click_bg");
        }
    }

    private void OnClickSuccess(ClickEvent evt)
    {
        string value = ToolTipsUtils.ConvertPriceToNormal(_userInput.value);
        OnButtonOk?.Invoke(value);
        base.HideWindow();
    }



    private void OnClickClose(ClickEvent evt)
    {
        OnButtonClose?.Invoke();
        base.HideWindow();
    }


 

  


    private void OnValueChanged(ChangeEvent<string> evt)
    {

        _userInput.UnregisterValueChangedCallback(OnValueChanged);

        if (evt.newValue.Length == 0 & evt.previousValue.Length >= 1)
        {
            _userInput.SetValueWithoutNotify("0");
            _userInput.RegisterValueChangedCallback(OnValueChanged);
            return;
        }
        

        string digitsOnly = ToolTipsUtils.ConvertPriceToNormal(evt.newValue);
        if (digitsOnly.Length > 20) return;
        if (string.IsNullOrEmpty(digitsOnly) || !long.TryParse(digitsOnly, out long newValue))
        {
            _userInput.SetValueWithoutNotify(evt.previousValue);
            _userInput.RegisterValueChangedCallback(OnValueChanged);
            return;
        }

        digitsOnly = digitsOnly.Trim();

        string formattedValue = ToolTipsUtils.ConvertToPrice(long.Parse(digitsOnly));
        
        _userInput.SetValueWithoutNotify(formattedValue);

        if (formattedValue.Length > 1) SetCursorToEnd(formattedValue);
        _userInput.RegisterValueChangedCallback(OnValueChanged);
    }

    private void SetCursorToEnd(string formattedValue)
    {
        _userInput.cursorIndex = formattedValue.Length + 1;
    }

    public void ShowWindow(int  allCount)
    {
        _count = allCount;
        base.ShowWindow();
        OnCenterScreen(_root);
        _userInput.value = "0";
    }




    private void OnDestroy()
    {
        _instance = null;
    }

}
