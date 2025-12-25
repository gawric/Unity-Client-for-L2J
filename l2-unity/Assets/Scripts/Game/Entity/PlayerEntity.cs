using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PlayerEntity : Entity
{
    private CharacterAnimationAudioHandler _characterAnimationAudioHandler;

    private static PlayerEntity _instance;
    public Animation RandomName { get; set; }

    public float RemainingTime { get; set; }

    public int CountAtk { get; set; }
    public int CurrentAttackCount { get; set; }

    public bool isAccesNewAtk {get;set;}
    public bool isStop { get; set; }

    public bool IsAttack { get; set; }
    public bool isAutoAttack { get; set; }

    public  Animation LastAtkAnimation { get; set; }
    public static PlayerEntity Instance { get => _instance; }

    //default combo name
    private readonly Animation[] pAtkList = { AnimationNames.ATK01, AnimationNames.ATK02, AnimationNames.ATK03 };
  

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            RandomName = pAtkList[0];
            CountAtk = 2;
        }
        else
        {
            Destroy(this);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _characterAnimationAudioHandler = GetComponentInChildren<CharacterAnimationAudioHandler>();

        if (_instance == null)
        {
            _instance = this;
        }

        EquipAllArmors();

        EntityLoaded = true;
    }

    void OnDestroy()
    {
        _instance = null;
    }

    private void EquipAllArmors()
    {
        PlayerInterludeAppearance appearance = (PlayerInterludeAppearance)_appearance;
        if (appearance.Chest != 0)
        {
            ((PlayerGear)_gear).EquipArmor(appearance.Chest, ItemSlot.chest);
        }
        else
        {
            ((PlayerGear)_gear).EquipArmor(ItemTable.NAKED_CHEST, ItemSlot.chest);
        }

        if (appearance.Legs != 0)
        {
            ((PlayerGear)_gear).EquipArmor(appearance.Legs, ItemSlot.legs);
        }
        else
        {
            ((PlayerGear)_gear).EquipArmor(ItemTable.NAKED_LEGS, ItemSlot.legs);
        }

        if (appearance.Gloves != 0)
        {
            ((PlayerGear)_gear).EquipArmor(appearance.Gloves, ItemSlot.gloves);
        }
        else
        {
            ((PlayerGear)_gear).EquipArmor(ItemTable.NAKED_GLOVES, ItemSlot.gloves);
        }

        if (appearance.Feet != 0)
        {
            ((PlayerGear)_gear).EquipArmor(appearance.Feet, ItemSlot.feet);
        }
        else
        {
            ((PlayerGear)_gear).EquipArmor(ItemTable.NAKED_BOOTS, ItemSlot.feet);
        }
    }

    public string GetEquippedWeaponName()
    {
        PlayerGear gear = (PlayerGear)_gear;
        return gear.GetNameWeapon();
    }

    protected override void LookAtTarget() { }

    protected override void OnDeath()
    {
        base.OnDeath();
        //Debug.Log("Player on death _networkAnimationReceive:" + _networkAnimationReceive);
        PlayerStateMachine.Instance.NotifyEvent(Event.DEAD);
    }

    protected override void OnHit(bool criticalHit)
    {
        base.OnHit(criticalHit);
        _characterAnimationAudioHandler.PlaySound(CharacterSoundEvent.Dmg);
    }



    public override float UpdateMAtkSpeed(int mAtkSpd)
    {
        float converted = base.UpdateMAtkSpeed(mAtkSpd);
        PlayerAnimationController.Instance.SetMAtkSpd(converted);

        return converted;
    }

    public override float UpdatePAtkSpeed(int pAtkSpd)
    {
        float converted = base.UpdatePAtkSpeed(pAtkSpd);
        PlayerAnimationController.Instance.SetPAtkSpd(converted);

        return converted;
    }

   
    public void RefreshRandomPAttack()
    {
        int randomIndex = UnityEngine.Random.Range(0, pAtkList.Length);
        RandomName = pAtkList[randomIndex];
    }


    public void UpdatePAtkSpeedPlayer(int pAtkSpd)
    {
        if (pAtkSpd == 0) return;
         float timeAtck = CalcBaseParam.CalculateTimeL2j(pAtkSpd);
         float speedAnim = CalcBaseParam.GetAnimatedSpeed(pAtkSpd, timeAtck);
         PlayerAnimationController.Instance.SetPAtkSpeed(speedAnim);
    }

  

    public override float UpdateRunSpeed(float speed)
    {
        float converted = base.UpdateRunSpeed(speed);
        float anim_converted = GetAnimSpeed(_appearance, speed);
        PlayerAnimationController.Instance.SetRunSpeed(anim_converted);
        PlayerController.Instance.UpdateRunSpeed(converted);

        return converted;
    }


    private float GetAnimSpeed(Appearance appearance , float speed)
    {
        if (appearance.GetType() == typeof(PlayerInterludeAppearance))
        {
            PlayerInterludeAppearance pia = (PlayerInterludeAppearance)appearance;
            if (pia.BaseClass == (int)BaseClass.MMagic)
            {
                return UpdateAnimRunMagicSpeed(speed);
            }
            else
            {
               return  UpdateAnimRunSpeed(speed);

            }
        }

        return UpdateAnimRunSpeed(speed);
    }

    public override float UpdateWalkSpeed(float speed)
    {
        float converted = base.UpdateWalkSpeed(speed);
        //float anim = UpdateAnimRunSpeed(speed);
        //default speed anim to human fighter
        PlayerAnimationController.Instance.SetWalkSpeed(0.45f);
        PlayerController.Instance.UpdateWalkSpeed(converted);

        return converted;
    }

  



    public override void UpdateWaitType(ChangeWaitTypePacket.WaitType moveType)
    {
        base.UpdateWaitType(moveType);
    }

    public override void UpdateMoveType(bool running)
    {
        base.UpdateMoveType(running);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.Running = running;
        }

        if (PlayerStateMachine.Instance != null)
        {
           // PlayerStateMachine.Instance.NotifyEvent(Event.MOVE_TYPE_UPDATED);
        }

        if (CharacterInfoWindow.Instance != null)
        {
            CharacterInfoWindow.Instance.UpdateValues();
        }
    }

    public string GetLastAnimName()
    {
        return _gear.LastWeaponAnim;
    }

    public string GetCurrentAnimName()
    {
        return _gear.WeaponAnim;
    }

    public Vector3 GetPositionRightHand()
    {
        return _gear.GetPositionRightHand();
    }

    public GameObject GetGoEtcItem()
    {
        return _gear.GetGoEtcItem();
    }

    public float TargetDistance()
    {
        Vector3 startPos = GetPositionRightHand();
        Transform target = PlayerEntity.Instance.Target;

        return VectorUtils.Distance2D(startPos , target.position);
    }
}