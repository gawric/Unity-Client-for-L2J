public class WalkingMonsterState : MonsterBase
{
    public WalkingMonsterState(MonsterStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {

    }
    public override void Exit() {
        //Debug.Log("EXIT WALKING MONSTER!!!!!!!!!!!!!");
       // _stateMachine.MoveMonster.StopRotateObject();
    }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        MonsterEntity monsterEntity = (MonsterEntity)_stateMachine.Entity;
        NetworkAnimationController nac = monsterEntity.GetAnimatorController();

        switch (evt)
        {
            case Event.ARRIVED:
                _stateMachine.ChangeState(MonsterState.IDLE);
                _stateMachine.NotifyEvent(Event.ARRIVED);
                DebugLineDraw.RemoveDrawLineDebug(monsterEntity.IdentityInterlude.Id);
                break;
            case Event.CANCEL:
                _stateMachine.ChangeState(MonsterState.IDLE);
                _stateMachine.NotifyEvent(Event.ARRIVED);
                DebugLineDraw.RemoveDrawLineDebug(monsterEntity.IdentityInterlude.Id);
                break;
            case Event.MOVE_TO:
               // Debug.Log("MosterAnimation State Walk > start animation");
                AnimationManager.Instance.PlayMonsterAnimation(monsterEntity.IdentityInterlude.Id , nac, AnimationNames.MONSTER_WALK.ToString());
                break;

        }
    }
}
