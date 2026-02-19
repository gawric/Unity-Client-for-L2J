using UnityEngine;

[CreateAssetMenu(fileName = "Effect1177Settings", menuName = "VFX/Settings/1177")]
public class Effect1177Settings : EffectSettings
{

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
