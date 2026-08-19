using UnityEngine;

/// <summary>
/// Filter Console by <c>[GEAR]</c> to trace UserEntity / PlayerEntity equipment.
/// </summary>
public static class GearFlowLog
{
    public const string Tag = "[GEAR]";

    public static void Info(string message)
    {
        Debug.Log(Tag + " " + message);
    }

    public static void Warn(string message)
    {
        Debug.LogWarning(Tag + " " + message);
    }

    public static string Paperdoll(PlayerAppearance a)
    {
        if (a == null)
            return "appearance=null";
        return "RHand=" + a.RHand +
            " LHand=" + a.LHand +
            " Chest=" + a.Chest +
            " Legs=" + a.Legs +
            " Gloves=" + a.Gloves +
            " Feet=" + a.Feet;
    }

    public static string Paperdoll(Entity entity)
    {
        if (entity == null)
            return "entity=null";
        return Paperdoll(entity.Appearance as PlayerAppearance);
    }

    public static string Entity(Entity entity)
    {
        if (entity == null)
            return "entity=null";
        int id = entity.Identity != null ? entity.Identity.Id : 0;
        return entity.GetType().Name + " go=" + entity.name + " id=" + id;
    }

    public static string ArmorVisual(Armor armor, CharacterRaceAnimation race, int itemId, ItemSlot slot)
    {
        string model = "";
        string texture = "";
        int raceIndex = (int)race;
        if (armor != null && armor.Armorgrp != null)
        {
            string[] models = armor.Armorgrp.FirstModel;
            if (models != null && raceIndex >= 0 && raceIndex < models.Length)
                model = models[raceIndex] ?? "";
            string[] textures = armor.Armorgrp.FirstTexture;
            if (textures != null && raceIndex >= 0 && raceIndex < textures.Length)
                texture = textures[raceIndex] ?? "";
        }

        return "id=" + itemId +
            " slot=" + slot +
            " race=" + race +
            " mesh=" + (string.IsNullOrEmpty(model) ? "(empty)" : model) +
            " texture=" + (string.IsNullOrEmpty(texture) ? "(empty)" : texture) +
            " meshPath=" + ArmorMeshPath(model) +
            " materialPath=" + ArmorMaterialPath(texture);
    }

    public static string ArmorMeshPath(string model)
    {
        if (string.IsNullOrEmpty(model))
            return "(none)";
        string[] parts = model.Split('.');
        if (parts.Length < 2)
            return model;
        return "Data/Animations/" + parts[0] + "/" + parts[1];
    }

    public static string ArmorMaterialPath(string texture)
    {
        if (string.IsNullOrEmpty(texture))
            return "(none)";
        string[] parts = texture.Split('.');
        if (parts.Length < 2)
            return texture;
        return "Data/SysTextures/" + parts[0] + "/Materials/" + parts[1];
    }
}
