using UnityEngine;

public class IdleNpcState : NpcBase
{
    public IdleNpcState(NpcStateMachine stateMachine) : base(stateMachine)
    {

    }

    public override void Enter()
    {

        var ent = (NpcEntity)_stateMachine.Entity;
        if (!ent.IsDead())
        {
            //ent.OnWaitAnim();
        }

    }
    public override void Exit() { }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        Debug.Log("IdleMosterState");
    }
}
