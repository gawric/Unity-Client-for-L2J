using UnityEngine;

[RequireComponent(typeof(NetworkAnimationController)),
    RequireComponent(typeof(MonsterAnimationAudioHandler))]
public class MonsterEntity : NetworkEntity
{
    private MonsterAnimationAudioHandler _monsterAnimationAudioHandler;
    private LayerMask _areaMask = 8192;
    private NpcData _npcData;
    private MonsterStateMachine _stateMachine;
    private float _defaultRunSpeed;
    private float _defaultWalkSpeed;
    private CharacterController _characterController;
    private readonly string _walk = "walk";
    private readonly string _run = "run";
    private readonly string _wait = "wait";

    public NpcData NpcData { get { return _npcData; } set { _npcData = value; } }

    public float DefaultRunSpeed { get { return _defaultRunSpeed; } set { _defaultRunSpeed = value; } }
    public float DefaultWalkSpeed { get { return _defaultWalkSpeed; } set { _defaultWalkSpeed = value; } }

    public override void Initialize()
    {
        base.Initialize();
        _monsterAnimationAudioHandler = GetComponent<MonsterAnimationAudioHandler>();
        _stateMachine = GetComponent<MonsterStateMachine>();
        _characterController = GetComponent<CharacterController>();
        EntityLoaded = true;
    }


    public MonsterStateMachine GetStateMachine()
    {
        return _stateMachine;
    }

    public  CharacterController GetCharacterController()
    {
        return _characterController;
    }

    public ObjectData GetByIdUseLocator(int id , Vector3 sourcePosition)
    {
        var _entitiesInRange = Physics.SphereCastAll(sourcePosition, 50f, transform.forward, 0, _areaMask);

        foreach (var entity in _entitiesInRange)
        {
            if (entity.transform.parent != null)
            {
                var _entity = entity.transform.parent.GetComponent<Entity>();

                if (_entity != null & _entity.IdentityInterlude.Id == id)
                {
                    return new ObjectData(entity.transform.parent.gameObject);
                }
            }
        }
        return null;
    }
    protected override void OnDeath()
    {
        base.OnDeath();
        _networkAnimationReceive.SetAnimationProperty((int)MonsterAnimationEvent.Death, 1f, true);
    }

    public void OnDeathL2j()
    {
        //Debug.Log("Dead Animation 2");
        _networkAnimationReceive.SetAnimationProperty((int)MonsterAnimationEvent.Death, 1f, true);
        _networkAnimationReceive.SetBool("death", true);
    }

    protected override void OnHit(bool criticalHit)
    {
        base.OnHit(criticalHit);
        _monsterAnimationAudioHandler.PlaySound(MonsterSoundEvent.Dmg);
    }

    public void OnWaitAnim()
    {
        _networkAnimationReceive.SetBool(_wait , true);
    }
    public override void OnStopMoving()
    {
        if (_networkAnimationReceive.GetAnimationProperty((int)MonsterAnimationEvent.Atk01) == 0f)
        {
            _networkAnimationReceive.SetAnimationProperty((int)MonsterAnimationEvent.Wait, 1f);
        }
        else
        {
            _networkAnimationReceive.SetAnimationProperty((int)NpcAnimationEvent.Wait, 1f);
        }
    }

    public void OnStopL2jMoving()
    {
        if (!_networkAnimationReceive.GetBool(_wait))
        {
            _networkAnimationReceive.SetAnimationProperty((int)NpcAnimationEvent.Wait, 1f);
            _networkAnimationReceive.SetBool(_wait, true);
        }

       // Debug.Log("STOPPPPPPPPPPPP L2j MOOOOOOOOVVVVVVVVVVIIIING");
    }

    public override void OnStartMoving(bool walking)
    {
        // _baseAnimationController.SetBool("walk_" + WeaponAnim, false);
        _networkAnimationReceive.SetAnimationProperty(walking ? (int)MonsterAnimationEvent.Walk : (int)MonsterAnimationEvent.Run, 1f);
    }

    public void OnStartL2jMoving(bool walking)
    {
       // base.OnStartMoving(walking);
        float speedAnim = walking ? Stats.ScaledAnimWalkSpeed : Stats.ScaledAnimRunSpeed;
        if (walking)
        {

        }
        else
        {
            if (!_networkAnimationReceive.GetBool(_run))
            {
                _networkAnimationReceive.SetAnimationProperty(walking ? (int)MonsterAnimationEvent.Walk : (int)MonsterAnimationEvent.Run, 1f);
            }
            
        }
        
        StartWalkAnim(walking);
    }

    private void StartWalkAnim(bool walking)
    {
        if (walking)
        {
            _networkAnimationReceive.SetBool(_walk, true);
        }
        else
        {
            if (!_networkAnimationReceive.GetBool(_run))
            {
                _networkAnimationReceive.SetBool(_run, true);
            }
            
        }
    }
  
    public void AttachArrowToNearestBone(GameObject attach , Vector3 hitPointCollider, Transform target , Vector3 hitDirection)
    {
        if(_gear.GetType() != typeof(MonsterGear))
        {
            Debug.LogWarning("MonsterEntity->AttachArrowToNearestBone: gear invalid cast !!!");
            return;
        }

        MonsterGear gear = (MonsterGear)_gear;
        Transform targetBone = gear.DetermineHitSide(hitPointCollider, target);
        bool isSpecialBone = (targetBone == gear.GetHeadBone() || targetBone == gear.GetTailBone());
        gear.SetPositionArrowRandomlyNearCenter( attach,  targetBone  , hitDirection , isSpecialBone);
    }



    public void OnDestroyDepends()
    {
        if (GetComponent<MonsterAnimationAudioHandler>())
            Destroy(GetComponent<MonsterAnimationAudioHandler>());
    }

  
}
