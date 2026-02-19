using UnityEngine;

public abstract class BaseEffect : MonoBehaviour
{
    protected MaterialPropertyBlock propBlock;
    protected Renderer targetRenderer;
    [SerializeField] protected Transform _owner;
    public virtual void Initialize()
    {
        propBlock = new MaterialPropertyBlock();
        targetRenderer = GetComponent<Renderer>();
    }

    public abstract void SetProgress(float normalizedTime); 
    public abstract void Play();

  
    public virtual void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        _owner = owner;
    }

    public void DestoryEffect(EffectSettings settings, MagicCastData castData = null)
    {
        float fadeOutTime = Mathf.Max(0, settings.defaultLifeTime - settings.hideTime);

        Invoke(nameof(BeginFadeOut), fadeOutTime);
        Destroy(gameObject, settings.defaultLifeTime);
    }

    protected void BeginFadeOut()
    {
        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);

        foreach (var part in parts)
        {
            part.StopPart();
        }
    }

}