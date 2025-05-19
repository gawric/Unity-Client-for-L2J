using UnityEngine;

public class IdleMosterState : MonsterBase
{
    public IdleMosterState(MonsterStateMachine stateMachine) : base(stateMachine)
    {
        Debug.Log("IDLE State ");
    }

    public override void Enter()
    {

    }
    public override void Exit() { }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        var ent = (MonsterEntity)_stateMachine.Entity;
        var nac = _stateMachine.Entity.GetAnimatorController();

        if (nac == null) return;

        switch (evt)
        {
            case Event.READY_TO_ACT:
                break;
            case Event.ENTER_WORLD:
                AnimationManager.Instance.PlayMonsterAnimation(ent.IdentityInterlude.Id , nac, AnimationNames.MONSTER_WAIT.ToString());
                break;
            case Event.ARRIVED:
                //Debug.Log("MosterAnimation State Walk > start animation");
                AnimationManager.Instance.PlayMonsterAnimation(ent.IdentityInterlude.Id , nac, AnimationNames.MONSTER_WAIT.ToString());
                break;


        }
    }
}
