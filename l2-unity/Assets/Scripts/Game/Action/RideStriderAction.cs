using UnityEngine;

public class RideStriderAction : L2Action
{
    public override void UseAction()
    {
        if (PlayerStateMachine.Instance == null)
            return;
        if (PlayerStateMachine.Instance.State == PlayerState.DEAD)
            return;
        PlayerStateMachine.Instance.ToggleRide();
    }
}
