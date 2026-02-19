
public abstract class IntentionBase
{
    protected PlayerStateMachine _stateMachine;

    public IntentionBase(PlayerStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter(object arg0) { }
    public virtual void Exit() { }
    public virtual void Update() { }

    protected bool IsSuccessAttack(object arg0)
    {
        if (arg0 == null)
        {
            return false; 
        }

        return true;
    }
}