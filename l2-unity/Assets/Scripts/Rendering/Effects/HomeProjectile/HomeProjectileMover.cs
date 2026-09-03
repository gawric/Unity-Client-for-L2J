using UnityEngine;

public sealed class HomeProjectileMover : MonoBehaviour
{
    readonly HomeProjectilePath _path = new HomeProjectilePath();
    IEffectAttachmentResolver _attachmentResolver;

    EffectResolveContext _context;
    EffectAttachmentPoint _homeAttachmentPoint;
    Vector3 _homeOffset;
    float _speed;
    float _acceleration;
    float _maxSpeed;
    float _fadeStartDistance;
    float _fadeOutSeconds;
    float _arriveDistance;
    float _maxLifetime;
    bool _rotateToVelocity;
    bool _destroyOnArrive;
    BaseEffect _effect;
    HomeProjectileFlightCoordinator _coordinator;
    EffectPart[] _effectParts;
    Vector3 _startPosition;
    float _pathDistanceEstimate;
    float _traveledDistance;
    float _startedAt;
    bool _isLaunched;
    bool _arrivalFadeStarted;
    bool _pathCompleted;
    float _fadeCompleteAt;

    public void Launch(
        BaseEffect effect,
        EffectResolveContext context,
        CompositeHomeProjectileConfig config,
        HomeProjectileFlightCoordinator coordinator,
        ParticleGroupHomeFlightProfile groupProfile,
        IEffectAttachmentResolver attachmentResolver)
    {
        if (context == null || context.CasterTransform == null || config == null)
        {
            enabled = false;
            return;
        }

        _attachmentResolver = attachmentResolver;
        _effect = effect;
        _coordinator = coordinator;
        _context = context;
        _homeAttachmentPoint = config.homeAttachmentPoint;
        _homeOffset = config.homeOffset;
        _speed = Mathf.Max(0.01f, config.speed * Mathf.Max(0.01f, groupProfile.speedScale));
        _acceleration = Mathf.Max(0f, config.acceleration);
        _maxSpeed = Mathf.Max(0f, config.maxSpeed);
        _fadeStartDistance = Mathf.Max(0.01f, config.fadeStartDistance);
        _fadeOutSeconds = Mathf.Max(0.05f, config.fadeOutSeconds);
        _arriveDistance = Mathf.Max(0.01f, config.arriveDistance);
        _maxLifetime = config.maxLifetime;
        _arrivalFadeStarted = false;
        _pathCompleted = false;
        _fadeCompleteAt = -1f;
        _rotateToVelocity = config.rotateToVelocity;
        _destroyOnArrive = config.destroyOnArrive;
        _path.Configure(config, groupProfile);
        _path.caster = context.CasterTransform;
        _startPosition = transform.position;
        _pathDistanceEstimate = _path.Estimate(_startPosition, ResolveHomePosition());
        _traveledDistance = 0f;
        _startedAt = Time.time;
        _isLaunched = true;

        CacheEffectParts();
        DetachFromFollow();
        SyncOwnerWorldPosOverride();
        enabled = true;
    }

    void LateUpdate()
    {
        if (!_isLaunched)
        {
            return;
        }

        DetachFromFollow();

        if (_context == null || _context.CasterTransform == null)
        {
            Finish();
            return;
        }

        if (_maxLifetime > 0f && Time.time - _startedAt >= _maxLifetime)
        {
            Finish();
            return;
        }

        Vector3 target = ResolveHomePosition();
        Vector3 current = transform.position;
        float distanceToHome = Vector3.Distance(current, target);
        float distanceToCasterRoot = Vector3.Distance(current, _context.CasterTransform.position);
        float distance = Mathf.Min(distanceToHome, distanceToCasterRoot);

        if (_arrivalFadeStarted)
        {
            SyncOwnerWorldPosOverride();
            if (Time.time >= _fadeCompleteAt || distance <= _arriveDistance)
            {
                Finish();
            }

            return;
        }

        if (distance <= _fadeStartDistance)
        {
            BeginArrivalFade();
            return;
        }

        Vector3 next = ComputeNextPosition(current, target);
        Vector3 velocity = next - current;
        transform.position = next;
        SyncOwnerWorldPosOverride();

        if (_pathCompleted)
        {
            BeginArrivalFade();
            return;
        }

        if (_rotateToVelocity && velocity.sqrMagnitude > 0.000001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }
    }

    void BeginArrivalFade()
    {
        _arrivalFadeStarted = true;
        _fadeCompleteAt = Time.time + _fadeOutSeconds;
        SyncOwnerWorldPosOverride();

        if (_coordinator != null)
        {
            HideAndDestroyFlightObject();
            _coordinator.NotifyMoverArrived();
        }
        else if (_effect != null)
        {
            _effect.DestroyHomeArrivalImmediate();
        }
        else
        {
            Destroy(gameObject);
        }

        _isLaunched = false;
        enabled = false;
    }

    void HideAndDestroyFlightObject()
    {
        EffectPart[] parts = GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null)
            {
                parts[i].StopPart();
            }
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }

        ClearOwnerWorldPosOverride();
        Destroy(gameObject);
    }

    Vector3 ResolveHomePosition()
    {
        if (_context == null || _context.CasterTransform == null)
        {
            return _startPosition;
        }

        if (_attachmentResolver != null &&
            _attachmentResolver.Resolve(_homeAttachmentPoint, _context, out _, out Vector3 worldPosition))
        {
            if (_homeOffset.sqrMagnitude > 0.000001f)
            {
                worldPosition += _context.CasterTransform.TransformDirection(_homeOffset);
            }

            return worldPosition;
        }

        return _context.CasterTransform.position + _homeOffset;
    }

    Vector3 ComputeNextPosition(Vector3 current, Vector3 target)
    {
        _speed += _acceleration * Time.deltaTime;
        if (_maxSpeed > 0f)
        {
            _speed = Mathf.Min(_speed, _maxSpeed);
        }

        return _path.Step(
            _startPosition,
            current,
            target,
            _speed,
            Time.deltaTime,
            ref _traveledDistance,
            ref _pathDistanceEstimate,
            out _pathCompleted);
    }

    void CacheEffectParts()
    {
        _effectParts = GetComponentsInChildren<EffectPart>(true);
    }

    void DetachFromFollow()
    {
        if (transform.parent != null)
        {
            transform.SetParent(null, true);
        }
    }

    void SyncOwnerWorldPosOverride()
    {
        if (_effectParts == null)
        {
            return;
        }

        Vector3 worldPosition = transform.position;
        for (int i = 0; i < _effectParts.Length; i++)
        {
            EffectPart part = _effectParts[i];
            if (part != null)
            {
                part.SetOwnerWorldPosOverride(true, worldPosition);
            }
        }
    }

    void ClearOwnerWorldPosOverride()
    {
        if (_effectParts == null)
        {
            return;
        }

        for (int i = 0; i < _effectParts.Length; i++)
        {
            EffectPart part = _effectParts[i];
            if (part != null)
            {
                part.SetOwnerWorldPosOverride(false, Vector3.zero);
            }
        }
    }

    void Finish()
    {
        _isLaunched = false;
        enabled = false;
        ClearOwnerWorldPosOverride();

        if (!_destroyOnArrive || _arrivalFadeStarted)
        {
            return;
        }

        if (_effect != null)
        {
            _effect.CompleteHomeArrivalAndDestroy(_fadeOutSeconds);
            return;
        }

        Destroy(gameObject);
    }
}
