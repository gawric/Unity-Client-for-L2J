using UnityEngine;

public class FollowIntention : IntentionBase
{
    PlayerStateMachine stateMachine;
    public FollowIntention(PlayerStateMachine stateMachine) : base(stateMachine) {
        this.stateMachine = stateMachine;
    }

    public override void Enter(object arg0)
    {

    }

    private bool checkDist(Vector3 playerPosition , Vector3 _targetPosition, float stopAtRange)
    {
        var _flatTransformPos = new Vector3(playerPosition.x, 0, playerPosition.z);
        var test = new Vector3(_targetPosition.x, 0, _targetPosition.z);
        var distance = Vector3.Distance(_flatTransformPos, test);
        float roundedDistance = Mathf.Floor(distance * 10) / 10;

        if (roundedDistance == stopAtRange) return false;
        return roundedDistance > stopAtRange;
    }
 
    public override void Exit() { }
    public override void Update() { }
}