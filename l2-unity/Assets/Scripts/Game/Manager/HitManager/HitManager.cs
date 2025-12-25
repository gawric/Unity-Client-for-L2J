using Org.BouncyCastle.Security;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.ProBuilder;
using static ChangeWaitTypePacket;

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

    public void HandleHitCollider(Transform attacker , Transform target, Vector3 hitCollider, Vector3 hitColliderDirection)
    {
        GameObject targetGameObject = target.gameObject;

        if(targetGameObject != null)
        {
            MonsterStateMachine targetStateMachine = targetGameObject.GetComponent<MonsterStateMachine>();
            if (targetStateMachine != null & targetStateMachine.State == MonsterState.IDLE)
            {
                targetStateMachine.NotifyEvent(Event.HIT_REACTION);
                Debug.Log("Hit colliders on HIT_REACTION");
            }

            WorldCombat.Instance.InflictAttack(hitCollider, hitColliderDirection);

        }

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
