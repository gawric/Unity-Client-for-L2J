using System;
using UnityEngine;

public class SystemMessageUtils : MonoBehaviour
{


    public static void CancelEvent(SystemMessageWindow systemMessageWindow, Action okMethod , Action cancelMethod)
    {
        systemMessageWindow.OnButtonOk -= okMethod;
        systemMessageWindow.OnButtonClosed -= cancelMethod;
    }
}
