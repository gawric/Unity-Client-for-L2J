using UnityEngine;

[RequireComponent(typeof(NetworkAnimationController)),
    RequireComponent(typeof(NetworkTransformReceive)),
    RequireComponent(typeof(NetworkCharacterControllerReceive)),
    RequireComponent(typeof(CharacterAnimationAudioHandler))]
public class NpcEntity : NetworkEntity
{
    private CharacterAnimationAudioHandler _npcAnimationAudioHandler;

    private static readonly string _walk = "walk";
    private static readonly string _run = "run";
    private static readonly string _wait = "wait";
    private string[] allAnim = new string[] { _walk , _run , _wait, };
    private CharacterController _characterController;
    private NpcStateMachine _stateMachine;
    [SerializeField] private NpcData _npcData;

    public NpcData NpcData { get { return _npcData; } set { _npcData = value; } }

    public override void Initialize()
    {
        base.Initialize();
        _npcAnimationAudioHandler = GetComponent<CharacterAnimationAudioHandler>();
        _stateMachine = GetComponent<NpcStateMachine>();
        _characterController = GetComponent<CharacterController>();

        EntityLoaded = true;
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        _networkAnimationReceive.SetAnimationProperty((int)NpcAnimationEvent.Death, 1f, true);
    }

    protected override void OnHit(bool criticalHit)
    {
        base.OnHit(criticalHit);
        _npcAnimationAudioHandler.PlaySound(CharacterSoundEvent.Dmg);
    }

    public override void OnStopMoving()
    {
        if (_networkAnimationReceive.GetAnimationProperty((int)NpcAnimationEvent.Atk01) == 0f)
        {
            _networkAnimationReceive.SetAnimationProperty((int)NpcAnimationEvent.Wait, 1f);
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
           // _networkAnimationReceive.SetAnimationProperty((int)NpcAnimationEvent.Wait, 1f);
           // _networkAnimationReceive.SetBool(_wait, true);
            _networkAnimationReceive.SetBoolDisabledOther(_wait, true, allAnim);
        }

        // Debug.Log("STOPPPPPPPPPPPP L2j MOOOOOOOOVVVVVVVVVVIIIING");
    }

    public void StartRunAnim(bool run)
    {
        if (run)
        {
            //Debug.Log("GETTTTTTTTTTTTT ANIIIIIIII MATION STATUS " + _networkAnimationReceive.name);
            //_networkAnimationReceive.Test();
            //_networkAnimationReceive.SetAnimationProperty((int)MonsterAnimationEvent.Run, 1f);
            // _networkAnimationReceive.SetBool(_run, true);
            _networkAnimationReceive.SetBoolDisabledOther(_run, true, allAnim);

        }
        else
        {
            if (!_networkAnimationReceive.GetBool(_walk))
            {
                //_networkAnimationReceive.SetBool(_walk, true);
                _networkAnimationReceive.SetBoolDisabledOther(_walk, true, allAnim);
            }
        }
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
                //_networkAnimationReceive.SetAnimationProperty(walking ? (int)MonsterAnimationEvent.Walk : (int)MonsterAnimationEvent.Run, 1f);

                string _runS = walking ? _walk : _run;
                _networkAnimationReceive.SetBoolDisabledOther(_runS, true, allAnim);

            }

        }

        StartWalkAnim(walking);
    }

    private void StartWalkAnim(bool walking)
    {
        if (walking)
        {
            _networkAnimationReceive.SetBoolDisabledOther(_walk, true, allAnim);
            //_networkAnimationReceive.SetBool(_walk, true);
        }
        else
        {
            if (!_networkAnimationReceive.GetBool(_run))
            {
                _networkAnimationReceive.SetBoolDisabledOther(_run, true, allAnim);
                //_networkAnimationReceive.SetBool(_run, true);
            }

        }
    }


    public override void OnStartMoving(bool walking)
    {
        base.OnStartMoving(walking);
        _networkAnimationReceive.SetAnimationProperty(walking ? (int)NpcAnimationEvent.Walk : (int)NpcAnimationEvent.Run, 1f);
    }

    public NpcStateMachine GetStateMachine()
    {
        return _stateMachine;
    }

    public CharacterController GetCharacterController()
    {
        return _characterController;
    }




}
