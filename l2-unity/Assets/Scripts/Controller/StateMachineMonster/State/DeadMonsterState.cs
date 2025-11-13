public class DeadMosterState : MonsterBase
{
    public DeadMosterState(MonsterStateMachine stateMachine) : base(stateMachine) { }
    public override void Enter() {

    }
    public override void Exit() { }
    public override void Update() { }
    public override void HandleEvent(Event evt)
    {
        switch (evt)
        {
            case Event.DEAD:
                MonsterEntity entity = (MonsterEntity)_stateMachine.Entity;
                NetworkAnimationController nac = entity.GetAnimatorController();
                AnimationManager.Instance.PlayMonsterAnimation(entity.IdentityInterlude.Id , nac , AnimationNames.DEAD.ToString());
                break;

        }
    }
}
