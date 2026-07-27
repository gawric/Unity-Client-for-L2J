using UnityEngine;

[RequireComponent(typeof(NetworkAnimationController)),
    RequireComponent(typeof(NetworkTransformReceive)),
    RequireComponent(typeof(NetworkCharacterControllerReceive))]

public class UserEntity : NetworkEntity
{
    private CharacterAnimationAudioHandler _characterAnimationAudioHandler;
    private CharacterController _characterController;

    public override void Initialize()
    {
        base.Initialize();
        _characterAnimationAudioHandler = transform.GetChild(0).GetComponentInChildren<CharacterAnimationAudioHandler>();
        _characterController = GetComponent<CharacterController>();

        EquipAllArmors();

        EntityLoaded = true;
    }

    public CharacterController GetCharacterController()
    {
        return _characterController;
    }

    /// <summary>
    /// Movement speed itself still goes through the same scaling every other network-driven
    /// entity uses (Stat.SPEED - what MovementData.GetSpeed()/CharacterController.Move() reads).
    /// The animator pace is different: the generic NPC conversion (Entity.UpdateAnimRunSpeed,
    /// Stat.ANIM_RUN_SPEED) isn't calibrated for this player-shaped rig/clip set, which is why
    /// other players visibly ran faster than their actual travel speed. PlayerEntity already solves
    /// this for the local player via CharTemplateRegistry (per class/sex/weapon); reusing the same
    /// table here keeps both movement types visually consistent with each other.
    /// </summary>
    public override float UpdateRunSpeed(float serverValue)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, serverValue);
        Stats.UnitySpeedRun = scaled;

        PlayerInterludeAppearance appearance = (PlayerInterludeAppearance)_appearance;
        float animConverted = CharTemplateRegistry.GetRunSpeed(appearance.BaseClass, appearance.Sex, serverValue, _gear.IsTwoHandedEquipped());
        _networkAnimationReceive.SetRunSpeed(animConverted);

        return scaled;
    }

    public override float UpdateWalkSpeed(float serverValue)
    {
        float scaled = StatsConverter.Instance.ConvertStat(Stat.SPEED, serverValue);
        Stats.UnitySpeedWalking = scaled;

        PlayerInterludeAppearance appearance = (PlayerInterludeAppearance)_appearance;
        float animConverted = CharTemplateRegistry.GetWalkSpeed(appearance.BaseClass, appearance.Sex, serverValue, _gear.IsTwoHandedEquipped());
        _networkAnimationReceive.SetWalkSpeed(animConverted);

        return scaled;
    }

    public void EquipAllArmors()
    {
        PlayerInterludeAppearance appearance = (PlayerInterludeAppearance)_appearance;
        if (appearance.Chest != 0)
        {
            ((UserGear)_gear).EquipArmor(appearance.Chest, ItemSlot.chest);
        }
        else
        {
            ((UserGear)_gear).EquipArmor(ItemTable.NAKED_CHEST, ItemSlot.chest);
        }

        if (appearance.Legs != 0)
        {
            ((UserGear)_gear).EquipArmor(appearance.Legs, ItemSlot.legs);
        }
        else
        {
            ((UserGear)_gear).EquipArmor(ItemTable.NAKED_LEGS, ItemSlot.legs);
        }

        if (appearance.Gloves != 0)
        {
            ((UserGear)_gear).EquipArmor(appearance.Gloves, ItemSlot.gloves);
        }
        else
        {
            ((UserGear)_gear).EquipArmor(ItemTable.NAKED_GLOVES, ItemSlot.gloves);
        }

        if (appearance.Feet != 0)
        {
            ((UserGear)_gear).EquipArmor(appearance.Feet, ItemSlot.feet);
        }
        else
        {
            ((UserGear)_gear).EquipArmor(ItemTable.NAKED_BOOTS, ItemSlot.feet);
        }
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        _networkAnimationReceive.SetAnimationProperty((int)PlayerAnimationEvent.death, 1f, true);
    }





    protected override void OnHit(bool criticalHit)
    {
        base.OnHit(criticalHit);
        _characterAnimationAudioHandler.PlaySound(CharacterSoundEvent.Dmg);
    }

   
}