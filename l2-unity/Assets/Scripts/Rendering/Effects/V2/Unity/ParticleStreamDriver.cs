using UnityEngine;

/// <summary>
/// EffectPart adapter for one V2 particle stream. Replaces ParticleGroup playback.
/// </summary>
public sealed class ParticleStreamDriver : EffectPart, IParticleEmitterV2
{
    ParticleStreamRuntime _runtime;
    ParticleGroupAuthoring _authoring;
    float _emissionWindow = 1f;
    ParticleLifetimePolicy _lifetimePolicy = ParticleLifetimePolicy.Authored;
    bool _bound;
    bool _streamVisible = true;
    bool _destroying;
    bool _hasExternalEmissionWindow;

    public bool IsGpuDraw => _runtime != null && _runtime.IsGpuDraw;
    public ParticleLifetimePolicy LifetimePolicy => _lifetimePolicy;
    public bool HasFixedDuration => _authoring.hasFixedDuration;
    public bool IsComplete => _runtime != null && _runtime.IsComplete;
    public float AuthoredDuration => Mathf.Max(0.01f, _authoring.duration);

    public void Bind(ParticleGroupAuthoring authoring)
    {
        _authoring = authoring;
        _runtime ??= new ParticleStreamRuntime(this);
        _runtime.Bind(authoring);
        _runtime.SetVisible(_streamVisible);
        _bound = true;
    }

    public void SetStreamVisible(bool visible)
    {
        _streamVisible = visible;
        _runtime?.SetVisible(visible);
    }

    public override void Setup(EffectSettings settings, MagicCastData castData)
    {
        _settings = settings;
        _castData = castData;
        RecaptureAuthoring();
        ResolveEmissionWindow(settings, castData);
    }

    public override void PlayPart()
    {
        if (!_bound)
        {
            RecaptureAuthoring();
        }

        if (!_bound)
        {
            return;
        }

        _runtime ??= new ParticleStreamRuntime(this);
        _runtime.Bind(_authoring);
        _runtime.SetVisible(_streamVisible);
        float authoredLife = Mathf.Max(0.01f, _authoring.authoredParticleLife);
        if (authoredLife <= 0.011f)
        {
            authoredLife = _runtime.ReadParticleLifetime(_authoring.duration);
        }

        float particleLife = authoredLife;
        if (_authoring.stretchParticleLifeToWindow &&
            _emissionWindow > authoredLife + 1e-4f)
        {
            particleLife = _emissionWindow;
            _lifetimePolicy = ParticleLifetimePolicy.StretchParticleLifetimeToCast;
        }

        _authoring.authoredParticleLife = authoredLife;
        _authoring.targetParticleLife = particleLife;
        _runtime.Bind(_authoring);
        _runtime.Start(Now(), Mathf.Max(_emissionWindow, particleLife), particleLife);
    }

    public void SetEmissionWindow(float windowSeconds, EmitterStopMode stopMode)
    {
        _hasExternalEmissionWindow = true;
        _emissionWindow = Mathf.Max(0.01f, windowSeconds);
        _authoring.instantKillAtCastEnd = stopMode == EmitterStopMode.Kill;
        _lifetimePolicy = ParticleLifetimePolicy.EmissionWindowFromCast;
    }

    public override void StopPart()
    {
        if (_runtime == null)
        {
            return;
        }

        _runtime.Stop(_runtime.InstantKillAtCastEnd ? EmitterStopMode.Kill : EmitterStopMode.Drain);
    }

    void FixedUpdate()
    {
        if (!_streamVisible)
        {
            return;
        }

        _runtime?.Tick(Now());
    }

    void LateUpdate()
    {
        if (!_streamVisible)
        {
            return;
        }

        _runtime?.LateDraw();
    }

    void OnDisable()
    {
        if (_runtime == null)
        {
            return;
        }

        _runtime.Stop(_destroying ? EmitterStopMode.Kill : EmitterStopMode.Drain);
    }

    void OnDestroy()
    {
        _destroying = true;
        _runtime?.Stop(EmitterStopMode.Kill);
        _runtime?.Dispose();
        _runtime = null;
    }

    void RecaptureAuthoring()
    {
        ParticleGroup group = GetComponent<ParticleGroup>();
        if (group != null)
        {
            Bind(group.CaptureAuthoring());
            return;
        }

        ParticleSingle single = GetComponent<ParticleSingle>();
        if (single != null)
        {
            Bind(single.CaptureAuthoring());
        }
    }

    void ResolveEmissionWindow(EffectSettings settings, MagicCastData castData)
    {
        if (_hasExternalEmissionWindow)
        {
            _lifetimePolicy = ParticleLifetimePolicy.EmissionWindowFromCast;
            _emissionWindow = Mathf.Max(0.01f, _emissionWindow);
            return;
        }

        if (_authoring.hasFixedDuration)
        {
            _lifetimePolicy = ParticleLifetimePolicy.Authored;
            _emissionWindow = Mathf.Max(0.01f, _authoring.duration);
            return;
        }

        _lifetimePolicy = ParticleLifetimePolicy.EmissionWindowFromCast;
        float fallback = Mathf.Max(0.01f, _authoring.duration);
        _emissionWindow = EffectCastDurationResolver.Resolve(
            fallback,
            false,
            settings,
            castData,
            out _,
            out _);
        if (_emissionWindow < 0.01f)
        {
            _emissionWindow = fallback;
        }
    }
}
