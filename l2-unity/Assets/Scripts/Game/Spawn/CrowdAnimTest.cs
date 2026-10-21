using UnityEngine;

/// <summary>
/// Crowd flags. CharInfo uses L2PawnWalk (no CC). Animator is on for everyone.
/// Physics: 50 Hz. Catch-up cap 200 ms (was 100 Hz / 333 ms).
/// maximumDeltaTime also clamps Time.deltaTime, so 100 ms slowed movement below 10 FPS.
/// </summary>
public static class CrowdAnimTest
{
    const float FixedDt = 0.02f;
    const float MaxDt = 0.2f;
    static bool _timeApplied;

    public static void EnsureTimeCap()
    {
        if (_timeApplied
            && Time.fixedDeltaTime == FixedDt
            && Time.maximumDeltaTime == MaxDt)
            return;
        Time.fixedDeltaTime = FixedDt;
        Time.maximumDeltaTime = MaxDt;
        _timeApplied = true;
    }

    public static void Apply(Entity entity)
    {
        EnsureTimeCap();
        if (entity is not UserEntity)
            return;
        ApplyGo(entity.gameObject);
    }

    public static void Apply(NetworkAnimationController controller)
    {
        EnsureTimeCap();
        if (controller == null)
            return;
        if (controller.GetComponent<UserEntity>() == null)
            return;
        ApplyGo(controller.gameObject);
    }

    static void ApplyGo(GameObject go)
    {
        if (go == null)
            return;

        CharacterController[] controllers = go.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterController cc = controllers[i];
            if (cc != null)
                cc.enabled = false;
        }

        Animator[] animators = go.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
                continue;
            animator.enabled = true;
            animator.speed = 1f;
            // CullCompletely freezes the Animator state machine when the mesh is
            // briefly off-camera (common for CharInfo at screen edge / behind
            // camera). Result: run clip stuck mid-stride (normT frozen) while
            // MoveAllCharacters still translates the pawn. CullUpdateTransforms
            // keeps the state machine advancing and only skips writing bones.
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }
    }
}
