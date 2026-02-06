using System.Collections.Generic;
using UnityEngine;

public class ScaleStep
{
    [Range(0, 100)] public float timePercent; // Когда менять размер (в % от жизни)
    public float scalePercent;                // На сколько менять (в % от начального)
}

[System.Serializable]
public class EffectSettings
{
    [Header("Общие настройки")]
    public float lifeTime = 3.0f; 

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

    [Header("Случайные смещения (Random)")]
    public bool useRandomPosition = true;
    public float minZ = 0f, maxZ = -0.02f;
    public float minY = 0f, maxY = -0.04f;


    [Header("Смещения (Offsets)")]
    public float footerYOffset = 0.2f;
    public float bodyYOffset = 0.15f;

    [Header("Размеры (Sizes)")]
    public float defaultFooterSize = 23f;
    public float defaultBodySize = 10f;

    [Header("Анимация масштаба")]
    public float scaleSizeStart = 23f;
    public float scaleSizeEnd = 15f;
    public float startTimeScale = 0.6f;
    public float endTimeScale = 1f;

    [Header("Aura Settings")]
    public float auraDefaultSize = 0.1f; // Твой defaulAuratSizeXYZ
    public float auraYOffset = 0.019f;    // Результат твоего GetGround()

    [Header("Fly Settings")]
    public float flyJumpHeight = 0.08f; // Высота подъема
    public float flyJumpSpeed = 1f;  // Скорость подъема
    public float flyAlphaThreshold = 0.3f; // Порог для запуска под-эффекта

}
