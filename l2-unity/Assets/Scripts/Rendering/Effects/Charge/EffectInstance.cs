using System;
using UnityEngine;
using UnityEngine.VFX;

[Serializable]
public class EffectInstance
{
    public Component component; 
    private Component _cachedSource;

    public void Activate(float now, float seed, Action<Component, float, float> applyParams)
    {
        if (component == null) return;
        component.gameObject.SetActive(true);

        if (_cachedSource == null)
        {
            _cachedSource = component is VisualEffect ? component : component.GetComponent<VisualEffect>();

            if (_cachedSource == null)
            {
                _cachedSource = component is Renderer ? component : component.GetComponent<Renderer>();
            }
        }

        if (_cachedSource != null)
        {
            applyParams?.Invoke(_cachedSource, now, seed);
        }
    }


    public void Deactivate()
    {
        if (component != null) component.gameObject.SetActive(false);
    }
}
