using UnityEngine;

/// <summary>
/// CharInfo / UserEntity step. RestoredClass Interlude:
/// APawn::calcVelocity (Pawn.h) + physWalking (PawnMove.h) + ULevel::MoveActor
/// as a transform write. No CharacterController, no PhysX pawn-vs-pawn.
/// Local player and monsters keep CharacterController.Move.
/// Y is never written: gravity/floor stay outside this step.
/// </summary>
public static class L2PawnWalk
{
    const float MinDelta = 0.0003f;
    const float MinDistSq = 1e-8f;

    public static bool IsCharInfoPawn(Entity entity)
    {
        return entity is UserEntity;
    }

    /// <summary>
    /// XY step into <paramref name="stepped"/>. False = stay put (too close / bad args).
    /// Does not write the transform.
    /// </summary>
    public static bool TryStep(
        Vector3 location,
        Vector3 destination,
        float speed,
        float deltaTime,
        out Vector3 stepped)
    {
        stepped = location;
        if (!(speed > 0f) || deltaTime <= MinDelta)
            return false;

        float dx = destination.x - location.x;
        float dz = destination.z - location.z;
        float distSq = dx * dx + dz * dz;
        if (distSq <= MinDistSq)
            return false;

        float step = speed * deltaTime;
        if (distSq <= step * step)
        {
            stepped.x = location.x + dx;
            stepped.z = location.z + dz;
            return true;
        }

        float k = step / Mathf.Sqrt(distSq);
        stepped.x = location.x + dx * k;
        stepped.z = location.z + dz * k;
        return true;
    }

    /// <summary>
    /// One physWalking iteration on XY. One sqrt only when still approaching.
    /// Arrive branch is dest snap (same as Dest/dt * dt).
    /// </summary>
    public static void PhysWalking(Transform transform, Vector3 destination, float speed, float deltaTime)
    {
        if (transform == null)
            return;
        if (!TryStep(transform.position, destination, speed, deltaTime, out Vector3 stepped))
            return;
        transform.position = stepped;
    }
}
