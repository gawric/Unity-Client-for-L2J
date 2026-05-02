using UnityEngine;

public abstract class NpcBase
{
    protected NpcStateMachine _stateMachine;

    public NpcBase(NpcStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void HandleEvent(Event evt) { }
}