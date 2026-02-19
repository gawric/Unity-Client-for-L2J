using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TargetManager : MonoBehaviour
{
    private TargetData _target = null;
    private TargetData _attackTarget = null;
    private Transform _playerTransform;

    [SerializeField] private float _maximumTargetDistance = 15f;
    [SerializeField] private LayerMask _entityMask;
    private LayerMask _areaMask = 8192;
    [SerializeField] private RaycastHit[] _entitiesInRange;
    [SerializeField] private List<Transform> _visibleEntities;
    [SerializeField] private int _nextTargetIndex;
    private string defaultHexColor = "#ffffff";

    public TargetData Target { get { return _target; } }
    public TargetData AttackTarget { get { return _attackTarget; } }

    private static TargetManager _instance;
    public static TargetManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        _target = null;
        _attackTarget = null;
    }

    void OnDestroy()
    {
        _instance = null;
    }

    public void SetMask(LayerMask mask)
    {
        _entityMask = mask;
    }

    private void FixedUpdate()
    {
        if (_playerTransform == null)
        {
            if (PlayerEntity.Instance != null && PlayerEntity.Instance.transform != null)
            {
                _playerTransform = PlayerEntity.Instance.transform;
            }
            else
            {
                return;
            }
        }

        VerifyNextTargetIndex();

        _entitiesInRange = Physics.SphereCastAll(_playerTransform.position, _maximumTargetDistance, transform.forward, 0, _entityMask);
        _visibleEntities = GetVisibleEntities();
    }

    private List<Transform> GetVisibleEntities()
    {
        List<Transform> visibleEntities = new List<Transform>();

        for (int i = 0; i < _entitiesInRange.Length; i++)
        {
            Transform entity = _entitiesInRange[i].transform;
            if (IsTransformVisible(entity))
            {
                visibleEntities.Add(entity);
            }
        }

        visibleEntities = visibleEntities
            .OrderBy(obj => Vector3.Distance(_playerTransform.position, obj.position))
            .ToList();

        return visibleEntities;
    }

    private bool IsTransformVisible(Transform target)
    {
        bool isTooFar = Vector3.Distance(_playerTransform.position, target.position) > _maximumTargetDistance;
        if (isTooFar)
        {
            return false;
        }

        return CameraController.Instance.IsObjectVisible(target);
    }

    private void VerifyNextTargetIndex()
    {
        // Is the next target out of bounds
        if (_nextTargetIndex >= _visibleEntities.Count)
        {
            _nextTargetIndex = 0;
            return;
        }

        // Is our target visible
        if (HasTarget() && !IsTransformVisible(_target.Data.ObjectTransform))
        {
            _nextTargetIndex = 0;
            return;
        }

        // Did my next target index change?
        if (HasTarget())
        {
            for (int i = 0; i < _visibleEntities.Count; i++)
            {
                if (_visibleEntities[i] == _target.Data.ObjectTransform)
                {
                    if (i != _nextTargetIndex)
                    {
                        _nextTargetIndex = 0;
                    }
                }
            }

            return;
        }
    }

    private int GetNextTargetIndex()
    {
        int index = _nextTargetIndex;

        if (index >= _visibleEntities.Count)
        {
            index = 0;
        }

        if (HasTarget() && _visibleEntities[index] == _target.Data.ObjectTransform)
        {
            index++;
        }

        if (index >= _visibleEntities.Count)
        {
            index = 0;
        }

        return index;
    }

    public void NextTarget()
    {
        _nextTargetIndex = GetNextTargetIndex();

        SetTarget(new ObjectData(_visibleEntities[_nextTargetIndex].gameObject) , defaultHexColor);
    }

    public void NextTargetById(int id , string hexColor)
    {
        var _entitiesInRange = Physics.SphereCastAll(_playerTransform.position, 50f, transform.forward, 0, _areaMask);

        foreach (var entity in _entitiesInRange)
        { 
            if(entity.transform.parent != null)
            {
                var _entity = entity.transform.parent.GetComponent<Entity>();

                if(_entity != null & _entity.IdentityInterlude.Id == id)
                {
                    var _targetData = new ObjectData(entity.transform.parent.gameObject);
                    SetTarget(_targetData , hexColor);
                }
            }
        } 
    }

    public ObjectData GetTargetByIdUseLocator(int id)
    {
        var _entitiesInRange = Physics.SphereCastAll(_playerTransform.position, 50f, transform.forward, 0, _areaMask);

        foreach (var entity in _entitiesInRange)
        {
            if (entity.transform.parent != null)
            {
                var _entity = entity.transform.parent.GetComponent<Entity>();

                if (_entity != null & _entity.IdentityInterlude.Id == id)
                {
                    return  new ObjectData(entity.transform.parent.gameObject);
                }
            }
        }
        return null;
    }

    public void SetTarget(ObjectData target , string hexColor)
    {
        if (target == null)
        {
            ClearTarget();
            return;
        }

        _target = new TargetData(target);
        _target.SetColor(hexColor);

        PlayerEntity.Instance.TargetId = _target.Identity.Id;
        PlayerEntity.Instance.Target = _target.Data.ObjectTransform;
        //GameClient.Instance.ClientPacketHandler.SendRequestSetTarget(_target.Identity.Id);
    }

    public void SetAttackTarget()
    {
        _attackTarget = _target;
    }

    public void ClearAttackTarget()
    {
        _attackTarget = null;
    }

    public bool IsAttackTargetSet()
    {
        return _target != null && _attackTarget != null && _target == _attackTarget;
    }

    public bool HasTarget()
    {
        return _target != null && _target.Data.ObjectTransform != null;
    }

    public bool HasAttackTarget()
    {
        return _attackTarget != null;
    }

    public void UnselectedTarget(int objId)
    {
         ClearTarget();
    }

    public void ClearTarget()
    {
        if (HasTarget())
        {
            if (PlayerEntity.Instance.TargetId != -1)
            {
                bool enable = GameClient.Instance.IsCryptEnabled();
                SendGameDataQueue.Instance().AddItem(CreatorPacketsUser.CreateRequestTargetCanceld(), enable, enable);
                PlayerEntity.Instance.TargetId = -1;
                PlayerEntity.Instance.Target = null;
            }

            _target = null;
            _attackTarget = null;
        }
    }

    void Update()
    {
        if (PlayerEntity.Instance == null)
        {
            return;
        }

        if (HasTarget())
        {
            _target.Distance = Vector3.Distance(
                PlayerController.Instance.transform.position,
                _target.Data.ObjectTransform.position);
        }
        else
        {
            ClearTarget();
        }
    }
}
