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
        this._teleportPosition = teleportPosition;
        _isTeleporting = true;
        
    }
}
