using System.Threading.Tasks;
using UnityEngine;

public class MagicAttackIntention : IntentionBase
{

    public MagicAttackIntention(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter(object arg0)
    {
        if(arg0.GetType() == typeof(MagicSkillUse))
        {
            MagicSkillUse useSkill = (MagicSkillUse)arg0;
            AnimationCombo anim = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId , useSkill.SkillLvl);
            float distance = VectorUtils.Distance2D(useSkill.AttackerPos, useSkill.TargetPos); 
            //Debug.Log("DISTANCE TO SERVER UNITY  " + distance);
           
            if (useSkill.SkillId != 1177) return;

            if (anim != null)
            {
                Rotate(PlayerController.Instance, useSkill);
                Task.Run(() => WaitAndStart(useSkill, anim, distance));
            }
        }
    }

    private async void WaitAndStart(MagicSkillUse useSkill , AnimationCombo anim , float distance)
    {
        var timeout = Task.Delay(500);

        while (PlayerController.Instance.IsTurnsAround())
        {
           // if (await Task.WhenAny(timeout) == timeout) break;
           Debug.LogWarning("Player turns around");
        }

        StartUseSkill(useSkill, anim, distance);
    }

    private async void StartUseSkill(MagicSkillUse useSkill , AnimationCombo anim , float distance)
    {
        Entity monster =await  World.Instance.GetEntityNoLock(useSkill.TargetId);
        EventProcessor.Instance.QueueEvent(() =>
        {
            SkillsManager.Instance.ExecutePlayerCombo(useSkill.TargetId, anim, useSkill.HitTime, distance, EffectSkillsmanager.Instance, useSkill.SkillId);
            var footerPosition = PlayerController.Instance.GetPlayerPosition();
            // var bodyPosition = PlayerController.Instance.GetBodyPosition();
            var bodyPosition = PlayerController.Instance.GetCollisionSelf(monster.transform);
            EffectSkillsmanager.Instance.ShowEffect(useSkill.SkillId, footerPosition, bodyPosition, useSkill.HitTime , monster);
        });
    }

    private async void Rotate(PlayerController controller , MagicSkillUse useSkill)
    {
        Entity entity = await World.Instance.GetEntityNoLock(useSkill.TargetId);
        controller.StartRotateFollow(entity);
    }

    public override void Exit() { }
    public override void Update()
    {

    }
}
