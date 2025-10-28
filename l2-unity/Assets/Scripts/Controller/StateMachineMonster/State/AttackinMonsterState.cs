public class AttackinMonsterState : MonsterBase
{
    public AttackinMonsterState(MonsterStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter() { }
    public override void Exit() {

    }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        //Debug.Log("Handle Event Attack ");
        switch (evt)
        {
            case Event.READY_TO_ACT:
                    OnAttack();
                break;
            case Event.ARRIVED:
                //test event running to move exit use event ARRIVED
                var monsterEntity = (MonsterEntity)_stateMachine.Entity;
                monsterEntity.OnStopL2jMoving();
                OnAttack();
                break;
        }
    }

    private void OnAttack()
    {
       
        MonsterEntity entity = (MonsterEntity)_stateMachine.Entity;
        NetworkAnimationController nac = entity.GetAnimatorController();
        AnimationManager.Instance.PlayMonsterAnimation(entity.IdentityInterlude.Id, nac, AnimationNames.MONSTER_ATK01.ToString());
    }
}
