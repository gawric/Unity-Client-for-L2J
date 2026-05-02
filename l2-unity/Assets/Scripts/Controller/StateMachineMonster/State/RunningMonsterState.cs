using UnityEngine;

public class RunningMonsterState : MonsterBase
{

    public RunningMonsterState(MonsterStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter()
    {
       

    }
    public override void Exit() { 
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
                //DebugLineDraw.RemoveDrawLineDebug(_stateMachine.GetObjectId());
                break;
            case Event.CANCEL:
                Debug.Log("CANCEL Running MONSTER TO POINT!");
                monsterEntity = (MonsterEntity)_stateMachine.Entity;

                break;
            case Event.MOVE_TO:
                // Debug.Log("MosterAnimation State Walk > start animation");
                AnimationManager.Instance.PlayMonsterAnimation(_stateMachine.GetObjectId(), AnimationNames.MONSTER_RUN.ToString());
                break;
        }
    }
}
