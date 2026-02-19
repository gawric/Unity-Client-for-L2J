using UnityEngine;

public abstract class MonsterBase
{
    protected MonsterStateMachine _stateMachine;

    public MonsterBase(MonsterStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void HandleEvent(Event evt) { }
}
