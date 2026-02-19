using UnityEngine;

public class DefaultEffect : BaseEffect
{
    private EffectSettings _settings;
    private MagicCastData _castData;

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        base.Setup(settings, castData, owner);
        _settings = settings;
        _castData = castData;

        if (castData != null)
        {
            _settings.defaultLifeTime = castData.HitTime;
        }
    }

    public override void Play()
    {
        Initialize();

        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);
        foreach (var part in parts)
        {
            part.Setup(_settings, _castData);
            part.PlayPart();
        }

        // Используем метод из базового класса (или где он у вас объявлен)
        DestoryEffect(_settings, _castData);
    }

    public override void SetProgress(float t) { }
}
