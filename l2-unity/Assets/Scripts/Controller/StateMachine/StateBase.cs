public abstract class StateBase
{
    protected PlayerStateMachine _stateMachine;

    public StateBase(PlayerStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    protected MagicSkillUseDto GetPayload(object payload) => payload is MagicSkillUseDto useSkill ? useSkill : null;



    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void HandleEvent(Event evt , object payload = null) { }

    protected Animation ForRide(Animation grounded)
    {
        if (_stateMachine == null || !_stateMachine.Riding || grounded == null)
            return grounded;
        if (grounded.Value.StartsWith("run"))
            return AnimationNames.STRIDER_RUN;
        if (grounded.Value.StartsWith("walk"))
            return AnimationNames.STRIDER_WALK;
        return AnimationNames.STRIDER_WAIT;
    }
}