using System;
using System.Threading;
using UnityEditorInternal;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;


public class NewPhysicalSkillsState : AbstractAttackEvents
{
    public NewPhysicalSkillsState(PlayerStateMachine stateMachine) : 
        base(stateMachine.GetObjectId() ,
        SpecialAnimationNames.GetPhisicalSkillsAnimations(),
        stateMachine) { }

    public override void Enter()
    {
        base.Enter();
    }
    public override void HandleEvent(Event evt , object payload = null)
    {
        MagicSkillUseDto useSkill = GetPayload(payload);


        switch (evt)
        {
            case Event.READY_TO_ACT:

                AnimationCombo animComboAct = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                //not use bow atk
                //RotateFaceToMonster(_stateMachine.Player);
                SkillExecutor.Instance.ExecuteSkill(_stateMachine.Player, animComboAct, _events);
                // Only skills with a ready EffectDatabase entry (e.g. Power Strike = 3).
                if (useSkill.SkillId == 3)
                {
                    EffectManager.Instance.PlayEffectSyncedToSkillAnimation(
                        useSkill.SkillId,
                        _stateMachine.Player,
                        useSkill.HitTime,
                        animComboAct);
                }
                break;
            case Event.CANCEL:
                Debug.Log("NewPhysicalSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");

                break;
            case Event.APPLY_SELF_SKILL:
                Skillgrp skillgrp = useSkill.SkillGrp;
                //2013 scroll effect
                if (SetupDurationHelper.IsLongCastSkill(useSkill))
                {
                    AnimationCombo selfCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                    Entity entity = _stateMachine.Player;
                    int objectId = entity.Identity.Id;
                    SetupDurationHelper.SetupLongCastDurationIfHitTimeNot0(useSkill, objectId, entity, selfCombo);

                    SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events, isLong: true);
                } //2031 potion heal , 2011 haste potion
                else if (SetupDurationHelper.IsUsePotion(useSkill))
                {
                    Entity entity = _stateMachine.Player;
                    EffectManager.Instance.PlayEffect(useSkill.SkillId, entity.transform, entity.GetMagicCastData());
                }
                else
                {
                    AnimationCombo combo = new AnimationCombo("-1", new string[1] { skillgrp.GetAnimOperationType3() }, "");
                    SkillExecutor.Instance.ExecuteSkill(_stateMachine.Player, combo, _events);
                }


                break;
            case Event.APPLY_SOULSHOT_CHARGED:
                Debug.Log(
                    $"[SS_CHARGE_SM] State.APPLY_SOULSHOT_CHARGED state={_stateMachine.State} " +
                    $"intention={_stateMachine.Intention} running={_stateMachine.Player != null && _stateMachine.Player.Running}");
                Transform transform = _stateMachine.Player.GetWeaponTransform();
                _stateMachine.Player.IsSoulshotCharged = true;
                EffectManager.Instance.PlayEffect(useSkill.SkillId , transform);
                break;

            case Event.ARRIVED:
                // Arrived while charged / physical-skills — locomotion finished, go IDLE.
                Debug.Log(
                    $"[SS_CHARGE_SM] State.ARRIVED while PHYSICAL_SKILLS → IDLE " +
                    $"(arrived overrides charged)");
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerStateMachine.Instance.NotifyEvent(Event.ARRIVED);
                break;

        }
    }



    private MagicSkillUseDto GetPayload(object payload)
    {

        if (payload is MagicSkillUseDto useSkill)
        {
            return useSkill;
        }

        return null;
    }
    // Disabled with AttackTimingHelper.RotateFaceToMonster — clip pose only.
    // private void RotateFaceToMonster(Entity entity)
    // {
    //     Transform monster = PlayerEntity.Instance.Target;
    //     if (monster == null) return;
    //     RotationService.Instance.RotateTowards(entity.transform, monster.position, () =>
    //     {
    //         float monsterHeight = monster.GetComponent<Entity>().Appearance.CollisionHeight;
    //         Vector3 monsterFacePosition = monster.position + Vector3.up * (monsterHeight * 0.8f);
    //         Vector3 startPoint = entity.transform.position + Vector3.up * 1.5f;
    //         Vector3 lookDir = (monsterFacePosition - startPoint).normalized;
    //         float verticalAngle = Mathf.Asin(lookDir.y) * Mathf.Rad2Deg;
    //         float spineAngle = Mathf.Clamp(verticalAngle * 0.4f, -15f, 10f);
    //         Vector3 spineRotation = new Vector3(0, 0, spineAngle);
    //         float armAngle = Mathf.Clamp(verticalAngle * 0.3f, -20f, 10f);
    //         Vector3 armRotation = new Vector3(0, 0, armAngle);
    //         PlayerEntity playerEntity = (PlayerEntity)entity;
    //         playerEntity.SetProceduralSpinePose(spineRotation);
    //         playerEntity.SetProceduralRightUpperArmPose(armRotation);
    //     });
    // }
}