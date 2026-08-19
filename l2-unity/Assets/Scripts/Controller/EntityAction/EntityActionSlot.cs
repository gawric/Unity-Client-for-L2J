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

    public void Clear()
    {
        PawnDist = 0f;
        CollisionPawn = null;
        Write(EntityActionKind.Idle, null, null, Vector3.zero);
    }
}
