using UnityEngine;
using static AttackingState;

public class AttackIntention : IntentionBase
{
    public AttackIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        Transform target = TargetManager.Instance.Target.Data.ObjectTransform;

        //Attack attackpacket = (Attack)arg0;

        //Debug.Log("AttackIntention++++ HP " + TargetManager.Instance.Target.Status.Hp);\\
        //TimeUtils.PrintFullTime("Attack Packet Time Intention " + _stateMachine.State);
        if (target == null)
        {
            //Debug.Log("Target is null, CANCEL event sent");
             _stateMachine.NotifyEvent(Event.CANCEL);
            return;
        }
        PlayerEntity.Instance.isAccesNewAtk = true;
        //default combo 2 attack
        //Attack packet send time 1200ms and UpdatePacket 600ms;
        if (PlayerEntity.Instance.CountAtk >= 2)
        {
            PlayerEntity.Instance.isStop = true;
        }
        PlayerEntity.Instance.CurrentAttackCount = 0;
        PlayerEntity.Instance.CountAtk = 2;
        PlayerEntity.Instance.RefreshRandomPAttack();

        if (_stateMachine.State != PlayerState.WAIT_RETURN)
        {
            _stateMachine.ChangeState(PlayerState.ATTACKING);

        }
        

        AttackIntentionType type = (AttackIntentionType)arg0;


        if (type != AttackIntentionType.TargetReached)
        {
            TargetManager.Instance.SetAttackTarget();
        }
        //Debug.Log("Attack Intention!!!!! comment code");
        if (type == AttackIntentionType.WaitReturn)
        {
            _stateMachine.ChangeState(PlayerState.ATTACKING);
           // _stateMachine.NotifyEvent(Event.WAIT_RETURN);
        }
        else
        {
            _stateMachine.ChangeState(PlayerState.IDLE);
            _stateMachine.NotifyEvent(Event.READY_TO_ACT);
        }

    }

    public override void Exit() { }
    public override void Update()
    {

    }
}