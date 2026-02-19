using UnityEngine;

public abstract class MonsterIntentionBase 
{
    protected MonsterStateMachine _stateMachine;

    public MonsterIntentionBase(MonsterStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter(object arg0) { }
    public virtual void Exit() { }
    public virtual void Update() { }
}
