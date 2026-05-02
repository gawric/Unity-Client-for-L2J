using UnityEngine;

public abstract class NpcIntentionBase
{
    protected NpcStateMachine _stateMachine;

    public NpcIntentionBase(NpcStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter(object arg0) { }
    public virtual void Exit() { }
    public virtual void Update() { }
}
