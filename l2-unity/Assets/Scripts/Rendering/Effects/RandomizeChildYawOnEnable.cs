using UnityEngine;

public class RandomizeChildYawOnEnable : MonoBehaviour
{
    [SerializeField] private Transform[] _targets;
    [SerializeField] private Vector2 _yawRange = new Vector2(0f, 360f);
    [SerializeField] private bool _randomizeOnEnable = true;

    private Quaternion[] _baseLocalRotations;
    private bool _cached;

    private void Awake()
    {
        CacheBaseRotations();
    }

    private void OnEnable()
    {
        if (_randomizeOnEnable)
        {
            ApplyRandomYaw();
        }
    }

    public void ApplyRandomYaw()
    {
        CacheBaseRotations();

        float yaw = Random.Range(_yawRange.x, _yawRange.y);
        Quaternion yawOffset = Quaternion.Euler(0f, yaw, 0f);

        for (int i = 0; i < _targets.Length; i++)
        {
            Transform target = _targets[i];
            if (target == null)
            {
                continue;
            }

            target.localRotation = yawOffset * _baseLocalRotations[i];
        }
    }

    private void CacheBaseRotations()
    {
        if (_cached)
        {
            return;
        }

        if (_targets == null || _targets.Length == 0)
        {
            int childCount = transform.childCount;
            _targets = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                _targets[i] = transform.GetChild(i);
            }
        }

        _baseLocalRotations = new Quaternion[_targets.Length];
        for (int i = 0; i < _targets.Length; i++)
        {
            _baseLocalRotations[i] = _targets[i] != null ? _targets[i].localRotation : Quaternion.identity;
        }

        _cached = true;
    }
}
