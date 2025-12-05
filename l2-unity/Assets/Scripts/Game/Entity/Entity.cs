using System;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.EventSystems.EventTrigger;

[System.Serializable]
public class Entity : MonoBehaviour {
    [SerializeField] private bool _entityLoaded;
    [SerializeField] private NetworkIdentity _identity;
    [SerializeField] private NetworkIdentityInterlude _identityInterlude;
    [SerializeField] private Status _status;
    [SerializeField] private Stats _stats;
    [SerializeField] private Status _statusInterlude;
    [SerializeField] protected Appearance _appearance;
    [SerializeField] private bool _running;
    [SerializeField] private bool _dead;
    [SerializeField] private CharacterRace _race;
    [SerializeField] private CharacterRaceAnimation _raceId;


    [Header("Combat")]
    [SerializeField] private int _targetId;
    [SerializeField] private Transform _target;
    [SerializeField] private Transform _attackTarget;
    [SerializeField] private long _stopAutoAttackTime;
    [SerializeField] private long _startAutoAttackTime;


    protected NetworkAnimationController _networkAnimationReceive;
    protected NetworkTransformReceive _networkTransformReceive;
    protected NetworkCharacterControllerReceive _networkCharacterControllerReceive;
    protected Gear _gear;
    public bool Running { get { return _running; } set { _running = value; } }
    
    public float GetWeaponRage() { return _gear.GetWeaponRange(); }
    public NetworkCharacterControllerReceive networkCharacterController { get { return _networkCharacterControllerReceive; } }
    public Status Status { get => _status; set => _status = value; }
    public Stats Stats { get => _stats; set => _stats = value; }
    public Appearance Appearance { get => _appearance; set { _appearance = value; } }
    public NetworkIdentity Identity { get => _identity; set => _identity = value; }

    public NetworkIdentityInterlude IdentityInterlude { get => _identityInterlude; set => _identityInterlude = value; }

    public int TargetId { get => _targetId; set => _targetId = value; }
    public Transform Target { get { return _target; } set { _target = value; } }
    public Transform AttackTarget { get { return _attackTarget; } set { _attackTarget = value; } }
    public long StopAutoAttackTime { get { return _stopAutoAttackTime; } }
    public long StartAutoAttackTime { get { return _startAutoAttackTime; } }
    public CharacterRace Race { get { return _race; } set { _race = value; } }
    public CharacterRaceAnimation RaceId { get { return _raceId; } set { _raceId = value; } }
    public bool EntityLoaded { get { return _entityLoaded; } set { _entityLoaded = value; } }

    public void FixedUpdate() {
        LookAtTarget();
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
    }

    public bool GetDead()
    {
        ///lock (this)
        //{
            return _dead;
        //}
    }

    protected virtual void LookAtTarget() {
        if (AttackTarget != null && Status.GetHp() > 0) {
            _networkTransformReceive.LookAt(_attackTarget);
        }
    }

    public virtual void Initialize() {
        TryGetComponent(out _networkAnimationReceive);
        TryGetComponent(out _networkTransformReceive);
        TryGetComponent(out _networkCharacterControllerReceive);
        TryGetComponent(out _gear);
       
        UpdatePAtkSpeed((int)_stats.PAtkSpd);
        UpdateMAtkSpeed(_stats.MAtkSpd);
        UpdateSpeed(_stats.Speed);

        EquipAllWeapons();
        _gear.OnEquipAnimationRefresh += OnWeaponEquipped;
        _gear.OnUnequipAnimationRefresh += OnWeaponUnequipped;
    }

    public NetworkAnimationController GetAnimatorController()
    {
        return _networkAnimationReceive;
    }

    public float GetCollissionHeight()
    {
        return VectorUtils.ConvertL2jDistance(_appearance.CollisionHeight);
    }

    public float GetCollissionRadius()
    {
        return VectorUtils.ConvertL2jDistance(_appearance.CollisionRadius);
    }

 


    // Called when ApplyDamage packet is received 
    public void ApplyDamage(int damage, bool criticalHit) {
        if(_status.GetHp() <= 0) {
            Debug.LogWarning("Trying to apply damage to a dead entity");
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
            Debug.LogWarning("Gear script is not attached to entity");
            return;
        }
        if (_appearance.LHand != 0) {
            _gear.EquipWeapon(_appearance.LHand, true);
        }
        if (_appearance.RHand != 0) {
            var weapon =  WeapongrpTable.Instance.GetWeapon(_appearance.RHand);

            if(weapon != null)
            {
                if(weapon.WeaponType == WeaponType.dual)
                {
                    _gear.EquipLeftAndRightWeapon(_appearance.RHand);
                    return;
                }

            }
            _gear.EquipWeapon(_appearance.RHand, false);

        }
    }

    /* Notify server that entity got attacked */
    public void InflictAttack(AttackType attackType) {
        GameClient.Instance.ClientPacketHandler.InflictAttack(_identity.Id, attackType);
    }

    protected virtual void OnDeath() {
        if(_networkAnimationReceive != null) {
            _networkAnimationReceive.enabled = false;
        }
        if (_networkTransformReceive != null) {
            _networkTransformReceive.enabled = false;
        }
        if (_networkCharacterControllerReceive != null) {
            _networkCharacterControllerReceive.enabled = false;
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

   // public void UpdateNpcPAtkSpd(int pAtackSpd)
    //{
     //   float speed = StatsConverter.Instance.ConvertStat(Stat.SPEED, pAtackSpd);
     //   Stats.ScaledSpeed = speed;
     //   Stats.PAtkSpd = (int)speed;
   // }

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
        float anim_converted = UpdateAnimWalkSpeed(walkSpeed);
        if (_networkAnimationReceive != null) _networkAnimationReceive.SetWalkSpeed(anim_converted);
        //Debug.Log("Scaled speed NPC Walk " + anim_converted + " name walk speed " + name + " original speed " + walkSpeed);
        Stats.UnitySpeedWalking = scaled;
    }

    public void UpdateNpcRunningSpd(float runSpeed)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, runSpeed);
        float anim_converted = UpdateAnimRunSpeed(runSpeed);
        if (_networkAnimationReceive != null) _networkAnimationReceive.SetRunSpeed(anim_converted);
        //Debug.Log("Scaled speed NPC Run " + anim_converted + " name walk speed " + name);
        Stats.UnitySpeedRun = scaled;
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
   
    public bool IsDead() {
        if (_dead) return true;
        return Status.GetHp() <= 0;
    }

    public virtual void UpdateWaitType(ChangeWaitTypePacket.WaitType moveType)
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
        if (_gear is not UserGear usergear) return;

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
                Debug.LogWarning($"Entity->UnEquipAndDetermineType->Unhandled item category: {item.Category}");
                break;
        }

    }

    public void EquipAndDetermineType(ItemInstance item, int objectId)
    {
        if (_gear is not UserGear usergear) return;

        switch (item.Category)
        {
            case ItemCategory.Weapon:
                HandleWeaponEquip(item, objectId);
                break;

            case ItemCategory.ShieldArmor:
                HandleShieldArmorItem(item, usergear);
                break;

            default:
                Debug.LogWarning($"Entity->EquipAndDetermineType->Unhandled item category: {item.Category}");
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

}
