using UnityEngine;

public class EffectResolveContext
{
    public int CasterUserId;
    public int TargetUserId;

    public Entity CasterEntity;
    public Entity TargetEntity;

    public Transform CasterTransform;
    public Transform TargetTransform;

    public MagicCastData CastData;

    public bool HasHitPoint;
    public Vector3 HitPoint;
}
