using UnityEngine;

[RequireComponent(typeof(NetworkAnimationController))]

public class UserEntity : NetworkEntity
{
    private CharacterAnimationAudioHandler _characterAnimationAudioHandler;
    private CharacterController _characterController;
    private float _attackVisualUntil;
    private UserBowArrowEvents _bowArrowEvents;

    public override void Initialize()
    {
        base.Initialize();
        _characterController = GetComponent<CharacterController>();
        if (transform.childCount > 0)
            _characterAnimationAudioHandler = transform.GetChild(0).GetComponentInChildren<CharacterAnimationAudioHandler>();

        EquipAllArmors();

        EntityLoaded = true;
    }

    public CharacterController GetCharacterController()
    {
        if (_characterController == null)
            _characterController = GetComponent<CharacterController>();
        return _characterController;
    }

    public void RefreshVisuals()
    {
        GearFlowLog.Info("UserEntity.RefreshVisuals nick=" + Nick +
            " id=" + (Identity != null ? Identity.Id : 0) +
            " " + GearFlowLog.Paperdoll(this) +
            " gear=" + (_gear != null ? _gear.GetType().Name : "null"));
        EquipAllWeapons();
        EquipAllArmors();
    }

    public string Nick
    {
        get
        {
            if (Identity != null && !string.IsNullOrEmpty(Identity.Name))
                return Identity.Name;
            return name;
        }
    }

    public string LogTag
    {
        get { return "[EntityAction:User] nick=" + Nick; }
    }

    public override float UpdateRunSpeed(float speed)
    {
        float converted = base.UpdateRunSpeed(speed);
        if (Stats != null)
            Stats.UnitySpeedRun = converted;
        float anim = ApplyAnimRunSpeed(speed);
        Debug.Log(LogTag + " UpdateRunSpeed id=" +
            (Identity != null ? Identity.Id : 0) +
            " l2=" + speed + " unity=" + converted +
            " anim=" + anim);
        return converted;
    }

    public override float UpdateWalkSpeed(float speed)
    {
        float converted = base.UpdateWalkSpeed(speed);
        ApplyAnimWalkSpeed(speed);
        return converted;
    }

    float ApplyAnimRunSpeed(float serverValue)
    {
        if (_networkAnimationReceive == null)
            return 0f;

        PlayerAppearance appearance = _appearance as PlayerAppearance;
        bool twoHanded = _gear != null && _gear.IsTwoHandedEquipped();
        float anim = appearance != null
            ? CharTemplateRegistry.GetRunSpeed(appearance.BaseClass, appearance.Sex, serverValue, twoHanded)
            : UpdateAnimRunSpeed(serverValue);
        _networkAnimationReceive.SetRunSpeed(anim);
        return anim;
    }

    void ApplyAnimWalkSpeed(float serverValue)
    {
        if (_networkAnimationReceive == null)
            return;

        PlayerAppearance appearance = _appearance as PlayerAppearance;
        bool twoHanded = _gear != null && _gear.IsTwoHandedEquipped();
        float anim = appearance != null
            ? CharTemplateRegistry.GetWalkSpeed(appearance.BaseClass, appearance.Sex, serverValue, twoHanded)
            : UpdateAnimWalkSpeed(serverValue);
        _networkAnimationReceive.SetWalkSpeed(anim);
    }

    public void LogSpeed(string where)
    {
        Stats s = Stats;
        float move = 0f;
        if (s != null)
            move = Running ? s.UnitySpeedRun : s.UnitySpeedWalking;
        string msg = LogTag + " speed " + where +
            " id=" + (Identity != null ? Identity.Id : 0) +
            " running=" + Running +
            " BaseRun=" + (s != null ? s.BaseRunSpeed : 0) +
            " BaseWalk=" + (s != null ? s.BaseWalkingSpeed : 0) +
            " RunReal=" + (s != null ? s.RunRealSpeed : 0f) +
            " WalkReal=" + (s != null ? s.WalkRealSpeed : 0f) +
            " unityRun=" + (s != null ? s.UnitySpeedRun : 0f) +
            " unityWalk=" + (s != null ? s.UnitySpeedWalking : 0f) +
            " moveSpeed=" + move;
        if (move <= 0.001f)
            Debug.LogWarning(msg);
        else
            Debug.Log(msg);
    }

    public string WeaponAnim
    {
        get
        {
            if (_gear != null && !string.IsNullOrEmpty(_gear.WeaponAnim))
                return _gear.WeaponAnim;
            return "hand";
        }
    }

    public void BindCharInfoAnimEvents(UserBowArrowEvents bowArrowEvents)
    {
        _bowArrowEvents?.Exit();
        if (Identity == null)
            return;
        _bowArrowEvents = bowArrowEvents;
        _bowArrowEvents?.Enter();
    }

    void OnDestroy()
    {
        _bowArrowEvents?.Exit();
        _bowArrowEvents = null;
    }

    public void FaceTowards(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
            return;
        transform.rotation = Quaternion.LookRotation(dir);
    }

    public void BeginAttackVisual(float durationSec)
    {
        _attackVisualUntil = Time.time + Mathf.Max(0.1f, durationSec);
    }

    public float AttackVisualLeftSec()
    {
        if (_attackVisualUntil <= 0f)
            return -1f;
        return _attackVisualUntil - Time.time;
    }

    public bool IsAttackVisualPlaying()
    {
        return _attackVisualUntil > 0f && Time.time < _attackVisualUntil;
    }

    public bool ShouldReturnFromAttack()
    {
        return _attackVisualUntil > 0f && Time.time >= _attackVisualUntil;
    }

    public void ClearAttackVisual()
    {
        _attackVisualUntil = 0f;
    }

    public void PlayDeath()
    {
        SetDead(true);
        ClearAttackVisual();
    }

    public void EquipAllArmors()
    {
        PlayerAppearance appearance = _appearance as PlayerAppearance;
        UserGear gear = _gear as UserGear;
        if (appearance == null || gear == null)
        {
            GearFlowLog.Warn("UserEntity.EquipAllArmors abort " + GearFlowLog.Entity(this) +
                " appearance=" + (appearance != null) + " gear=" + (_gear != null ? _gear.GetType().Name : "null"));
            return;
        }

        GearFlowLog.Info("UserEntity.EquipAllArmors " + GearFlowLog.Entity(this) +
            " " + GearFlowLog.Paperdoll(appearance));
        gear.SyncEquippedArmor(appearance);
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        _networkAnimationReceive.SetAnimationProperty((int)PlayerAnimationEvent.death, 1f, true);
    }





    protected override void OnHit(bool criticalHit)
    {
        base.OnHit(criticalHit);
        if (_characterAnimationAudioHandler != null)
            _characterAnimationAudioHandler.PlaySound(CharacterSoundEvent.Dmg);
    }

   
}