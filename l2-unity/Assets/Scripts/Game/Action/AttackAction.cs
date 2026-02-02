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

        if (TargetManager.Instance.HasTarget())
        {
            if (!PlayerEntity.Instance.IsAttack && PlayerStateMachine.Instance.State != PlayerState.DEAD)
            {
                TargetData target = TargetManager.Instance.Target;

                if (target != null && !target.IsDead())
                {
                    Debug.Log("Trying To Attack");

                    var l2jpos = target.Identity.GetL2jPos();
                    ClickAction sendPaket = CreatorPacketsUser.CreateActiont(target.Identity.Id, (int)l2jpos.x, (int)l2jpos.y, (int)l2jpos.z, 0);
                    bool enable = GameClient.Instance.IsCryptEnabled();
                    SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
                }
            }
        }
    }
}