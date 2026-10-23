using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

[System.Serializable]
public class Entity : MonoBehaviour {
    [SerializeField] private bool _entityLoaded;
    [SerializeField] private EntityIdentity _identityInterlude;
    [SerializeField] private Status _status;
    [SerializeField] private Stats _stats;
    [SerializeField] private Status _statusInterlude;
    protected Hit _selfHit;
    [SerializeField] protected Appearance _appearance;
    [SerializeField] private bool _running;
    [SerializeField] private bool _dead;
    [SerializeField] private CharacterRace _race;
    [SerializeField] private CharacterRaceAnimation _raceId;
    [SerializeField] private EntityBodyType _bodyType = EntityBodyType.Humanoid;
    
    public Animator Animator { get; private set; }

    public bool HasAnimator()
    {
        if (Animator == null)
            Animator = GetComponentInChildren<Animator>(true);
        return Animator != null && Animator.runtimeAnimatorController != null;
    }
    [Header("Combat")]
    [SerializeField] private int _targetId;
    [SerializeField] private Transform _target;
    private Transform _lastTarget;
    private Entity _cachedTargetEntity;
    [SerializeField] private Transform _attackTarget;
    [SerializeField] private long _stopAutoAttackTime;
    [SerializeField] private long _startAutoAttackTime;

    private readonly EntityActionSlot _actionSlot = new EntityActionSlot();
    public EntityActionSlot ActionSlot { get { return _actionSlot; } }

    protected MagicCastData _castData;
    protected NetworkAnimationController _networkAnimationReceive;
    protected Gear _gear;
    public Gear Gear => _gear;
    public bool Running
    {
        get { return _running; }
        set
        {
            bool changed = _running != value;
            _running = value;
            if (changed && this is UserEntity && GaitLog.Enabled)
            {
                GaitLog.Log(this, "running-set",
                    "running=" + value +
                    " identityRunning=" + (Identity != null ? Identity.IsRunning : false));
            }
        }
    }

    // Locomotion gait state (Interlude APawn.MoveTimer / bRunning) persisted on the
    // actor rather than on the MovementData. MovementData is dropped on every
    // OnFinish between move packets, which reset MoveTimer to 0 and re-triggered the
    // 0.4s walk window each packet. Keep the timer/gait here so a running CharInfo
    // stays running across consecutive MoveToLocation packets.
    [System.NonSerialized] private float _locomotionTimer;
    [System.NonSerialized] private string _locomotionGait;

    public float LocomotionTimer
    {
        get { return _locomotionTimer; }
        set
        {
            // [GAIT] detect who resets the timer (regression: gait keeps restarting).
            if (_locomotionTimer > 0.05f && value <= 0.001f && GaitLog.Enabled)
            {
                GaitLog.Log(this, "timer-reset",
                    "from=" + _locomotionTimer.ToString("F3") + " to=" + value.ToString("F3"));
            }
            _locomotionTimer = value;
        }
    }

    public string LocomotionGait
    {
        get { return _locomotionGait; }
        set { _locomotionGait = value; }
    }

    public Transform GetWeaponTransform()
    {
        if (_gear == null)
            return null;

        // weapon_shot.int [BOW] RH=-1 LH=0: the charge is attached to the left-hand bow, not Sword Bone.
        if (_gear.WeaponType == WeaponType.bow)
        {
            Transform bow = FirstWeaponMesh(_gear.GetAllTransformByLeftHand(new[] { Gear.weaponName }));
            if (bow != null)
                return bow;
        }

        Transform right = FirstWeaponMesh(_gear.GetAllTransformByRightHand(new[] { Gear.weaponName }));
        if (right != null)
            return right;

        return _gear.GetTransformRightHandBone();
    }

    static Transform FirstWeaponMesh(Transform[] found)
    {
        if (found == null)
            return null;

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
                return found[i];
        }

        return null;
    }

    static readonly string[] SwordBladePointNames = { "Sword_Base", "Sword_Tip" };

    public Transform[] GetSwordBasePoints()
    {
        if (_gear == null)
            return null;
        return _gear.GetAllTransformByRightHand(SwordBladePointNames);
    }

    public Vector3 GetPositionRightHand()
    {
        if (_gear == null)
            return transform.position;
        return _gear.GetPositionRightHand();
    }

    public float GetWeaponRage() { return _gear.GetWeaponRange(); }
    public Status Status { get => _status; set => _status = value; }
    public Stats Stats { get => _stats; set => _stats = value; }
    public Appearance Appearance { get => _appearance; set { _appearance = value; } }
    public EntityIdentity Identity { get => _identityInterlude; set => _identityInterlude = value; }

    public bool IsSoulshotCharged {get;set;}
    public int TargetId { get => _targetId; set => _targetId = value; }

    public Transform LastTarget { get { return _lastTarget; } set { _lastTarget = value; } }
    public Transform Target
    {
        get { return _target; }
        set
        {
            _target = value;
            _lastTarget = null;
            _cachedTargetEntity = null;
        }
    }
    public Transform AttackTarget { get { return _attackTarget; } set { _attackTarget = value; } }
    public bool InCombat { get; set; }
    public long StopAutoAttackTime { get { return _stopAutoAttackTime; } }
    public long StartAutoAttackTime { get { return _startAutoAttackTime; } }
    public CharacterRace Race { get { return _race; } set { _race = value; } }
    public CharacterRaceAnimation RaceId { get { return _raceId; } set { _raceId = value; } }
    public EntityBodyType BodyType { get { return _bodyType; } set { _bodyType = value; } }
    public bool EntityLoaded { get { return _entityLoaded; } set { _entityLoaded = value; } }

    protected  void Awake() 
    {
        _castData = new MagicCastData();

        Animator = GetComponent<Animator>();

        if (Animator == null)
        {
            Animator = GetComponentInChildren<Animator>();
        }
    }

    public void HideObject()
    {
        gameObject.SetActive(false);
    }
    public void ShowObject()
    {
        gameObject.SetActive(true);
    }
    public void SetDead(bool dead)
    {
       _dead = dead;
        if(_dead) Status.SetHp(0);
    }

    public bool GetDead()
    {
        ///lock (this)
        //{
            return _dead;
        //}
    }

    public virtual void Initialize() {
        TryGetComponent(out _networkAnimationReceive);
        TryGetComponent(out _gear);
       
        UpdatePAtkSpeed((int)_stats.PAtkSpd);
        UpdateMAtkSpeed(_stats.MAtkSpd);
        UpdateSpeed(_stats.Speed);

        EquipAllWeapons();
        _gear.OnEquipAnimationRefresh -= OnWeaponEquipped;
        _gear.OnUnequipAnimationRefresh -= OnWeaponUnequipped;
        _gear.OnEquipAnimationRefresh += OnWeaponEquipped;
        _gear.OnUnequipAnimationRefresh += OnWeaponUnequipped;
    }

    public NetworkAnimationController GetAnimatorController()
    {
        return _networkAnimationReceive;
    }

    public float GetCollissionRadius()
    {
        return VectorUtils.ConvertL2jDistance(_appearance.CollisionRadius);
    }

 


    // Called when ApplyDamage packet is received 
    public void ApplyDamage(int damage, bool criticalHit) {
        if(_status.GetHp() <= 0) {
            // Debug.LogWarning("Trying to apply damage to a dead entity");
            return;
        }

        _status.SetHp(Mathf.Max((float)_status.GetHp() - damage, 0));

        OnHit(criticalHit);

        if(_status.GetHp() <= 0) {
            OnDeath();
        }
    }

    protected virtual void EquipAllWeapons() {
        if(_gear == null) {
            // Debug.LogWarning("Gear script is not attached to entity");
            return;
        }
        _gear.SyncEquippedWeapons(_appearance.RHand, _appearance.LHand);
    }

    /* Notify server that entity got attacked */
    public void InflictAttack(AttackType attackType) {
    }

    protected virtual void OnDeath() {
        if(_networkAnimationReceive != null) {
            _networkAnimationReceive.enabled = false;
        }
    }

    protected virtual void OnHit(bool criticalHit) {
        // TODO: Add armor type for more hit sounds
        AudioManager.Instance.PlayHitSound(criticalHit, transform.position);
    }

    public virtual void OnStopMoving() {}

    public virtual void OnStartMoving(bool walking) {}

    public virtual float UpdatePAtkSpeed(int pAtkSpd) {
        _stats.PAtkSpd = pAtkSpd;
        //float convert =  StatsConverter.Instance.ConvertStat(Stat.PHYS_ATTACK_SPEED, pAtkSpd);
        return  StatsConverter.Instance.ConvertStat(Stat.SPEED, pAtkSpd);
    }

    public virtual float UpdateMAtkSpeed(int mAtkSpd) {
        _stats.MAtkSpd = mAtkSpd;
        return StatsConverter.Instance.ConvertStat(Stat.MAGIC_ATTACK_SPEED, mAtkSpd);
    }


    public void UpdateNpcPAtkSpd(int pAtkSpd)
    {
        if (pAtkSpd == 0) return;
        float timeAtck = CalcBaseParam.CalculateTimeL2j(pAtkSpd);
        float speedAnim = CalcBaseParam.GetAnimatedSpeed(pAtkSpd, timeAtck);
        _networkAnimationReceive.SetPAtkSpd(speedAnim);
    }


    public void UpdateNpcWalkSpd(float walkSpeed)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, walkSpeed);
        float anim_converted = NpcLocomotionAnimRateForMesh("Walk");
        if (_networkAnimationReceive != null) _networkAnimationReceive.SetWalkSpeed(anim_converted);
        Stats.UnitySpeedWalking = scaled;
    }


    public void UpdateNpcRunningSpd(float runSpeed)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, runSpeed);
        float anim_converted = NpcLocomotionAnimRateForMesh("run");
        if (_networkAnimationReceive != null) _networkAnimationReceive.SetRunSpeed(anim_converted);
        Stats.UnitySpeedRun = scaled;

    }

    /// <summary>
    /// Monster/NPC locomotion anim rate read from AnimTween col "rate" for this
    /// mesh (runtimeAnimatorController.name = prefab stem, e.g. wolf → wolf_anim),
    /// corrected for the imported 24 fps Unity clip timeline: 1.10 * rate/24.
    /// Falls back to the former 1.10 * 30/24 hardcode when the mesh is not listed.
    /// </summary>
    float NpcLocomotionAnimRateForMesh(string sequence)
    {
        string controllerName = _networkAnimationReceive != null
            ? _networkAnimationReceive.GetAnimatorName()
            : null;

        if (string.IsNullOrEmpty(controllerName))
            return NumberUtils.NpcLocomotionAnimRate();

        // runtimeAnimatorController.name is the override controller's stem
        // (e.g. "wolf"); AnimTween lists it as "<stem>_anim" (e.g. wolf_anim).
        string mesh = controllerName.EndsWith("_anim", System.StringComparison.OrdinalIgnoreCase)
            ? controllerName
            : controllerName + "_anim";

        float rate = AnimTweenTable.Instance.TryGetRate(mesh, sequence);
        return NumberUtils.NpcLocomotionAnimRate(rate);
    } 



    public virtual float UpdateSpeed(int speed) {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, speed);
        _stats.Speed = speed;
        _stats.ScaledSpeed = scaled;
        return StatsConverter.Instance.ConvertStat(Stat.SPEED, speed);
    }

    public virtual float UpdateRunSpeed(float speed)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, speed);
        _stats.UnitySpeedRun = speed;
        return scaled;
        //_stats.ScaledRunSpeed = scaled;
        //return StatsConverter.Instance.ConvertStat(Stat.SPEED, speed);
    }

    public virtual float UpdateAnimRunSpeed(float speed)
    {
        return StatsConverter.Instance.ConvertStat(Stat.ANIM_RUN_SPEED, speed);
    }

    public virtual float UpdateAnimWalkSpeed(float speed)
    {
        return StatsConverter.Instance.ConvertStat(Stat.ANIM_WALK_SPEED, speed);
    }


    public float UpdateAnimRunMagicSpeed(float speed)
    {
        return StatsConverter.Instance.ConvertStat(Stat.ANIM_MAGIC_RUN_SPEED, speed);
    }

    public virtual float UpdateWalkSpeed(float speed)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, speed);
        _stats.UnitySpeedWalking = scaled;
        return scaled;
        //_stats.ScaledWalkSpeed = scaled;
       // return StatsConverter.Instance.ConvertStat(Stat.SPEED, speed);
    }
   
    public virtual bool IsDead() {
        if (_dead) return true;
        return _status != null && _status.GetHp() <= 0;
    }



    public double Hp()
    {
        return _status.GetHp();
    }

    

    public virtual void UpdateWaitType(WaitType moveType)
    {

    }

    public virtual void UpdateMoveType(bool running)
    {
        Running = running;
    }

    public void EquipWeapon(int weaponId , bool isLeftHand)
    {
        _gear.EquipWeapon(weaponId, isLeftHand);
    }


    public void EquipArrow(int itemId , bool leftSlot = false)
    {
        _gear.EquipArrowEtcItem(itemId, leftSlot);
    }

    public bool IsUseBow() => PlayerEntity.Instance.GetCurrentAnimName().IndexOf("bow") > -1;

    public void EquipDualWeapon(int weaponId)
    {
        _gear.EquipLeftAndRightWeapon(weaponId);
    }

    public void UnequipWeapon(bool isLeftHand , int itemId , bool lrDestroy)
    {
        _gear.UnequipWeapon(isLeftHand , itemId, lrDestroy);
    }

    public void UnequipAndDetermineType(ItemInstance item)
    {
        if (_gear is not UserGear usergear)
        {
            // GearFlowLog.Warn("Unequip skip gear not UserGear " + GearFlowLog.Entity(this) +
                // " gear=" + (_gear != null ? _gear.GetType().Name : "null"));
            return;
        }

        // GearFlowLog.Info("UnequipAndDetermineType " + GearFlowLog.Entity(this) +
            // " itemId=" + (item != null ? item.ItemId : 0) +
            // " cat=" + (item != null ? item.Category.ToString() : "null"));

        switch (item.Category)
        {
            case ItemCategory.Weapon:
                bool leftHand = (item.IsBow())? true : false;
                bool lrDestroy = (item.IsDual())? true : false;
                UnequipWeapon(leftHand, item.ItemId , lrDestroy);
                break;

            case ItemCategory.ShieldArmor:
                HandleUnequipSheildArmorItem(item, usergear);
                break;

            default:
                // Debug.LogWarning($"Entity->UnEquipAndDetermineType->Unhandled item category: {item.Category}");
                break;
        }

    }

    public void EquipAndDetermineType(ItemInstance item, int objectId)
    {
        if (_gear is not UserGear usergear)
        {
            // GearFlowLog.Warn("Equip skip gear not UserGear " + GearFlowLog.Entity(this) +
                // " gear=" + (_gear != null ? _gear.GetType().Name : "null"));
            return;
        }

        // GearFlowLog.Info("EquipAndDetermineType " + GearFlowLog.Entity(this) +
            // " itemId=" + (item != null ? item.ItemId : 0) +
            // " cat=" + (item != null ? item.Category.ToString() : "null") +
            // " body=" + (item != null ? item.BodyPart.ToString() : "null"));

        switch (item.Category)
        {
            case ItemCategory.Weapon:
                HandleWeaponEquip(item, objectId);
                break;

            case ItemCategory.ShieldArmor:
                HandleShieldArmorItem(item, usergear);
                break;

            default:
                // GearFlowLog.Warn("EquipAndDetermineType unhandled cat=" + item.Category +
                    // " itemId=" + item.ItemId + " " + GearFlowLog.Entity(this));
                // Debug.LogWarning($"Entity->EquipAndDetermineType->Unhandled item category: {item.Category}");
                break;
        }
    }

    private void HandleWeaponEquip(ItemInstance item, int objectId)
    {
        if (item.IsDual())
        {
            EquipDualWeapon(item.ItemId);
        }
        else
        {
            EquipWeapon(item.ItemId, false);
        }
    }
    private void HandleUnequipSheildArmorItem(ItemInstance item, UserGear usergear)
    {
        int itemId = item.ItemId;
        Weapon weapon = ItemTable.Instance.GetWeapon(itemId);

        if (weapon != null)
        {
            usergear.UnequipWeapon(true , item.ItemId);
            usergear.UnequipShield(item.ItemId);
        }
        else
        {

            usergear.UnequipArmor(item.ItemId , item.BodyPart);
        }
    }
    private void HandleShieldArmorItem(ItemInstance item, UserGear usergear)
    {
        int itemId = item.ItemId;

        Weapon weapon = ItemTable.Instance.GetWeapon(itemId);

        if (weapon != null)
        {
            usergear.EquipShield(itemId);
        }
        else
        {
            usergear.EquipArmor(itemId, item.BodyPart);

        }
    }

    private void OnWeaponEquipped(int weaponId, Weapon weapon)
    {
        if(weapon != null)
        {
            if(this.GetType() == typeof(PlayerEntity))
            {
                PlayerEntity playerEntity = (PlayerEntity)this;
                playerEntity.RefreshRunSpeed();
                PlayerStateMachine.Instance.NotifyEvent(Event.CHANGE_EQUIP);
                
            }
        }
    }

    private void OnWeaponUnequipped(string lastWeaponAnim)
    {
       if (this.GetType() == typeof(PlayerEntity))
       {
          PlayerStateMachine.Instance.NotifyEvent(Event.CHANGE_EQUIP);
       }
    }

    public Entity GetTargetEntity()
    {
        if (_target == null)
        {
            _lastTarget = null;
            _cachedTargetEntity = null;
            return null;
        }

        if (_cachedTargetEntity == null || _lastTarget != _target)
        {
            _lastTarget = _target;
            _cachedTargetEntity = _target.GetComponent<Entity>();
        }

        return _cachedTargetEntity;
    }

    public void SetDamage(int damage) 
    {
        _status.SetDamage(damage);
    }

    public void SetSelfHit(Hit hit)
    {
        _selfHit = hit;
    }
    
    public bool HitIsMissed()
    {
        if (_selfHit == null) return false;
        return _selfHit.isMiss();
    }

    public double CalculateRemainingHp()
    {
        return _status.GetRemainingHp();
    }

    public void SetupTotalCastDuration(float serverHitTimeMs, float flightTimeMs, float[] clipsDurations, float shotEventTime, int targetObjectId = 0, int castStyle = 1, bool mixCombo = false)
    {
        if (_castData == null) _castData = new MagicCastData();

        _castData.Setup(serverHitTimeMs, flightTimeMs, clipsDurations, shotEventTime, targetObjectId, castStyle, mixCombo);
    }

    public void SetupLongCastDuration(float serverHitTimeMs, float[] clipsDurations, float shotEventTime, int targetObjectId = 0)
    {
        if (_castData == null) _castData = new MagicCastData();

        _castData.SetupLongCast(serverHitTimeMs, clipsDurations, shotEventTime, targetObjectId);
    }

    public MagicCastData GetMagicCastData()
    {
        return _castData;
    }

}
