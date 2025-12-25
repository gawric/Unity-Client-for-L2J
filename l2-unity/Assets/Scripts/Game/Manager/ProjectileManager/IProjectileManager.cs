using System;
using UnityEngine;

public interface IProjectileManager
{
    public int LaunchProjectile(GameObject readyProjectile, Vector3 startPos, Transform target, ProjectileData settings = null);
    public void StopProjectile(int projectileId);
    public event Action<GameObject, Transform, Vector3, Vector3> OnHitMonster;
    public event Action<Transform, Vector3, Vector3> OnHitCollider;
}
