using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public class SkillExecutor : MonoBehaviour
{
    private SkillAnimationRunner _animRunner;

    public event Action OnSkillSequenceFinished;
    public static SkillExecutor Instance { get; private set; }


    public SkillExecutor()
    {
        _animRunner = new SkillAnimationRunner();

    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task ExecuteSkill(Entity entity , AnimationCombo animationCombo , AnimationEventsBase actions)
    {
        if (entity == null || animationCombo == null) return;
        int objectId = entity.IdentityInterlude.Id;


        // _emitter.SetupActions(actions);

        string[] cycle = animationCombo.GetAnimCycle();
        _animRunner.StartRun(cycle, objectId , AnimationManager.Instance  , () => OnAllAnimationFinish(actions));
    }

    public async Task ExecuteSkillOverride(Entity entity, AnimationCombo animationCombo, AnimationEventsBase actions)
    {
        if (entity == null || animationCombo == null) return;
        int objectId = entity.IdentityInterlude.Id;

        EffectManager.Instance.PlayEffect(1177, entity.transform , entity.GetMagicCastData());
        //_emitter.SetupActions(actions);

        string[] cycle = animationCombo.GetAnimCycle();
        _animRunner.StartRunOverride(cycle, objectId, AnimationManager.Instance, () => OnAllAnimationFinish(actions));
    }


    private void OnAllAnimationFinish(AnimationEventsBase actions)
    {
       // _emitter.CleanupActions(actions);
    }


}