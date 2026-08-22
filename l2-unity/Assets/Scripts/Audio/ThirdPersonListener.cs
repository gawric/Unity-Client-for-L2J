using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonListener : MonoBehaviour
{
    [SerializeField] private GameObject _player, _cam;
    [SerializeField] private int _listener;

    public GameObject Player { get { return _player; } }
    public GameObject Cam { get { return _cam; } set { _cam = value; } }

    private FMOD.ATTRIBUTES_3D _attributes = new FMOD.ATTRIBUTES_3D();

    private static ThirdPersonListener _instance;
    public static ThirdPersonListener Instance { get { return _instance; } }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        }

        if (_cam == null && Camera.main != null) {
            _cam = Camera.main.gameObject;
        }
    }

    private void OnEnable() {
        _instance = this;
    }

    private void OnDisable() {
        if (_instance == this) {
            _instance = FindActiveListener();
        }
    }

    void OnDestroy() {
        if (_instance == this) {
            _instance = null;
        }
    }

    private ThirdPersonListener FindActiveListener() {
        ThirdPersonListener[] listeners = FindObjectsByType<ThirdPersonListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < listeners.Length; i++) {
            if (listeners[i] != null && listeners[i] != this && listeners[i].isActiveAndEnabled) {
                return listeners[i];
            }
        }

        return null;
    }

    void Update() {
        if(_player == null) {
            if(PlayerController.Instance != null) {
                _player = PlayerController.Instance.gameObject;
            }
        } else {
            _attributes.position = FMODUnity.RuntimeUtils.ToFMODVector(_player.transform.position);
        }
        
        _attributes.forward = FMODUnity.RuntimeUtils.ToFMODVector(_cam.transform.forward);
        _attributes.up = FMODUnity.RuntimeUtils.ToFMODVector(_cam.transform.up);
        FMODUnity.RuntimeManager.StudioSystem.setListenerAttributes(_listener, _attributes);
    }
}

