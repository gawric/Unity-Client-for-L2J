using UnityEngine;

public enum EffectPartType { Body, Footer, Aura , Fly , FlySub }

[RequireComponent(typeof(Renderer))]
public abstract class EffectPart : MonoBehaviour
{
    [Header("Настройки части")]
    public EffectPartType partType;

    protected Renderer targetRenderer;
    protected MaterialPropertyBlock propBlock;
    protected EffectSettings settings;
    protected MagicCastData _castData;
    protected float baseSize;
    protected const string SHADER_PARAMETR_ALPHA = "_Alpha";
    protected const string SHADER_PARAMETR_MAX_ALPHA = "_MaxAlpha";
    protected const string SHADER_PARAMETR_SPEED = "_Speed";

    protected const string SHADER_PARAMETR_RANGE_MAX_ALPHA = "_MaxAlpha";

    protected const string SHADER_PARAMETR_SCALE_START_SIZE = "_ScaleSizeStart";
    protected const string SHADER_PARAMETR_SCALE_END_SIZE = "_ScaleSizeEnd";
    protected const string SHADER_PARAMETR_SCALE_START_TIME = "_StartTimeScale";
    protected const string SHADER_PARAMETR_SCALE_END_TIME = "_EndTimeScale";

    protected const string SHADER_PARAMETR_START_ALPHA_TIMEV1 = "_StartAlphaTimeV1";
    protected const string SHADER_PARAMETR_END_ALPHA_TIMEV1 = "_EndAlphaTimeV1";
    protected const string SHADER_PARAMETR_START_ALPHA_TIMEV0 = "_StartAlphaTimeV0";
    protected const string SHADER_PARAMETR_END_ALPHA_TIMEV0 = "_EndAlphaTimeV0";

    public virtual void Initialize(EffectSettings settings, float baseSize)
    {
        this.settings = settings;
        this.baseSize = baseSize;

        targetRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();



        UpdateShaderFloat(SHADER_PARAMETR_ALPHA, 0);
    }


    protected void UpdateShaderFloat(string name, float value)
    {
        if (targetRenderer != null)
        {
            // Сначала получаем текущий блок из рендерера
            targetRenderer.GetPropertyBlock(propBlock);

            // Устанавливаем значение
            propBlock.SetFloat(name, value);

            // ПРИНУДИТЕЛЬНО отдаем его обратно
            targetRenderer.SetPropertyBlock(propBlock);
        }

    }

    public abstract void Setup(EffectSettings settings , MagicCastData castData);
    public abstract void PlayPart();
    public abstract void StopPart();
}