using System.Threading;
using UnityEngine;


public class NewPhysicalSkillsState : AbstractAttackEvents
{
    public NewPhysicalSkillsState(PlayerStateMachine stateMachine) : 
        base(stateMachine.GetObjectId() ,
        SpecialAnimationNames.GetPhisicalSkillsAnimations(),
        stateMachine) { }

    public override void Enter()
    {
   
    }
    public override void HandleEvent(Event evt , object payload = null)
    {
        MagicSkillUse useSkill = GetPayload(payload);
        switch (evt)
        {
            case Event.READY_TO_ACT:

                AnimationCombo animComboAct = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                //not use bow atk
                //RotateFaceToMonster(_stateMachine.Player);
                SkillExecutor.Instance.ExecuteSkill(_stateMachine.Player, animComboAct, _events);
                break;
            case Event.CANCEL:
                Debug.Log("NewPhysicalSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");

                break;
            case Event.APPLY_SELF_SKILL:
                Skillgrp skillgrp = useSkill.SkillGrp;
                AnimationCombo combo = new AnimationCombo("-1", new string[1] { skillgrp.GetAnimOperationType3() }, "");
                SkillExecutor.Instance.ExecuteSkill(_stateMachine.Player, combo , _events);
                break;
            case Event.APPLY_SOULSHOT_CHARGED:
                Transform transform = _stateMachine.Player.GetWeaponTransform();
                EffectManager.Instance.PlayEffect(useSkill.SkillId , transform);
                break;

        }
    }


    private MagicSkillUse GetPayload(object payload)
    {

        if (payload is MagicSkillUse useSkill)
        {
            return useSkill;
        }

        return null;
    }
    private void RotateFaceToMonster(Entity entity)
    {
        Transform monster = PlayerEntity.Instance.Target;
        if (monster == null) return;


        RotationService.Instance.RotateTowards(entity.transform, monster.position, () =>
        {
            float monsterHeight = monster.GetComponent<Entity>().Appearance.CollisionHeight;
            Vector3 monsterFacePosition = monster.position + Vector3.up * (monsterHeight * 0.8f);

            Vector3 startPoint = entity.transform.position + Vector3.up * 1.5f;
            Vector3 lookDir = (monsterFacePosition - startPoint).normalized;
            float verticalAngle = Mathf.Asin(lookDir.y) * Mathf.Rad2Deg;

            // --- НАСТРОЙКА СПИНЫ ---
            // Берем 40% от общего угла для естественности
            float spineAngle = Mathf.Clamp(verticalAngle * 0.4f, -15f, 10f);
            Vector3 spineRotation = new Vector3(0, 0, spineAngle);

            // --- НАСТРОЙКА РУКИ ---
            // Добавляем еще 30% наклона именно для руки, чтобы она била ниже
            float armAngle = Mathf.Clamp(verticalAngle * 0.3f, -20f, 10f);
            Vector3 armRotation = new Vector3(0, 0, armAngle);
            // ВНИМАНИЕ: Если рука крутится не туда, проверьте ось (возможно, нужна X вместо Z)

            // 4. Применяем через ваш PlayerEntity и SpineProceduralController
            PlayerEntity playerEntity = (PlayerEntity)entity;

            // Применяем к позвоночнику (вы уже настроили это в SetProceduralPose)
            playerEntity.SetProceduralSpinePose(spineRotation);
            playerEntity.SetProceduralRightUpperArmPose(armRotation);
        });

        //DebugLineDraw.ShowDrawLineDebugNpc(-1, startPoint, lookDir * 3f, Color.black);
    }
}