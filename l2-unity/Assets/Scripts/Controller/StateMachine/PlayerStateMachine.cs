using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    private bool _enableLogs = true;
 
    private static PlayerStateMachine _instance;
    public static PlayerStateMachine Instance => _instance;


    private PlayerState _currentState;
    private Intention _currentIntention;

    public Intention Intention { get { return _currentIntention; } }
    public Transform Follow { get; set; }

    public int GetObjectId()
    {
        if (Player == null) return -1;
        return Player.IdentityInterlude.Id;
    }
    public PlayerEntity Player { get; set; }
    public bool IsMoveToPawn { get; set; }
    public PlayerState State { get { return _currentState; } }



    private StateBase _stateInstance;
    private IntentionBase _intentionInstance;
    private Dictionary<PlayerState, StateBase> _dictState = new Dictionary<PlayerState, StateBase>();
    private Dictionary<Intention, IntentionBase> _dictIntention = new Dictionary<Intention, IntentionBase>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
         
        }
        else
        {
            Destroy(this);
        }

        InitializeState();
        InitializeIntention();

        this.enabled = false;
    }


    private void Update()
    {
        _stateInstance?.Update();
        _intentionInstance?.Update();
    }

    public void ChangeState(PlayerState newState)
    {
        if (_enableLogs) Debug.Log("[StateMachine][STATE] " + newState);

        _stateInstance?.Exit();
        _currentState = newState;
        InitializeState();
        _stateInstance?.Enter();
    }

    public void ChangeIntention(Intention intention)
    {
        ChangeIntention(intention, null);
    }

    public void ChangeIntention(Intention newIntention, object arg0)
    {

        if (_enableLogs) Debug.Log("[Player->StateMachine][INTENTION] " + newIntention);
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



    private StateBase GetOrAddState(Dictionary<PlayerState, StateBase> dict, PlayerState key)
    {

        if (!dict.ContainsKey(key))
        {
            var state = CreatorStatePlayer.GetState(key, this);
            dict.Add(key, state);
        }


        return dict[key];
    }

    private IntentionBase GetOrAddIntention(Dictionary<Intention, IntentionBase> dict, Intention key)
    {

        if (!dict.ContainsKey(key))
        {
            var state = CreateIntentionPlayer.GetIntention(key, this);
            dict.Add(key, state);
        }


        return dict[key];
    }
    public void NotifyEvent(Event evt , object payload = null)
    {
        if (_enableLogs) Debug.Log("[StateMachine][EVENT] " + evt);
        _stateInstance?.HandleEvent(evt , payload);
    }



    public void OnWaitReturn()
    {
        if (_enableLogs) Debug.Log("[StateMachine] wait return attack");
        NotifyEvent(Event.WAIT_RETURN);
    }
}

