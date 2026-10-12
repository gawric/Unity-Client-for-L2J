using UnityEngine;

[RequireComponent(typeof(NetworkAnimationController)),
    RequireComponent(typeof(CharacterAnimationAudioHandler))]
public class NpcEntity : NetworkEntity
{
    private CharacterAnimationAudioHandler _npcAnimationAudioHandler;

    private static readonly string _walk = "walk";
    private static readonly string _run = "run";
    private static readonly string _wait = "wait";
    private string[] allAnim = new string[] { _walk , _run , _wait, };
    private CharacterController _characterController;
    [SerializeField] private NpcData _npcData;

    public NpcData NpcData { get { return _npcData; } set { _npcData = value; } }

    public override void Initialize()
    {
        base.Initialize();
        _npcAnimationAudioHandler = GetComponent<CharacterAnimationAudioHandler>();
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
        PlayNpcLocomotion(_wait);
    }

    public void StartRunAnim(bool run)
    {
        PlayNpcLocomotion(run ? _run : _walk);
    }

    public void OnStartL2jMoving(bool walking)
    {
        PlayNpcLocomotion(walking ? _walk : _run);
    }

    private void StartWalkAnim(bool walking)
    {
        PlayNpcLocomotion(walking ? _walk : _run);
    }

    void PlayNpcLocomotion(string state)
    {
        if (_networkAnimationReceive == null || string.IsNullOrEmpty(state))
            return;

        Animator animator = _networkAnimationReceive.GetAnimator();
        int hash = Animator.StringToHash(state);
        if (animator != null && animator.HasState(0, hash))
        {
            // Npc_Basic still has bool-driven wait/walk/run transitions.
            // Clear them before direct CrossFade so a stale true bool cannot
            // pull the Animator back to the previous locomotion state.
            _networkAnimationReceive.SetBoolDisabledOther(state, false, allAnim);
            PlayerLocomotionCrossFade.TryPlay(_networkAnimationReceive, state);
            return;
        }

        _networkAnimationReceive.SetBoolDisabledOther(state, true, allAnim);
    }


    public override void OnStartMoving(bool walking)
    {
        base.OnStartMoving(walking);
        _networkAnimationReceive.SetAnimationProperty(walking ? (int)NpcAnimationEvent.Walk : (int)NpcAnimationEvent.Run, 1f);
    }

    public CharacterController GetCharacterController()
    {
        return _characterController;
    }




}
