using System.Collections.Generic;
using UnityEngine;

public class MonsterStateMachine : MonoBehaviour
{

    [SerializeField] private bool _enableLogs;
    [SerializeField] private MonsterState _currentState;
    [SerializeField] private MonsterIntention _currentIntention;
    private bool _isAutoAttack = false;
    public MonsterIntention Intention { get { return _currentIntention; } }
    public Transform Follow { get; set; }
    public MonsterState State { get { return _currentState; } }
    private MonsterBase _stateInstance;
    private MonsterIntentionBase _intentionInstance;
    private int _monsterId;
    private int _npcId;
    private GameObject _go;
    private PlayerEntity _target;
    private Entity _entity;
   // private GravityMonster _gravityMonster;


    private Dictionary<MonsterState, MonsterBase> _dictState = new Dictionary<MonsterState, MonsterBase>();
    private Dictionary<MonsterIntention, MonsterIntentionBase> _dictIntention = new Dictionary<MonsterIntention, MonsterIntentionBase>();

    private void Start()
    {
        //_waitingForServerReply = false;
        this.enabled = false;

        InitializeState();
        InitializeIntention();
    }

    public PlayerEntity GetTarget() { return _target; }

    public void SetTarget(PlayerEntity target)
    {
        _target = target;
    }
    public GameObject MonsterObject { get { return _go; } }

    public Entity Entity { get { return _entity; } }



    public virtual void Initialize(int mosterId, 
        int npcId,
        GameObject go, 
        Entity entity
        )
    {
        _monsterId = mosterId;
        _npcId = npcId;
        _go = go;
        _entity = entity;
        //_gravityMonster = gravityMonster;

        ChangeIntention(MonsterIntention.INTENTION_IDLE);
        ChangeState(MonsterState.IDLE);
        NotifyEvent(Event.ENTER_WORLD);
        GravityNpc.Instance.AddGravity(entity.IdentityInterlude.Id, new GravityData(entity));
        // _gravityMonster.Sync();
    }

    public void ChangeState(MonsterState newState)
    {
        //if (_enableLogs) Debug.Log("[Monster:StateMachine][STATE] " + newState);
        //Debug.Log("[Monster:StateMachine][STATE] " + newState);
        _stateInstance?.Exit();
        _currentState = newState;
        InitializeState();
        _stateInstance?.Enter();
    }

    public void ChangeIntention(MonsterIntention intention)
    {
        ChangeIntention(intention, null);
    }


    // Update is called once per frame
    private void Update()
    {
        _stateInstance?.Update();
        _intentionInstance?.Update();
    }


    public void ChangeIntention(MonsterIntention newIntention, object arg0)
    {
        //if (_enableLogs) Debug.Log("[Monster:StateMachine][INTENTION] " + newIntention);
        _intentionInstance?.Exit();
        _currentIntention = newIntention;
        InitializeIntention();
        _intentionInstance?.Enter(arg0);
    }


   
    private void InitializeState()
    {
        _stateInstance = GetOrAddState(_dictState, _currentState);
    }

    private void InitializeIntention()
    {
        _intentionInstance = GetOrAddIntention(_dictIntention, _currentIntention);
    }


    private MonsterBase GetOrAddState(Dictionary<MonsterState, MonsterBase> dict, MonsterState key)
    {

        if (!dict.ContainsKey(key))
        {
            var state = CreatorState.GetState(key, this);
            dict.Add(key, state);
        }


        return dict[key];
    }

    private MonsterIntentionBase GetOrAddIntention(Dictionary<MonsterIntention, MonsterIntentionBase> dict, MonsterIntention key)
    {

        if (!dict.ContainsKey(key))
        {
            var state = CreatorIntention.GetIntention(key, this);
            dict.Add(key, state);
        }


        return dict[key];
    }


    public void NotifyEvent(Event evt)
    {
        _stateInstance?.HandleEvent(evt);
    }

    public override string ToString()
    {
        return "Monster ID: " + _monsterId + "  Npc Id: " + _npcId;
    }

}
