using System;
using UnityEngine;
using UnityEngine.UIElements;

public class ProgressBarUtils
{
    //set current = 250 , max = 250 The program will be empty.
    public static void UpdatePb(VisualElement pbBg, VisualElement pbGauge, float bgWidth, float currentData, float maxData)
    {
        if (pbBg != null && pbGauge != null)
        {
            double hpRatio = (currentData / maxData);
            double barWidth = bgWidth * (1 - hpRatio);
            pbGauge.style.width = Convert.ToSingle(barWidth);
        }
    }
    //set current = 1 , max = 250 The program will be empty.
    public static void UpdatePbCurrentFirst(VisualElement pbBg, VisualElement pbGauge, float bgWidth, float currentData, float maxData)
    {
        if (pbBg != null && pbGauge != null)
        {
            double hpRatio = (currentData / maxData);
            double barWidth = bgWidth * hpRatio;
            pbGauge.style.width = Convert.ToSingle(barWidth);
        }
    }
}
