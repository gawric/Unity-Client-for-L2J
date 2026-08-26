using System;
using UnityEngine;


public class PlayerEntity : Entity
{


    private CharacterAnimationAudioHandler _characterAnimationAudioHandler;

    private float _lastServerRunSpeed = 0;

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

    //private readonly Animation[] pAtkList = {  AnimationNames.ATK01 };

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

    public void RefreshVisuals()
    {
        GearFlowLog.Info("PlayerEntity.RefreshVisuals " + GearFlowLog.Entity(this) +
            " " + GearFlowLog.Paperdoll(this) +
            " gear=" + (_gear != null ? _gear.GetType().Name : "null"));
        EquipAllWeapons();
        EquipAllArmors();
    }

    void OnDestroy()
    {
        _instance = null;
    }

    private void EquipAllArmors()
    {
        PlayerAppearance appearance = _appearance as PlayerAppearance;
        UserGear gear = _gear as UserGear;
        if (appearance == null || gear == null)
        {
            GearFlowLog.Warn("PlayerEntity.EquipAllArmors abort " + GearFlowLog.Entity(this) +
                " appearance=" + (appearance != null) + " gear=" + (_gear != null ? _gear.GetType().Name : "null"));
            return;
        }

        GearFlowLog.Info("PlayerEntity.EquipAllArmors " + GearFlowLog.Entity(this) +
            " " + GearFlowLog.Paperdoll(appearance));
        gear.SyncEquippedArmor(appearance);
    }

    public string GetEquippedWeaponName()
    {
        PlayerGear gear = (PlayerGear)_gear;
        return gear.GetNameWeapon();
    }

    protected override void OnDeath()
    {
        base.OnDeath();
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



    public override float UpdateRunSpeed(float serverValue)
    {
        float converted = base.UpdateRunSpeed(serverValue);
        PlayerAppearance playerApperance = (PlayerAppearance)_appearance;

        float anim_converted = CharTemplateRegistry.GetRunSpeed(playerApperance.BaseClass,
            playerApperance.Sex, 
            serverValue, 
            _gear.IsTwoHandedEquipped());

        PlayerAnimationController.Instance.SetRunSpeed(anim_converted);
        IncomingPacketActions.Player.UpdateRunSpeed(converted);
        _lastServerRunSpeed = serverValue;
        return converted;
    }

    public void RefreshRunSpeed()
    {
        PlayerAppearance playerApperance = (PlayerAppearance)_appearance;
        float anim_converted = CharTemplateRegistry.GetRunSpeed(playerApperance.BaseClass, playerApperance.Sex, _lastServerRunSpeed, _gear.IsTwoHandedEquipped());
        PlayerAnimationController.Instance.SetRunSpeed(anim_converted);
    }

    public override float UpdateWalkSpeed(float serverValue)
    {
        float converted = base.UpdateWalkSpeed(serverValue);
        PlayerAppearance playerApperance = (PlayerAppearance)_appearance;
        float anim_converted = CharTemplateRegistry.GetWalkSpeed(playerApperance.BaseClass, playerApperance.Sex, serverValue, _gear.IsTwoHandedEquipped());
        PlayerAnimationController.Instance.SetWalkSpeed(anim_converted);
        //PlayerAnimationController.Instance.SetWalkSpeed(0.45f);
        IncomingPacketActions.Player.UpdateWalkSpeed(converted);

        return converted;
    }

  



    public override void UpdateWaitType(WaitType moveType)
    {
        base.UpdateWaitType(moveType);
    }

    public override void UpdateMoveType(bool running)
    {
        base.UpdateMoveType(running);

        if (IncomingPacketActions.Player != null)
        {
            IncomingPacketActions.Player.Running = running;
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

  

    public string GetCurrentAnimName()
    {
        return _gear.WeaponAnim;
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

    public void SetProceduralSpinePose(Vector3 rotation)
    {
        try
        {
            Transform bone = _gear.GetSpineBone();
            SpineProceduralController.Instance.SetBoneMod(bone, new BoneModification(rotation, Vector3.zero, 1.0f));
        }
        catch(Exception ex)
        {
            Debug.LogError("");
        }


    }

    public void SetProceduralRightUpperArmPose(Vector3 rotation)
    {
        try
        {
            Transform upperArm = _gear.GetRightUpperArm();
            SpineProceduralController.Instance.SetBoneMod(upperArm, new BoneModification(rotation, Vector3.zero, 1.0f));
        }
        catch (Exception ex)
        {
            Debug.LogError("");
        }

    }

    public void RemoveProceduralPose()
    {
        Transform bone = _gear.GetSpineBone();
        Transform upperArm = _gear.GetRightUpperArm();
        SpineProceduralController.Instance.RemoveBoneMod(bone);
        SpineProceduralController.Instance.RemoveBoneMod(upperArm);
    }
}