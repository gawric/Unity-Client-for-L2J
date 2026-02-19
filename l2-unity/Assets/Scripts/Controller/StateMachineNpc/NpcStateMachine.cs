using System;
using System.Collections.Generic;

using System.Security.Cryptography;
using System.Security.Principal;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class NpcStateMachine : MonoBehaviour
{

    [SerializeField] private bool _enableLogs;
    [SerializeField] private NpcState _currentState;
    [SerializeField] private NpcIntention _currentIntention;
 
    public NpcIntention Intention { get { return _currentIntention; } }
    public Transform Follow { get; set; }
    public NpcState State { get { return _currentState; } }
    private NpcBase _stateInstance;
    private NpcIntentionBase _intentionInstance;
    private int _npcObjectId;
    private int _npcId;
    private GameObject _go;
    private MoveNpc _moveNpc;
    private PlayerEntity _target;
    private Entity _entity;
    private Dictionary<NpcState, NpcBase> _dictState = new Dictionary<NpcState, NpcBase>();
    private Dictionary<NpcIntention, NpcIntentionBase> _dictIntention = new Dictionary<NpcIntention, NpcIntentionBase>();



    private void Start()
    {
        //_waitingForServerReply = false;
        this.enabled = false;


    }

    public GameObject NpcObject { get { return _go; } }

    public MoveNpc MoveNpc { get { return _moveNpc; } }

    public Entity Entity { get { return _entity; } }

    public virtual void Initialize(int npcObjectId,
        int npcId,
        GameObject go,
        MoveNpc moveNpc,
        Entity entity
        )
    {
        _npcObjectId = npcObjectId;
        _npcId = npcId;
        _go = go;
        _moveNpc = moveNpc;
        _entity = entity;

        InitializeState();
        InitializeIntention();
        ChangeState(NpcState.IDLE);

        SetGravityOnlyMoveNpc(entity);

    }



    public void ChangeState(NpcState newState)
    {
        if (_enableLogs) Debug.Log("[NPC :StateMachine][STATE] " + newState);
        _stateInstance?.Exit();
        _currentState = newState;
        InitializeState();
        _stateInstance?.Enter();
    }

    public void ChangeIntention(NpcIntention intention)
    {
        ChangeIntention(intention, null);
    }


    // Update is called once per frame
    private void Update()
    {
        _stateInstance?.Update();
        _intentionInstance?.Update();
    }


    private void InitializeState()
    {
        _stateInstance = GetOrAddState(_dictState, _currentState);
    }

    private void InitializeIntention()
    {
        _intentionInstance = GetOrAddIntention(_dictIntention, _currentIntention);
    }


    private NpcBase GetOrAddState(Dictionary<NpcState, NpcBase> dict, NpcState key)
    {

        if (!dict.ContainsKey(key))
        {
            var state = CreatorStateNpc.GetState(key, this);
            dict.Add(key, state);
        }


        return dict[key];
    }


    private NpcIntentionBase GetOrAddIntention(Dictionary<NpcIntention, NpcIntentionBase> dict, NpcIntention key)
    {

        if (!dict.ContainsKey(key))
        {
            var state = CreatorIntentionNpc.GetIntention(key, this);
            dict.Add(key, state);
        }


        return dict[key];
    }


    public void ChangeIntention(NpcIntention newIntention, object arg0)
    {
       if (_enableLogs) Debug.Log("[NPC:StateMachine][INTENTION] " + newIntention);
      _intentionInstance?.Exit();
      _currentIntention = newIntention;
       InitializeIntention();
     _intentionInstance?.Enter(arg0);
    }


    public void SetGravityOnlyMoveNpc(Entity entity)
    {
        if (entity.name.Equals("Leandro") | entity.name.Equals("Remy"))
        {
            GravityNpc.Instance.AddGravity(entity.IdentityInterlude.Id, new GravityData(entity));
        }
    }


    public void NotifyEvent(Event evt)
    {
        if (_enableLogs) Debug.Log("[Monster: StateMachine][EVENT] " + evt);
         _stateInstance?.HandleEvent(evt);
    }

    public override string ToString()
    {
        return "NPC ObjID: " + _npcObjectId + "  You Npc Id: " + _npcId;
    }

}
