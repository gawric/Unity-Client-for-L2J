using UnityEngine;
using UnityEngine.Rendering;

public class CameraPreRenderUpdater : MonoBehaviour
{
    public bool OnlyWhenCameraMove = true;

    Camera _mainCam;
    Vector3 _lastPos;
    Quaternion _lastRot;

    void OnEnable()
    {
        // Кэшируем текущую Main Camera (если есть)
        _mainCam = Camera.main;
        if (_mainCam != null)
        {
            _lastPos = _mainCam.transform.position;
            _lastRot = _mainCam.transform.rotation;
        }

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        // Игнорируем все камеры, кроме Main Camera
        if (camera != _mainCam)
        {
            // Если Main Camera сменилась — обновляем кэш и продолжаем
            if (camera == Camera.main)
            {
                _mainCam = camera;
                _lastPos = _mainCam.transform.position;
                _lastRot = _mainCam.transform.rotation;
            }
            else
            {
                return;
            }
        }

       

        if (OnlyWhenCameraMove)
        {
            var pos = _mainCam.transform.position;
            var rot = _mainCam.transform.rotation;

            if (pos == _lastPos && rot == _lastRot)
                return;

            _lastPos = pos;
            _lastRot = rot;
        }

        // Вызываем всех зарегистрированных билбордов
        RegisteredBillboards.InvokeAll(_mainCam);
    }
}