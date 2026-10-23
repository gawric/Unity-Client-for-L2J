using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SocialPlatforms;

public class MoveAllCharacters : MonoBehaviour, IMoveAllCharacters
{
    private static IMoveAllCharacters _instance;
    public static IMoveAllCharacters Instance { get { return _instance; } }

    private Dictionary<int, MovementData> _moveDict;
    private Dictionary<int, RotateData> _rotateDict;
    private List<int> _removeListMove;
    private List<int> _removeListRotate;
    private Dictionary<int, MovementData> _stoppedMoveData;
    private List<int> _reconciledStoppedMoves;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _moveDict = new Dictionary<int, MovementData>();
            _rotateDict = new Dictionary<int, RotateData>();
            _removeListMove = new List<int>();
            _removeListRotate = new List<int>();
            _stoppedMoveData = new Dictionary<int, MovementData>();
            _reconciledStoppedMoves = new List<int>();
            CrowdAnimTest.EnsureTimeCap();

        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        CrowdAnimTest.EnsureTimeCap();
        StepMoves();

        if (_rotateDict.Count == 0) return;

        foreach (KeyValuePair<int, RotateData> kvp in _rotateDict)
        {
            RotateData data = kvp.Value;
            bool isRotate = data.IsRotate();

            if (isRotate)
            {
                if (data.IsMonsterDead())
                {
                    _removeListRotate.Add(kvp.Key);
                }
                else if (data.IsEntityTarget() & data.IsEntityMonster())
                {
                    Transform _targetRotation = data.GetTargetTransform();
                    Transform transform = data.GetMonsterTransform();
                    float angleThreshold = data.GetAngleThreshold();

                    if (!VectorUtils.IsFacingAttacker(transform, _targetRotation.position, angleThreshold))
                    {

                        TurnTowardsAttacker(_targetRotation.position, transform);
                    }
                    else
                    {
                        data.SetRotate(false);
                        _removeListRotate.Add(kvp.Key);
                    }
                }
                else
                {
                    _removeListRotate.Add(kvp.Key);
                }

            }
        }

        RemoveFinishKeyRotate();
    }

    public void AddRotate(int id, RotateData data)
    {
        if (!_rotateDict.ContainsKey(id))
        {

            _rotateDict.Add(id, data);
        }
        else
        {
            _rotateDict[id] = data;
        }
    }
    public void AddMoveData(int id, MovementData data)
    {
        MovementData prev;
        if (_moveDict.TryGetValue(id, out prev) && prev != null && prev.IsMove())
            data.InheritMoveTimer(prev);
        _moveDict[id] = data;
    }



    public void CancelMove(int objId)
    {
        if (_moveDict.ContainsKey(objId))
        {
            MovementData data = _moveDict[objId];
            data.SetIsMove(false);
        }
    }

    public void CancelRotate(int objId)
    {
        if (_rotateDict.ContainsKey(objId))
            _rotateDict.Remove(objId);
    }

    public void Forget(int objId)
    {
        _moveDict.Remove(objId);
        _rotateDict.Remove(objId);
        _stoppedMoveData.Remove(objId);
        _removeListMove.Remove(objId);
        _removeListRotate.Remove(objId);
        _reconciledStoppedMoves.Remove(objId);
    }

    public bool IsMoving(int objId)
    {
        if (_moveDict.ContainsKey(objId))
        {
            MovementData data = _moveDict[objId];
            return data.IsMove();
        }

        return false;
    }

    public MovementData GetMove(int objId)
    {
        MovementData data;
        return _moveDict.TryGetValue(objId, out data) ? data : null;
    }

    void StepMoves()
    {
        ReconcileStoppedMoves();
        if (_moveDict.Count == 0)
        {
            MoveCostLog.NoteStep(0, 0, 0);
            MoveCostLog.EndFrame();
            return;
        }

        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        int moving = 0;
        foreach (KeyValuePair<int, MovementData> kvp in _moveDict)
        {
            MovementData data = kvp.Value;
            bool isMove = data.IsMove();
            MovementTarget movementTarget = data.GetMovementTarget();

            if (isMove == true | data.IsEntity())
            {
                if (movementTarget == null || !movementTarget.TryGetTarget(out Vector3 target))
                {
                    _removeListMove.Add(kvp.Key);
                    continue;
                }

                if (isMove)
                    moving++;
                MoveToPoint(kvp.Key, target, data);
            }
            else
            {

                _removeListMove.Add(kvp.Key);
            }

        }

        RemoveFinishKeyMove();
        double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        MoveCostLog.NoteStep(moving, moving, ms);
        MoveCostLog.EndFrame();
    }

    private void MoveToPoint(int key, Vector3 target, MovementData data)
    {
        if (!data.IsEntity())
        {
            _removeListMove.Add(key);
            return;
        }

        Transform transform = data.GetTransform();
        if (transform == null)
        {
            _removeListMove.Add(key);
            return;
        }

        if (data.IsOwnerDead())
        {
            data.SetIsMove(false);
            _removeListMove.Add(key);
            data.SetLastPosition(transform.position);
            return;
        }

        Vector3 before = transform.position;
        if (!data.IsMove())
        {
            _removeListMove.Add(key);
            data.SetLastPosition(before);
            return;
        }

        float stopDistance = data.GetDistance();
        float dx = target.x - before.x;
        float dz = target.z - before.z;
        float distSq = dx * dx + dz * dz;
        if (distSq < stopDistance * stopDistance)
        {
            data.OnFinish(target);
            _removeListMove.Add(key);
            data.SetLastPosition(transform.position);
            return;
        }

        if (distSq <= 0.0001f)
        {
            data.SetLastPosition(transform.position);
            return;
        }

        data.AdvanceMoveTimer();
        data.SyncUserGait();
        float speed = data.GetSpeed();
        if (L2PawnWalk.IsCharInfoPawn(data.GetEntity()))
        {
            data.StepCharInfo(target, speed);
        }
        else
        {
            Vector3 direction = new Vector3(dx, 0f, dz);
            float dist = Mathf.Sqrt(distSq);
            direction.x /= dist;
            direction.z /= dist;

            // Clamp the final step so the pawn lands exactly on target instead
            // of overshooting it. Interlude calcVelocity does Dest/dt when the
            // remaining distance is shorter than one frame step; we reproduce that
            // as dist/dt here so monsters/NPCs stop without a cut-off jerk.
            float stepLength = speed * Time.deltaTime;
            if (stepLength >= dist)
                speed = dist / Time.deltaTime;

            data.Move(direction, speed);
        }

        data.SetLastPosition(transform.position);
    }

    private void TurnTowardsAttacker(Vector3 attackerPosition, Transform mTranform)
    {
        Vector3 directionToAttacker = attackerPosition - mTranform.position;
        directionToAttacker.y = 0f;
        if (directionToAttacker.sqrMagnitude < 0.0001f)
            return;
        Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
        //default 25
        mTranform.rotation = Quaternion.Slerp(mTranform.rotation, lookRotation, Time.deltaTime * 15f);
    }

    private void RemoveFinishKeyMove()
    {
        foreach (int key in _removeListMove)
        {
            MovementData data;
            if (_moveDict.TryGetValue(key, out data))
            {
                _stoppedMoveData[key] = data;
                _moveDict.Remove(key);
            }
        }

        _removeListMove.Clear();
    }

    private void ReconcileStoppedMoves()
    {
        if (_stoppedMoveData.Count == 0)
            return;

        foreach (KeyValuePair<int, MovementData> pair in _stoppedMoveData)
        {
            MovementData current;
            if (_moveDict.TryGetValue(pair.Key, out current) &&
                current != null &&
                current.IsMove())
            {
                _reconciledStoppedMoves.Add(pair.Key);
                continue;
            }

            if (pair.Value == null || pair.Value.ReconcileStoppedAnimation())
                _reconciledStoppedMoves.Add(pair.Key);
        }

        for (int i = 0; i < _reconciledStoppedMoves.Count; i++)
            _stoppedMoveData.Remove(_reconciledStoppedMoves[i]);
        _reconciledStoppedMoves.Clear();
    }

    private void RemoveFinishKeyRotate()
    {
        foreach (int key in _removeListRotate)
        {
            if (_rotateDict.ContainsKey(key))
            {
                _rotateDict.Remove(key);
            }
        }

        _removeListRotate.Clear();
    }

    public void OnDestroy()
    {
        _moveDict.Clear();
        _rotateDict.Clear();
        _removeListMove.Clear(); 
        _removeListRotate.Clear(); 
        _stoppedMoveData.Clear();
        _reconciledStoppedMoves.Clear();
        _rotateDict.Clear(); 
        _removeListMove.Clear();
        _removeListRotate.Clear();

        _moveDict = null;
        _rotateDict = null;
        _removeListMove = null;
        _removeListRotate = null;
        _stoppedMoveData = null;
        _reconciledStoppedMoves = null;
        _rotateDict = null;
        _removeListMove = null;
        _removeListRotate = null;


        Destroy(gameObject);
    }

}
