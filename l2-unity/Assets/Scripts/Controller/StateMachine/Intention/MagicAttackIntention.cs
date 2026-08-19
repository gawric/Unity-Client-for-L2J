using System.Threading.Tasks;
using UnityEngine;

public class MagicAttackIntention : IntentionBase
{

    public MagicAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        if(arg0.GetType() == typeof(MagicSkillUseDto))
        {
            MagicSkillUseDto useSkill = (MagicSkillUseDto)arg0;
            AnimationCombo anim = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId , useSkill.SkillLvl);
            float distance = VectorUtils.Distance2D(useSkill.AttackerPos, useSkill.TargetPos); 
            //Debug.Log("DISTANCE TO SERVER UNITY  " + distance);
           
            if (useSkill.SkillId != 1177) return;

            if (anim != null)
            {
                Rotate(IncomingPacketActions.Player, useSkill);
                Task.Run(() => WaitAndStart(useSkill, anim, distance));
            }
        }
    }

    private async void WaitAndStart(MagicSkillUseDto useSkill , AnimationCombo anim , float distance)
    {
        var timeout = Task.Delay(500);

        while (IncomingPacketActions.Player.IsTurnsAround())
        {
           // if (await Task.WhenAny(timeout) == timeout) break;
           Debug.LogWarning("Player turns around");
        }

        StartUseSkill(useSkill, anim, distance);
    }

    private async void StartUseSkill(MagicSkillUseDto useSkill , AnimationCombo anim , float distance)
    {
        Entity monster =await  IncomingPacketActions.GameWorld.GetEntityNoLock(useSkill.TargetId);
        IncomingPacketActions.Queue(() =>
        {
            IncomingPacketActions.SkillCombos.ExecutePlayerCombo(useSkill.TargetId, anim, useSkill.HitTime, distance, IncomingPacketActions.EffectSkills, useSkill.SkillId);
            var footerPosition = IncomingPacketActions.Player.GetPlayerPosition();
            // var bodyPosition = PlayerController.Instance.GetBodyPosition();
            var bodyPosition = IncomingPacketActions.Player.GetCollisionSelf(monster.transform);
            IncomingPacketActions.EffectSkills.ShowEffect(useSkill.SkillId, footerPosition, bodyPosition, useSkill.HitTime , monster);
        });
    }

    private async void Rotate(PlayerController controller , MagicSkillUseDto useSkill)
    {
        Entity entity = await IncomingPacketActions.GameWorld.GetEntityNoLock(useSkill.TargetId);
        controller.StartRotateFollow(entity);
    }

    public override void Exit() { }
    public override void Update()
    {

    }
}
