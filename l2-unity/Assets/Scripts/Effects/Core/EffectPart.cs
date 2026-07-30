using UnityEngine;



//[RequireComponent(typeof(Renderer))]
public abstract class EffectPart : MonoBehaviour
{

    protected Renderer targetRenderer;
    protected MaterialPropertyBlock propBlock;
    protected EffectSettings _settings;
    protected MagicCastData _castData;
    protected float _baseSize;
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
        _settings = settings;
        _baseSize = baseSize;

        targetRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();



        UpdateShaderFloat(SHADER_PARAMETR_ALPHA, 0);
    }


    protected void UpdateShaderFloat(string name, float value)
    {
        if (targetRenderer == null)
        {
            return;
        }

        // L2Particle calls Setup/PlayPart without Initialize — create on demand.
        if (propBlock == null)
        {
            propBlock = new MaterialPropertyBlock();
        }

        // Сначала получаем текущий блок из рендерера
        targetRenderer.GetPropertyBlock(propBlock);

        // Устанавливаем значение
        propBlock.SetFloat(name, value);

        // ПРИНУДИТЕЛЬНО отдаем его обратно
        targetRenderer.SetPropertyBlock(propBlock);
    }


    public virtual Vector3 SurfaceNormal { get; set; }
    public virtual Transform FollowTarget { get; set; }
    public virtual Transform OwnerTarget { get; set; }

    private bool _ownerWorldPosOverrideActive;
    private Vector3 _ownerWorldPosOverride;
    private bool _shaderTargetWorldPosOverrideActive;
    private Vector3 _shaderTargetWorldPosOverride;
    private Transform _shaderTargetWorldPosFollowTransform;
    private Vector3 _shaderTargetWorldPosLocal;

    public void SetOwnerWorldPosOverride(bool active, Vector3 worldPosition)
    {
        _ownerWorldPosOverrideActive = active;
        if (active)
        {
            _ownerWorldPosOverride = worldPosition;
        }
    }

    protected Vector3 ResolveOwnerWorldPos()
    {
        if (_ownerWorldPosOverrideActive)
        {
            return _ownerWorldPosOverride;
        }

        return ResolveOwnerWorldPosDefault();
    }

    protected Vector3 ResolveOwnerWorldPosForShader(Material material)
    {
        if (material != null &&
            material.HasProperty(L2MaterialPropertyCopier.UseOwnerFromShaderTargetId) &&
            material.GetFloat(L2MaterialPropertyCopier.UseOwnerFromShaderTargetId) > 0.5f &&
            TryResolveShaderTargetWorldPos(out Vector3 targetWorldPos))
        {
            return targetWorldPos;
        }

        return ResolveOwnerWorldPos();
    }

    public void SetShaderTargetWorldPosOverride(bool active, Vector3 worldPosition, Transform followTransform = null)
    {
        _shaderTargetWorldPosOverrideActive = active;
        _shaderTargetWorldPosFollowTransform = null;
        if (active)
        {
            _shaderTargetWorldPosOverride = worldPosition;
            if (followTransform != null)
            {
                _shaderTargetWorldPosFollowTransform = followTransform;
                _shaderTargetWorldPosLocal = followTransform.InverseTransformPoint(worldPosition);
            }
        }
    }

    protected bool TryResolveShaderTargetWorldPos(out Vector3 worldPosition)
    {
        if (_shaderTargetWorldPosOverrideActive)
        {
            if (_shaderTargetWorldPosFollowTransform != null)
            {
                worldPosition = _shaderTargetWorldPosFollowTransform.TransformPoint(_shaderTargetWorldPosLocal);
                return true;
            }

            worldPosition = _shaderTargetWorldPosOverride;
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }

    protected Vector3 ResolveOwnerWorldPosDefault()
    {
        if (PlayerEntity.Instance != null)
        {
            return PlayerEntity.Instance.transform.position;
        }

        if (OwnerTarget != null)
        {
            return OwnerTarget.position;
        }

        if (FollowTarget != null)
        {
            return FollowTarget.position;
        }

        return transform.position + transform.forward;
    }

    public abstract void Setup(EffectSettings settings , MagicCastData castData);
    public abstract void PlayPart();
    public abstract void StopPart();


    protected float Now()
    {
#if UNITY_EDITOR
        float now = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
#else
        float now = Time.time;
#endif
        return now;
    }


}