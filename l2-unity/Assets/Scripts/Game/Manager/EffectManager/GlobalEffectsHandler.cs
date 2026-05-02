using UnityEngine;

public class GlobalEffectsHandler : MonoBehaviour
{


    private void Start()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDestroy()
    {

        if (EventBus.Instance != null)
        {
            EventBus.Instance.OnLevelUp -= HandleLevelUp;
        }
    }

    private void HandleLevelUp(Entity entity, int newLevel)
    {
        EffectManager.Instance.PlayEffect((int)SpecialSkillType.LevelUp, entity.transform);
        Debug.Log($"[GlobalEffects] Played LevelUp effect (ID: {SpecialSkillType.LevelUp}) for {entity.name}");
    }
}