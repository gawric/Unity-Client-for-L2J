using UnityEngine;

[DisallowMultipleComponent]
public sealed class EffectPrefabRotator : MonoBehaviour
{
    [SerializeField] private Vector3 _rotationAxis = Vector3.up;
    [SerializeField] private float _degreesPerSecond = 180f;
    [SerializeField] private Space _rotationSpace = Space.Self;

    public Vector3 RotationAxis => _rotationAxis;
    public float DegreesPerSecond => _degreesPerSecond;
    public Space RotationSpace => _rotationSpace;

    private void Update()
    {
        if (_degreesPerSecond == 0f || _rotationAxis.sqrMagnitude <= 0f)
        {
            return;
        }

        transform.Rotate(_rotationAxis.normalized, _degreesPerSecond * Time.deltaTime, _rotationSpace);
    }
}
