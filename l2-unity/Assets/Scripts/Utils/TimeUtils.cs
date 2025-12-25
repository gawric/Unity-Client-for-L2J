using System;
using UnityEngine;

public class TimeUtils
{
    public static float ConvertMsToSec(float milliseconds)
    {
        return (float) milliseconds / 1000.0f;
    }

    public static float ConvertSecToMs(float sec)
    {
        return (float)sec * 1000.0f;
    }

    public static string GetCurrentFullTime()
    {

        DateTime now = DateTime.Now;
        string formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        return formattedTime;
    }

   
   public static string FormatTime(float seconds)
   {
       if (seconds < 0)
       {
                return "";
       }

       if (seconds < 60)
       {
                return $"{Mathf.RoundToInt(seconds)} Cекунд";
       }

       float minutes = seconds / 60f;
       if (minutes < 2f)
       {
            return $"{minutes:F1}".Replace(',', '.') + " Минуты";
        }

       if (minutes < 60f)
       {
                return $"{Mathf.RoundToInt(minutes)} Минут";
       }

       float hours = minutes / 60f;
       if (hours < 24f)
       {
           return $"{Mathf.RoundToInt(hours)} Часов";
       }

       float days = hours / 24f;
        return $"{Mathf.RoundToInt(days)} Дней";
    }
}
