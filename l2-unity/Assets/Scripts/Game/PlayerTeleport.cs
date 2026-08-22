using UnityEngine;

public class PlayerTeleport : MonoBehaviour
{
    private Vector3 _teleportPosition;
    private bool _isTeleporting;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
        
    //}

    
    void Update()
    {
        if (_isTeleporting)
        {
            _isTeleporting = false;
            transform.position = _teleportPosition;
        }
    }

    public void TeleportTo(Vector3 teleportPosition)
    {
        // Snap to terrain before reveal — server Z often floats above mesh.
        _teleportPosition = GroundSnapHelper.SnapToGroundOrKeep(teleportPosition);
        _isTeleporting = true;
    }

    /// <summary>World position after ground snap (set when <see cref="TeleportTo"/> runs).</summary>
    public Vector3 LastTeleportPosition => _teleportPosition;
}
