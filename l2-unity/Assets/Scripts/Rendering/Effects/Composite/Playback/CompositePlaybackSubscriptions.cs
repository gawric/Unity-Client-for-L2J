using System;
using System.Collections.Generic;
using UnityEngine;

public static class CompositePlaybackSubscriptions
{
    public static void SubscribeShootIfNeeded(
        CompositePrefabPart[] parts,
        EffectResolveContext context,
        List<AnimationEventsBase> shootEventSources,
        Action<string> handler,
        out AnimationEventsBase animationEvents,
        ref bool isSubscribedToAnyShoot)
    {
        UnsubscribeShoot(shootEventSources, handler, ref isSubscribedToAnyShoot);
        animationEvents = null;

        if (!CompositeProjectileLaunchHelper.RequiresAnimationShootEvent(parts))
        {
            return;
        }

        if (context?.CasterEntity?.Identity == null || IncomingPacketActions.Animations == null)
        {
            return;
        }

        int casterId = context.CasterEntity.Identity.Id;
        animationEvents = IncomingPacketActions.Animations.GetAnimationEvents(casterId);
        TrySubscribeShootSource(shootEventSources, animationEvents, handler);
        isSubscribedToAnyShoot = true;
    }

    public static void UnsubscribeShoot(
        List<AnimationEventsBase> shootEventSources,
        Action<string> handler,
        ref bool isSubscribedToAnyShoot)
    {
        if (shootEventSources != null)
        {
            for (int i = 0; i < shootEventSources.Count; i++)
            {
                AnimationEventsBase source = shootEventSources[i];
                if (source != null)
                {
                    source.OnAnimationStartShoot -= handler;
                }
            }

            shootEventSources.Clear();
        }

        isSubscribedToAnyShoot = false;
    }

    public static void TrySubscribeShootSource(
        List<AnimationEventsBase> shootEventSources,
        AnimationEventsBase source,
        Action<string> handler)
    {
        if (source == null || shootEventSources == null || shootEventSources.Contains(source))
        {
            return;
        }

        source.OnAnimationStartShoot += handler;
        shootEventSources.Add(source);
    }

    public static void SubscribeHitIfNeeded(
        CompositePrefabPart[] parts,
        Action<GameObject, Transform, Vector3, Vector3, int> handler,
        ref bool isSubscribed)
    {
        if (!CompositePartScheduler.RequiresHitColliderSpawn(parts) ||
            ProjectileManager.Instance == null ||
            isSubscribed)
        {
            return;
        }

        ProjectileManager.Instance.OnHitEffectProjectile += handler;
        isSubscribed = true;
    }

    public static void UnsubscribeHit(
        Action<GameObject, Transform, Vector3, Vector3, int> handler,
        ref bool isSubscribed)
    {
        if (ProjectileManager.Instance != null && isSubscribed)
        {
            ProjectileManager.Instance.OnHitEffectProjectile -= handler;
        }

        isSubscribed = false;
    }

    public static bool IsFromLaunchedCompositeProjectile(
        Transform attacker,
        Dictionary<CompositePrefabPart, BaseEffect> spawnedPartInstances)
    {
        if (attacker == null || spawnedPartInstances == null)
        {
            return false;
        }

        foreach (KeyValuePair<CompositePrefabPart, BaseEffect> pair in spawnedPartInstances)
        {
            if (!CompositeProjectileLaunchHelper.IsProjectilePart(pair.Key) || pair.Value == null)
            {
                continue;
            }

            Transform spawnedTransform = pair.Value.transform;
            if (attacker == spawnedTransform || attacker.IsChildOf(spawnedTransform))
            {
                return true;
            }
        }

        return false;
    }
}
