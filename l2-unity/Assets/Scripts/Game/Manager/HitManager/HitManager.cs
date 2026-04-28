using System;
using UnityEngine;

public class HitManager : MonoBehaviour
{
    public static HitManager Instance { get; private set; }
    private const string ETC_NAME = "etc_";



    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void HandleHitBody(GameObject source, Transform target , Vector3 hitPointCollider, Vector3 hitDirection)
    {
        
        string sourceNameLower = source.name.ToLower();
        string etcNameLower = ETC_NAME.ToLower();
        //HandleHitCollider(target, hitPointCollider, hitDirection);

        if (sourceNameLower.IndexOf(etcNameLower) > -1)
        {
            HandleArrowHit(source, target, hitPointCollider, hitDirection);
        }
        else
        {
            Debug.LogWarning("HitManager> HandleHit Errors no detected hit type");
        }


    }

    public void HandleHitCollider(Entity attaker ,  Transform attacker , MonsterStateMachine targetStateMachine, Vector3 hitCollider, Vector3 hitColliderDirection)
    {
    
        if(targetStateMachine != null)
        {
            if (targetStateMachine != null & targetStateMachine.State == MonsterState.IDLE)
            {
                targetStateMachine.NotifyEvent(Event.HIT_REACTION);
            }
            if (attaker.IsSoulshotCharged)
            {
                EffectManager.Instance.PlayerImpactEffect(99998, hitCollider);
                attaker.IsSoulshotCharged = false;
            }
            else
            {
                WorldCombat.Instance.InflictAttack(hitCollider, hitColliderDirection);
            }
            //WorldCombat.Instance.InflictAttack(hitCollider, hitColliderDirection);
            //Эффект рабочий просто для теста off

        }
    }

    public bool TryPrepareProjectileEffectHit(
        GameObject projectilePrefab,
        Vector3 hitPoint,
        Vector3 hitDirection,
        int attackerEntityId,
        Func<Transform, bool> isFromTrackedProjectile,
        out Vector3 resolvedHitPoint,
        out Vector3 resolvedHitDirection)
    {
        resolvedHitPoint = hitPoint;
        resolvedHitDirection = Vector3.forward;

        if (projectilePrefab == null || isFromTrackedProjectile == null)
        {
            return false;
        }

        Transform attacker = projectilePrefab.transform;
        if (attacker == null || !isFromTrackedProjectile(attacker))
        {
            return false;
        }

        resolvedHitDirection = hitDirection.sqrMagnitude > 0.0001f
            ? hitDirection.normalized
            : Vector3.forward;

        Entity attackerEntity = ResolveEntityFromWorld(attackerEntityId);
        if (attackerEntity != null && attackerEntity.IsSoulshotCharged)
        {
            EffectManager.Instance.PlayerImpactEffect(99998, resolvedHitPoint);
            attackerEntity.IsSoulshotCharged = false;
        }

        return true;
    }

    private Entity ResolveEntityFromWorld(int entityId)
    {
        if (entityId <= 0 || World.Instance == null)
        {
            return null;
        }

        return World.Instance.GetEntityNoLockSync(entityId);
    }


    private void HandleArrowHit(GameObject arrow, Transform target, Vector3 hitPointCollider ,Vector3 hitDirection)
    {
        if (!target.CompareTag("Npc")) return;


        MonsterEntity entity = target.GetComponent<MonsterEntity>();

        if (entity == null) return;

        entity.AttachArrowToNearestBone(arrow, hitPointCollider , target, hitDirection);

        // Disable physics
        Collider arrowCollider = arrow.GetComponent<Collider>();
        if (arrowCollider != null)
        {
            arrowCollider.enabled = false;
        }
    }



}



public enum HitType
{
    Projectile,
    Melee
}
