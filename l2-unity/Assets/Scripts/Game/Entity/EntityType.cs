using System;
using UnityEngine;

public enum EntityType
{
    Player,
    User,
    NPC,
    Monster,
    Item,
    Pawn

}

public static class EntityTypeParser {
    public static EntityType ParseEntityType(string type) {
        if (!string.IsNullOrEmpty(type) &&
            type.IndexOf("LineageNPC", StringComparison.OrdinalIgnoreCase) >= 0)
            return EntityType.NPC;
        return EntityType.Monster;
    }
}
