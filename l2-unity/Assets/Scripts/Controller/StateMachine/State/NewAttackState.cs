
using System.Threading;
using UnityEngine;



public class NewAttackState : AbstractAttackEvents
{

  
    public NewAttackState(PlayerStateMachine stateMachine) :
        base(stateMachine.GetObjectId() , 
        SpecialAnimationNames.GetSpecialsAttackAnimations() , 
        stateMachine)
    {

    }


    public override void Update()
    {

    }

    public override void HandleEvent(Event evt , object payload = null)
    {
        switch (evt)
        {
            case Event.READY_TO_ACT:
                Debug.Log("Attack Sate to Intention> начало новой atk пришел запрос от сервера");
                RotateFaceToMonster(_stateMachine.Player);
                PlayerEntity.Instance.RefreshRandomPAttack();
                Animation random = PlayerEntity.Instance.RandomName;
                AnimationManager.Instance.PlayAnimationTrigger(_stateMachine.GetObjectId() , random.ToString());

                break;
            case Event.CANCEL:
                Debug.Log("Attack Sate to Intention> Отмена скорее всего запрос пришел из ActionFaild");
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE);
                PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                PlayerEntity.Instance.LastAtkAnimation = null;
                break;

        }
    }








    private void RotateFaceToMonster(Entity entity)
    {
        Transform monster = PlayerEntity.Instance.Target;
        if (monster == null) return;

  
        RotationService.Instance.RotateTowards(entity.transform, monster.position,  () =>
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