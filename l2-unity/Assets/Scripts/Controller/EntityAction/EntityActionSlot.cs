using UnityEngine;

public sealed class EntityActionSlot
{
    public EntityActionKind Action { get; set; }
    public object Payload { get; set; }
    public Entity Target { get; set; }
    public Vector3 Destination { get; set; }

    public void Write(EntityActionKind action, object payload, Entity target, Vector3 destination)
    {
        Action = action;
        Payload = payload;
        Target = target;
        Destination = destination;
    }

    public float PawnDist { get; set; }
    public Entity CollisionPawn { get; set; }
    /// <summary>
    /// Incremented on each remote melee AttackDto. MonsterStateAtk must not
    /// SwitchToIdle a previous swing after a newer packet already restarted atk01.
    /// </summary>
    public int AttackSwingId { get; set; }

    /// <summary>
    /// Death clip froze on last frame (Loop Time on the clip must not restart the corpse).
    /// </summary>
    public bool DeathPoseHeld { get; set; }

    public void Clear()
    {
        PawnDist = 0f;
        CollisionPawn = null;
        DeathPoseHeld = false;
        Write(EntityActionKind.Idle, null, null, Vector3.zero);
    }
}
