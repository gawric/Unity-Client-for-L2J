using System.Collections.Generic;
using UnityEngine;

public static class AnimationDataCache
{
    // Кэшируем сам объект клипа: [ControllerName_OriginalMotionName] -> AnimationClip
    private static readonly Dictionary<string, AnimationClip> _clipCache = new Dictionary<string, AnimationClip>();

    public static AnimationClip GetOverrideClip(Animator animator, string originalMotionName)
    {
        if (string.IsNullOrEmpty(originalMotionName) || animator == null) return null;

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        string cacheKey = controller.name + "_" + originalMotionName;

        if (_clipCache.TryGetValue(cacheKey, out AnimationClip cachedClip))
        {
            return cachedClip;
        }

        AnimationClip finalClip = null;
        var overrideController = controller as AnimatorOverrideController;

        if (overrideController != null)
        {
            // Прямой доступ через индексатор (самый быстрый)
            finalClip = overrideController[originalMotionName];

            // Если через индексатор не вышло, ищем в списке переопределений
            if (finalClip == null)
            {
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
                overrideController.GetOverrides(overrides);
                foreach (var pair in overrides)
                {
                    if (pair.Key != null && pair.Key.name == originalMotionName)
                    {
                        finalClip = pair.Value != null ? pair.Value : pair.Key;
                        break;
                    }
                }
            }
        }
        else
        {
            
            foreach (var clip in controller.animationClips)
            {
                if (clip.name == originalMotionName)
                {
                    finalClip = clip;
                    break;
                }
            }
        }

        if (finalClip != null) _clipCache[cacheKey] = finalClip;
        return finalClip;
    }

    // Получить длину клипа
    public static float GetOverrideLength(Animator animator, string originalMotionName)
    {
        AnimationClip clip = GetOverrideClip(animator, originalMotionName);
        return clip != null ? clip.length : 1.0f;
    }

    // Получить время эвента по имени
    public static float GetEventTimeByName(Animator animator, string originalMotionName, string eventName)
    {
        AnimationClip clip = GetOverrideClip(animator, originalMotionName);
        if (clip == null) return 0f;

        foreach (var animEvent in clip.events)
        {
            if (animEvent.functionName == eventName)
            {
                return animEvent.time;
            }
        }

        Debug.LogWarning($"[AnimCache] Event '{eventName}' не найден в клипе {clip.name}");
        return 0f;
    }

    public static AnimationClip GetActiveClip(Animator animator, int layerIndex)
    {
        if (animator == null) return null;

        AnimatorClipInfo[] clipInfo;

        // Если аниматор в процессе перехода, нам нужен клип, К КОТОРОМУ мы идем
        if (animator.IsInTransition(layerIndex))
        {
            clipInfo = animator.GetNextAnimatorClipInfo(layerIndex);
        }
        else
        {
            clipInfo = animator.GetCurrentAnimatorClipInfo(layerIndex);
        }

        if (clipInfo.Length > 0)
        {
            // Возвращает именно тот клип, который подставлен в OverrideController
            return clipInfo[0].clip;
        }

        return null;
    }

    public static float GetEventTimeByName(Animator animator, AnimationClip clip , string eventName)
    {
        if (clip == null) return 0f;

        foreach (var animEvent in clip.events)
        {
            if (animEvent.functionName == eventName)
            {
                return animEvent.time;
            }
        }

        Debug.LogWarning($"[AnimCache] Event '{eventName}' не найден в клипе {clip.name}");
        return 0f;
    }



    public static void ClearCache() => _clipCache.Clear();


}
