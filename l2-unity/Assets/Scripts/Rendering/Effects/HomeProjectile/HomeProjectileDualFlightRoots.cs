using System.Collections.Generic;
using UnityEngine;

public sealed class HomeProjectileDualFlightRoots
{
    public List<HomeProjectileFlightAnchor> CollectTaggedAnchors(BaseEffect effect)
    {
        List<HomeProjectileFlightAnchor> anchors = new List<HomeProjectileFlightAnchor>();
        if (effect == null)
        {
            return anchors;
        }

        HomeProjectileFlightAnchor[] tagged =
            effect.GetComponentsInChildren<HomeProjectileFlightAnchor>(true);
        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i] != null)
            {
                anchors.Add(tagged[i]);
            }
        }

        return anchors;
    }

    public void Ensure(BaseEffect instance, bool mirrorDualFlight)
    {
        if (instance == null || !mirrorDualFlight)
        {
            return;
        }

        if (CollectTaggedAnchors(instance).Count >= 2)
        {
            return;
        }

        Transform root = instance.transform;
        if (root.Find("HomeFlight_L") != null)
        {
            return;
        }

        GameObject leftObject = new GameObject("HomeFlight_L");
        Transform left = leftObject.transform;
        left.SetParent(root, false);
        left.localPosition = Vector3.zero;
        left.localRotation = Quaternion.identity;
        left.localScale = Vector3.one;

        List<Transform> toMove = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != left)
            {
                toMove.Add(child);
            }
        }

        for (int i = 0; i < toMove.Count; i++)
        {
            toMove[i].SetParent(left, true);
        }

        HomeProjectileTrailVelocityProvider.MoveFromRootToFlightRoot(root, left);

        GameObject rightObject = Object.Instantiate(leftObject, root);
        rightObject.name = "HomeFlight_R";
        HomeProjectileTrailVelocityProvider rightTrail =
            rightObject.GetComponent<HomeProjectileTrailVelocityProvider>();
        if (rightTrail != null)
        {
            rightTrail.RetargetBindingsTo(rightObject.transform);
            rightTrail.ResetRuntimeState();
        }

        HomeProjectileFlightAnchor leftAnchor = leftObject.AddComponent<HomeProjectileFlightAnchor>();
        leftAnchor.profile = ParticleGroupHomeFlightProfile.DefaultAnchor;
        HomeProjectileFlightAnchor rightAnchor = rightObject.GetComponent<HomeProjectileFlightAnchor>();
        if (rightAnchor == null)
        {
            rightAnchor = rightObject.AddComponent<HomeProjectileFlightAnchor>();
        }

        rightAnchor.profile = ParticleGroupHomeFlightProfile.MirroredAnchor;

        ParticleEmitterV2.BindHostOwnedEmission(rightObject.transform);
        EffectPart[] cloneParts = rightObject.GetComponentsInChildren<EffectPart>(true);
        for (int i = 0; i < cloneParts.Length; i++)
        {
            if (cloneParts[i] != null)
            {
                cloneParts[i].PlayPart();
            }
        }
    }
}
