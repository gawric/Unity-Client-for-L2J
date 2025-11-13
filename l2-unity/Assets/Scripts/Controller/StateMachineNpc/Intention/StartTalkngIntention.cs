public class StartTalkingIntention : NpcIntentionBase
{
    public StartTalkingIntention(NpcStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {

        if (arg0.GetType() == typeof(NpcHtmlMessage))
        {
            NpcHtmlMessage packet = (NpcHtmlMessage)arg0;
            Entity entity = _stateMachine.Entity;
            Entity player = PlayerEntity.Instance;

            StartRotate(entity, player);
            ShowHtmlDialog(packet);
        }
 
    }

    private void StartRotate(Entity entity , Entity player)
    {
        if (!MoveAllCharacters.Instance.IsMoving(entity.IdentityInterlude.Id))
        {
            MoveAllCharacters.Instance.AddRotate(entity.IdentityInterlude.Id, new RotateData(player, entity));
        }
    }
   
    private void ShowHtmlDialog(NpcHtmlMessage packet)
    {

       HtmlWindow.Instance.InjectToWindow(packet.Elements());
       HtmlWindow.Instance.Test2();

    }



    public override void Exit()
    {
       
    }
    public override void Update()
    {
      
    }

}
