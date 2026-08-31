using UnityEngine;

/// <summary>
/// UC Z is world-up. Bone tilt would put PTDU_Normal feet discs edge-on or under the floor.
/// </summary>
public sealed class NpcDecoKeepWorldUp : MonoBehaviour
{
    void LateUpdate()
    {
        transform.rotation = NpcDecoAttachment.UprightYaw(transform.parent);
    }
}
