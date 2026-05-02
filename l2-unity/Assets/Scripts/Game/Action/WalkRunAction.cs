using UnityEngine;

public class WalkRunAction : L2Action
{
    public WalkRunAction() : base() { }

    // Local action
    public override void UseAction()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.DEAD)
        {
            return;
        }
        Debug.Log("Нужно создать пакет доя обработки RequestActionUse WalkRun");
        //GameClient.Instance.ClientPacketHandler.RequestActionUse((int)ActionType.WalkRun);
    }
}