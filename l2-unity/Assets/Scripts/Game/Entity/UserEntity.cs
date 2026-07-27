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

    /// <summary>
    /// L2J has no lightweight "equip changed" packet for other players - the server just re-sends
    /// a full CharInfo, which World.SpawnUserInterlude routes here instead of treating as a fresh
    /// spawn. Unequips whatever was actually worn before (falling back to the same "naked"
    /// substitutes EquipAllArmors uses for an empty slot) so the old mesh doesn't linger once the
    /// new gear from the fresh appearance is equipped.
    /// </summary>
    public void RefreshEquipment(PlayerInterludeAppearance newAppearance)
    {
        PlayerInterludeAppearance oldAppearance = (PlayerInterludeAppearance)_appearance;

        UnequipCurrentWeapons(oldAppearance);
        UnequipCurrentArmors(oldAppearance);

        Appearance = newAppearance;

        EquipAllWeapons();
        EquipAllArmors();
    }

    private void UnequipCurrentWeapons(PlayerInterludeAppearance oldAppearance)
    {
        // lrDestroy=true checks both hand bones regardless of which one it actually ended up on -
        // EquipWeapon internally overrides handedness for some weapon types (e.g. bows), so we
        // can't reliably guess which bone the old mesh is parented to from the appearance data alone.
        if (oldAppearance.RHand != 0)
        {
            UnequipWeapon(false, oldAppearance.RHand, true);
        }

        if (oldAppearance.LHand != 0 && oldAppearance.LHand != oldAppearance.RHand)
        {
            UnequipWeapon(false, oldAppearance.LHand, true);
            // UnequipWeapon only ever looks for a "weapon_<id>" child under the hand bones - a
            // shield is named "shield_<id>" and lives on the shield bone instead, so it's never
            // found/removed there. UnequipShield no-ops safely if LHand wasn't actually a shield.
            _gear.UnequipShield(oldAppearance.LHand);
        }
    }

    private void UnequipCurrentArmors(PlayerInterludeAppearance oldAppearance)
    {
        UserGear gear = (UserGear)_gear;
        gear.UnequipArmor(oldAppearance.Chest != 0 ? oldAppearance.Chest : ItemTable.NAKED_CHEST, ItemSlot.chest);
        gear.UnequipArmor(oldAppearance.Legs != 0 ? oldAppearance.Legs : ItemTable.NAKED_LEGS, ItemSlot.legs);
        gear.UnequipArmor(oldAppearance.Gloves != 0 ? oldAppearance.Gloves : ItemTable.NAKED_GLOVES, ItemSlot.gloves);
        gear.UnequipArmor(oldAppearance.Feet != 0 ? oldAppearance.Feet : ItemTable.NAKED_BOOTS, ItemSlot.feet);
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