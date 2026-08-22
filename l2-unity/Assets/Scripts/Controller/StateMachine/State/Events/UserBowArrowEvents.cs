/// <summary>
/// CharInfo bow clip events. Same wiring as <see cref="AbstractAttackEvents"/>:
/// <see cref="IAnimationManager.GetAnimationEvents"/> then subscribe. Local player is not handled here.
/// One instance per <see cref="UserEntity"/> — created via VContainer factory, not singleton.
/// </summary>
public sealed class UserBowArrowEvents
{
    private readonly int _objectId;
    private readonly UserEntity _user;
    private readonly BowArrowVisual _bowArrow;
    private readonly IAnimationManager _animations;
    private AnimationEventsBase _events;
    private bool _isSubscribed;

    public UserBowArrowEvents(UserEntity user, BowArrowVisual bowArrow, IAnimationManager animations)
    {
        _user = user;
        _bowArrow = bowArrow;
        _animations = animations;
        _objectId = user != null && user.Identity != null ? user.Identity.Id : 0;
        ResolveEvents();
    }

    public void Enter()
    {
        if (_isSubscribed)
            return;

        ResolveEvents();
        if (_events == null)
            return;

        _isSubscribed = true;
        _events.OnAnimationStartLoadArrow += OnLoadArrow;
        _events.OnAnimationStartShoot += OnShoot;
    }

    public void Exit()
    {
        if (!_isSubscribed)
            return;

        _isSubscribed = false;
        if (_events == null)
            return;

        _events.OnAnimationStartLoadArrow -= OnLoadArrow;
        _events.OnAnimationStartShoot -= OnShoot;
        _events = null;
    }

    void ResolveEvents()
    {
        if (_events != null || _objectId <= 0)
            return;
        if (_animations == null)
            return;
        _events = _animations.GetAnimationEvents(_objectId);
    }

    void OnLoadArrow(string animName)
    {
        if (_bowArrow != null)
            _bowArrow.TryLoadArrow(_user, animName);
    }

    void OnShoot(string animName)
    {
        if (_bowArrow != null)
            _bowArrow.TryShoot(_user, animName);
    }
}
