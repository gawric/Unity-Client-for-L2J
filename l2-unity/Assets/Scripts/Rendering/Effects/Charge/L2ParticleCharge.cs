using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

public class L2ParticleCharge : BaseEffect
{
    [SerializeField] private BaseEffectGroup[] _particleGroups;
    private EffectSettings _settings;
    private MagicCastData _castData;


    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {
        base.Setup(settings, castData, owner);
        _settings = settings;
        _castData = castData;


        if (_particleGroups == null || _particleGroups.Length == 0)
            _particleGroups = GetComponentsInChildren<BaseEffectGroup>(true);


        foreach (var group in _particleGroups)
            group.Setup(settings, castData);
    }

    public override void Play()
    {
        ResetTimer();
        DestoryEffect(_settings, _castData);
    }


    public override void SetProgress(float normalizedTime)
    {
        Debug.Log("L2ParticleCharge>SetProgress: не реализовано");
        //foreach (var group in _particleGroups)
           // group.SetProgress(normalizedTime);
    }

    public void ResetTimer()
    {

        float duration = (_settings != null) ? _settings.defaultLifeTime : 0.5f;

        foreach (var group in _particleGroups)
        {
            // Используем интерфейс IWeaponEffect, о котором говорили ранее
            if (group is IWeaponEffect weaponEffect)
            {
                weaponEffect.SetWeapon(_owner);
            }

  
            group.ResetTimer(0.95f, _owner != null ? _owner.transform : _owner);
        }
    }


}
