using System;
using UnityEngine;

public class NewMagicSkillsState  : AbstractAttackEvents
{
    public NewMagicSkillsState(PlayerStateMachine stateMachine) :
         base(stateMachine.GetObjectId(),
         SpecialAnimationNames.GetMagicSkillsAnimations(),
         stateMachine)
    { }

    public override void Enter()
    {

    }
    public override void HandleEvent(Event evt, object payload = null)
    {
        MagicSkillUse useSkill = GetPayload(payload);
        PlayerEntity entity = _stateMachine.Player;
        int objectId = entity.IdentityInterlude.Id;

        switch (evt)
        {
            case Event.READY_TO_ACT:

                AnimationCombo readyCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                float[] durations = AnimationManager.Instance.GetOverrideClipsDurations(objectId, readyCombo.GetAnimCycle());
                float shotEventTime = AnimationManager.Instance.GetOverrideEventTimeByName(objectId, readyCombo.GetAnimCycle(), "OnAnimationShoot");
                float flightTimeMs = ResolveMagicFlightTimeMs(entity);
                entity.SetupTotalCastDuration(useSkill.HitTime, flightTimeMs, durations, shotEventTime);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                MagicCastData castData = entity.GetMagicCastData();
                if (castData != null)
                {
                    Debug.Log(
                        $"[MagicCastTimeline] castStart={castData.StartTime:F3}s now={Time.time:F3}s " +
                        $"globalSinceStart={Time.time - castData.StartTime:F3}s serverShoot={castData.serverTimeToShoot:F3}s " +
                        $"serverHit={castData.HitTime:F3}s configuredFlightMs={flightTimeMs:F1} shotEvent={shotEventTime:F3}s.");
                }
#endif

                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, readyCombo, _events);
                break;
            case Event.CANCEL:
                Debug.Log("NewMagicSkillsState Use Sate> Отмена скорее всего запрос пришел из ActionFaild");
                break;
            case Event.APPLY_SELF_SKILL:
                AnimationCombo selfCombo = SkillgrpTable.Instance.GetAnimComboBySkillId(useSkill.SkillId, useSkill.SkillLvl);
                SkillExecutor.Instance.ExecuteSkillOverride(useSkill.SkillGrp, entity, selfCombo, _events);
                break;

        }
    }

    private static float ResolveMagicFlightTimeMs(PlayerEntity entity)
    {
        const float fallbackFlightMs = 1000f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (entity != null && entity.Target != null)
        {
            float distance = entity.TargetDistance();
            Debug.Log(
                $"[MagicFlightResolve] distance={distance:F3}m resolvedFlightMs={fallbackFlightMs:F1} " +
                $"mode=fixed_for_sync");
        }
        else
        {
            Debug.Log($"[MagicFlightResolve] distance=-1 resolvedFlightMs={fallbackFlightMs:F1} mode=fixed_for_sync");
        }
#endif
        return fallbackFlightMs;
    }
}
