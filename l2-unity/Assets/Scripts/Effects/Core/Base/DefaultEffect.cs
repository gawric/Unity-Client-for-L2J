using UnityEngine;

public class DefaultEffect : BaseEffect
{
    private EffectSettings _settings;
    private MagicCastData _castData;
    // Метод для получения настроек из EffectManager
    public void Setup(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        _settings.lifeTime = castData.HitTime;
    }

    public override void Play()
    {
        if (_settings == null) _settings = new EffectSettings();

        Initialize();


        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);

        foreach (var part in parts)
        {
            part.Setup(_settings , _castData);
            part.PlayPart();
        }


        float fadeOutTime = Mathf.Max(0, _settings.lifeTime - _settings.hideTime);
        Invoke(nameof(BeginFadeOut), fadeOutTime);
        Destroy(gameObject, _settings.lifeTime);
    }

    private void BeginFadeOut()
    {
        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);
        foreach (var part in parts)
        {
            part.StopPart(); 
        }
    }

  
    public override void SetProgress(float t)
    {
    
    }
}
