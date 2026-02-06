using UnityEngine;

public abstract class BaseEffect : MonoBehaviour
{
    protected MaterialPropertyBlock propBlock;
    protected Renderer targetRenderer;

    public virtual void Initialize()
    {
        propBlock = new MaterialPropertyBlock();
        targetRenderer = GetComponent<Renderer>();
    }

    public abstract void SetProgress(float normalizedTime); // 0..1 для анимации
    public abstract void Play();
}