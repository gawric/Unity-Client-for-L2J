using UnityEngine;

public class MagicCastShot : StateBase
{
    public MagicCastShot(PlayerStateMachine stateMachine) : base(stateMachine) { 
        Debug.Log(""); 
    }


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
