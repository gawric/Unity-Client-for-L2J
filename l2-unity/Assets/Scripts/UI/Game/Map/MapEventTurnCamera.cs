using UnityEngine;

[RequireComponent(typeof(Transform))]
public class MapEventTurnCamera : MonoBehaviour, IRegisteredBillboard
{
    void OnEnable()
    {
        RegisteredBillboards.Register(this);
    }

    void OnDisable()
    {
        RegisteredBillboards.Unregister(this);
    }

    public void OnCameraPreRender(Camera camera)
    {
        if (camera == null) return;

        //Debug.Log("Rotate Camera Is Turn");
        MapWindow.Instance.TurnMarker(camera);
    }
}
