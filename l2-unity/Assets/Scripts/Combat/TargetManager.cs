using System;
using UnityEngine;
using VContainer;

public class TargetManager : MonoBehaviour
{
    // L2 UNetworkHandler GetNextObject type ids.
    public const int KindAllPawns = 0;
    public const int KindItem = 1;
    public const int KindEnemy = 2;
    public const int KindFriend = 3;
    public const int KindParty = 4;
    public const int KindNpc = 5;
    public const int KindCreature = 6;

    [Inject] GameClient _gameClient;
    private TargetData _target = null;
    private TargetData _attackTarget = null;
    private Transform _playerTransform;

    [SerializeField] private float _maximumTargetDistance = 15f;
    [SerializeField] private LayerMask _entityMask;
    private string defaultHexColor = "#ffffff";

    private int _cycleKind = int.MinValue;
    private float _cycleLastDistance;
    private int _cycleLastObjectId = -1;

    private Action<Entity> _considerCandidate;
    private int _scanKind;
    private int _scanSelfId;
    private int _scanExcludeId;
    private float _scanMinDistance;
    private float _scanBestDistance;
    private Vector3 _scanOrigin;
    private Entity _scanBest;

    public TargetData Target { get { return _target; } }
    public TargetData AttackTarget { get { return _attackTarget; } }

    private static TargetManager _instance;
    public static TargetManager Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _considerCandidate = ConsiderCandidate;
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

    void Update()
    {
        if (PlayerEntity.Instance == null || !HasTarget())
        {
            return;
        }

        PlayerController player = PlayerController.Instance;
        if (player == null)
        {
            return;
        }

        _target.Distance = VectorUtils.Distance2D(
            player.transform.position,
            _target.Data.ObjectTransform.position);
    }

    private bool TryResolvePlayerTransform()
    {
        if (_playerTransform != null)
        {
            return true;
        }

        if (PlayerEntity.Instance != null && PlayerEntity.Instance.transform != null)
        {
            _playerTransform = PlayerEntity.Instance.transform;
            return true;
        }

        return false;
    }

    public void NextTarget()
    {
        NextTarget(KindAllPawns);
    }

    public void NextTarget(int kind)
    {
        if (PlayerEntity.Instance == null || PlayerEntity.Instance.Identity == null)
        {
            return;
        }

        if (!TryResolvePlayerTransform())
        {
            return;
        }

        World world = IncomingPacketActions.GameWorld;
        if (world == null)
        {
            return;
        }

        if (kind != _cycleKind)
        {
            _cycleKind = kind;
            _cycleLastDistance = 0f;
            _cycleLastObjectId = -1;
        }
        else if (_cycleLastObjectId > 0)
        {
            Entity last = world.GetEntityNoLockSync(_cycleLastObjectId);
            if (last != null && last.transform != null)
            {
                _cycleLastDistance = VectorUtils.Distance2D(_playerTransform.position, last.transform.position);
            }
            else
            {
                _cycleLastDistance = 0f;
                _cycleLastObjectId = -1;
            }
        }

        Entity next = GetNextObject(world, kind, _maximumTargetDistance, _cycleLastDistance, _cycleLastObjectId);
        if (next == null)
        {
            next = GetNextObject(world, kind, _maximumTargetDistance, 0f, _cycleLastObjectId);
        }

        if (next == null)
        {
            next = GetNextObject(world, kind, _maximumTargetDistance, 0f, -1);
        }

        if (next == null || next.Identity == null || next.transform == null)
        {
            return;
        }

        _cycleLastObjectId = next.Identity.Id;
        _cycleLastDistance = VectorUtils.Distance2D(_playerTransform.position, next.transform.position);

        SetTarget(new ObjectData(next.gameObject), defaultHexColor);
        SendAction(next);
    }

    public void NextTargetById(int id , string hexColor)
    {
        World world = IncomingPacketActions.GameWorld;
        Entity found = world != null ? world.GetEntityNoLockSync(id) : null;
        if (found == null &&
            PlayerEntity.Instance != null &&
            PlayerEntity.Instance.Identity != null &&
            PlayerEntity.Instance.Identity.Id == id)
        {
            found = PlayerEntity.Instance;
        }

        if (found == null)
        {
            return;
        }

        SetTarget(new ObjectData(found.gameObject), hexColor);
    }

    private Entity GetNextObject(World world, int kind, float radius, float minDistanceExclusive, int excludeId)
    {
        _scanKind = kind;
        _scanSelfId = PlayerEntity.Instance.Identity.Id;
        _scanExcludeId = excludeId;
        _scanMinDistance = minDistanceExclusive;
        _scanBestDistance = radius;
        _scanOrigin = _playerTransform.position;
        _scanBest = null;
        world.ForEachEntity(_considerCandidate);
        return _scanBest;
    }

    private void ConsiderCandidate(Entity entity)
    {
        if (entity == null || entity.Identity == null || entity.transform == null)
        {
            return;
        }

        int id = entity.Identity.Id;
        if (id == _scanSelfId || id == _scanExcludeId)
        {
            return;
        }

        if (!MatchesKind(_scanKind, entity))
        {
            return;
        }

        if (entity.IsDead())
        {
            return;
        }

        float dist = VectorUtils.Distance2D(_scanOrigin, entity.transform.position);
        if (dist <= _scanMinDistance || dist >= _scanBestDistance)
        {
            return;
        }

        _scanBest = entity;
        _scanBestDistance = dist;
    }

    private static bool MatchesKind(int kind, Entity entity)
    {
        switch (kind)
        {
            case KindItem:
                return entity is ItemEntity;
            case KindNpc:
                return entity is NpcEntity;
            case KindCreature:
                return entity is MonsterEntity;
            case KindEnemy:
            case KindFriend:
            case KindParty:
                return entity is UserEntity;
            default:
                return entity is UserEntity || entity is NpcEntity || entity is MonsterEntity;
        }
    }

    private void SendAction(Entity entity)
    {
        if (entity == null || entity.Identity == null)
        {
            return;
        }

        GameClient game = _gameClient != null ? _gameClient : IncomingPacketActions.Game;
        if (game == null)
        {
            return;
        }

        var l2jpos = entity.Identity.GetL2jPos();
        game.Send(new ClickActionCommand(
            entity.Identity.Id, (int)l2jpos.x, (int)l2jpos.y, (int)l2jpos.z, 0));
    }

    public void SetTarget(ObjectData target , string hexColor)
    {
        if (target == null)
        {
            ClearTarget();
            return;
        }

        // New select is Target (blue) until AttackDto/AutoAttackStart marks attack target again.
        int newId = -1;
        Entity newEntity = target.Entity;
        if (newEntity == null && target.ObjectTransform != null)
        {
            newEntity = target.ObjectTransform.GetComponent<Entity>();
        }

        if (newEntity != null && newEntity.Identity != null)
        {
            newId = newEntity.Identity.Id;
        }

        if (_target != null && _target.Identity != null && _target.Identity.Id != newId)
        {
            ClearAttackTarget();
        }

        _target = new TargetData(target);
        _target.SetColor(hexColor);

        PlayerEntity.Instance.TargetId = _target.Identity.Id;
        PlayerEntity.Instance.Target = _target.Data.ObjectTransform;
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
        if (_target == null || _attackTarget == null)
        {
            return false;
        }

        if (_target.Identity == null || _attackTarget.Identity == null)
        {
            return _target == _attackTarget;
        }

        return _target.Identity.Id == _attackTarget.Identity.Id;
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
                GameClient game = _gameClient != null ? _gameClient : IncomingPacketActions.Game;
                if (game != null)
                    game.Send(new RequestTargetCanceldCommand());
                PlayerEntity.Instance.TargetId = -1;
                PlayerEntity.Instance.Target = null;
            }

            _target = null;
            _attackTarget = null;
        }
    }
}
