using UnityEngine;

public class MagicCastState : StateBase
{
    public MagicCastState(PlayerStateMachine stateMachine) : base(stateMachine) { }


    public override void Update()
    {
       
    }

    public override void HandleEvent(Event evt, object payload = null)
    {
        switch (evt)
        {
            case Event.DEAD:
                break;
        }
    }
}
