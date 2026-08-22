using UnityEngine;
using static AttackingState;

public class AttackAction : L2Action
{
    public AttackAction() : base() { }

    //backup
    // Local action
    //public override void UseAction()
    // {
    //    Debug.LogWarning("Use attack action.");

    //   if (TargetManager.Instance.HasTarget())
    //   {
    //      if (!PlayerEntity.Instance.IsAttack && PlayerStateMachine.Instance.State != PlayerState.DEAD)
    //       {
    //          var target = PlayerEntity.Instance.GetTargetEntity();
    //           if (target != null && !target.IsDead())
    //         {
    //              Debug.LogWarning("Trying To Attack");
    //              ClickManager.Instance.OnClickOnEntity();
    //         }
    //      }
    //  }
    // }

    public override void UseAction()
    {
        Debug.Log("Use attack action.");

        if (IncomingPacketActions.Targets.HasTarget())
        {
            if (!PlayerEntity.Instance.IsAttack && PlayerStateMachine.Instance.State != PlayerState.DEAD)
            {
                TargetData target = IncomingPacketActions.Targets.Target;

                if (target != null && !target.IsDead())
                {
                    Entity entity = target.GetEntity();
                    if (entity is MonsterEntity)
                    {
                        Debug.Log("Trying To Attack");
                        // Melee attack intent (hotkey) — red while closing distance.
                        IncomingPacketActions.Targets.SetAttackTarget();
                    }

                    var l2jpos = target.Identity.GetL2jPos();
                    IncomingPacketActions.Game.Send(new ClickActionCommand(target.Identity.Id, (int)l2jpos.x, (int)l2jpos.y, (int)l2jpos.z, 0));
                }
            }
        }
    }
}