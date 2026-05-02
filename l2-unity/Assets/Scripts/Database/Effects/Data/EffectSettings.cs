using System.Collections.Generic;
using UnityEngine;

[System.Serializable] 
public class ScaleStep
{
    [Range(0, 100)] public float timePercent; 
    public float scalePercent;               
}


[System.Serializable] 
public abstract class EffectSettings: ScriptableObject 
{
    [Header("Общие настройки")]
    public float defaultLifeTime = 3.0f;

    [Tooltip("Если включено, эффект будет перемещаться вместе с персонажем")]
    public bool isFollowCaster = false;

    [Header("Настройки появления (Fade)")]
    public float showTime = 1.0f;
    public float hideTime = 1.0f;
    public float maxAlpha = 0.3f;
    public float stopAlphaShow = 0.3f;

    [Header("Настройки появления Alpa footer")]
    public float startAlphaTimeV1 = 0.08f;
    public float endAlphaTimeV1 = 0.5f;
    public float startAlphaTimeV0 = 0.6f;
    public float endAlphaTimeV0 = 1f;
    public float maxAlphaFooter = 0.5f;

    [Header("Вращение и Шейдер")]
    public float speedRotateMin = 23f;
    public float speedRotateMax = 27f;

}
