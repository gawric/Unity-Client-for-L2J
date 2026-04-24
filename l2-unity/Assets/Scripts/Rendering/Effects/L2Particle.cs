using UnityEngine;

public class L2Particle : BaseEffect
{

    [SerializeField] private Vector3 _surfaceNormal;
    [SerializeField] private PooledEffect _pooledEffect;
    [SerializeField] private EffectPart[] _particleGroups;
    private EffectSettings _settings;
    private MagicCastData _castData;
    public PooledEffect PooledEffect { get { return _pooledEffect; } }
    public Vector3 SurfaceNormal { get { return _surfaceNormal; } set { _surfaceNormal = value; } }

    private void Awake()
    {
        _pooledEffect.ResetTimerCallback = () =>
        {
            ResetTimer();
        };
    }

    public override void Setup(EffectSettings settings, MagicCastData castData, Transform owner)
    {

        base.Setup(settings, castData, owner);

        _settings = settings;
        _castData = castData;


        if (castData != null)
        {
            //_settings.defaultLifeTime = castData.HitTime;
        }

    }


    public override void SetProgress(float normalizedTime)
    {
        throw new System.NotImplementedException();
    }

    public override void Play()
    {
        ResetTimer();
        DestoryEffect(_settings,  _castData);
    }

    public void ResetTimer()
    {
        if (_particleGroups == null || _particleGroups.Length == 0)
        {
            _particleGroups = GetComponentsInChildren<EffectPart>();
        }

        for (int i = 0; i < _particleGroups.Length; i++)
        {
            _particleGroups[i].Setup(_settings, _castData);

            if (_owner != null)
            {
                _particleGroups[i].OwnerTarget = _owner;
            }

            if(_settings == null)
            {
                Debug.Log("L2Particle>ResetTimer _settings is null");
                return ;
            }
            
            _particleGroups[i].FollowTarget = _settings.isFollowCaster ? _owner : null;

            if (!_settings.isFollowCaster)
            {
                _particleGroups[i].SurfaceNormal = _surfaceNormal;
            }

            _particleGroups[i].PlayPart();
        }
    }


}
