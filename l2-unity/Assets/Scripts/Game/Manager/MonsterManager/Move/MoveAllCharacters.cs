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

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _moveDict = new Dictionary<int, MovementData>();
            _rotateDict = new Dictionary<int, RotateData>();
            _removeListMove = new List<int>();
            _removeListRotate = new List<int>();

        }
        else
        {
            Destroy(this);
        }
    }

    void Update()
    {
        if (_rotateDict.Count == 0) return;

        foreach (KeyValuePair<int, RotateData> kvp in _rotateDict)
        {
            RotateData data = kvp.Value;
            bool isRotate = data.IsRotate();

            if (isRotate)
            {
                if (data.IsEntityTarget() & data.IsEntityMonster())
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
        if (!_moveDict.ContainsKey(id))
        {

            _moveDict.Add(id, data);
        }
        else
        {
            _moveDict[id] = data;
        }
    }



    public void CancelMove(int objId)
    {
        if (_moveDict.ContainsKey(objId))
        {
            MovementData data = _moveDict[objId];
            data.SetIsMove(false);
        }
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

    void FixedUpdate()
    {
        if (_moveDict.Count == 0) return;

        foreach (KeyValuePair<int, MovementData> kvp in _moveDict)
        {
            MovementData data = kvp.Value;
            bool isMove = data.IsMove();
            MovementTarget _movementTarget = data.GetMovementTarget();

            if (isMove == true | data.IsEntity())
            {
                Vector3 target = _movementTarget.GetTarget();

                if (target == null)
                    return;

                MoveToPoint(kvp.Key, target, data);
            }
            else
            {

                _removeListMove.Add(kvp.Key);
            }

        }

        RemoveFinishKeyMove();

        //Debug.Log("MoveAllCharacters FixedUpdate Size " + _moveDict.Count);
    }

    private void MoveToPoint(int key, Vector3 target, MovementData data)
    {

        if (target != null)
        {
            float stopDistance = data.GetDistance();
            float speed = data.GetSpeed();
            bool isMove = data.IsMove();

            if (!data.IsEntity()) {
                _removeListMove.Add(key);
                return;
            }

            Transform transform = data.GetTransform();

            float distance = VectorUtils.Distance2D(transform.position, target);

            if (distance >= stopDistance)
            {

                var gravityOffTransform = new Vector3(transform.position.x, 0, transform.position.z);
                var gravityOffTarget = new Vector3(target.x, 0, target.z);

                Vector3 point = gravityOffTarget - gravityOffTransform;
                Vector3 direction = point.normalized;

                if (isMove)
                {
                    data.Move(direction, speed);

                    if (data.GetLastPosition() == transform.position)
                    {
                        data.OnFinish(target);
                        _removeListMove.Add(key);
                        //Debug.Log("MoveAllCharacters Stop Run3");
                    }
                }
                else
                {
                    _removeListMove.Add(key);

                }
            }
            else
            {
                data.OnFinish(target);
                _removeListMove.Add(key);

            }

            data.SetLastPosition(transform.position);
        }
    }

    private void TurnTowardsAttacker(Vector3 attackerPosition, Transform mTranform)
    {
        Vector3 directionToAttacker = (attackerPosition - mTranform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
        //default 25
        mTranform.rotation = Quaternion.Slerp(mTranform.rotation, lookRotation, Time.deltaTime * 15f);
    }

    private void RemoveFinishKeyMove()
    {
        foreach (int key in _removeListMove)
        {
            if (_moveDict.ContainsKey(key))
            {
                _moveDict.Remove(key);
            }
        }

        _removeListMove.Clear();
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
        _rotateDict.Clear(); 
        _removeListMove.Clear();
        _removeListRotate.Clear();

        _moveDict = null;
        _rotateDict = null;
        _removeListMove = null;
        _removeListRotate = null;
        _rotateDict = null;
        _removeListMove = null;
        _removeListRotate = null;


        Destroy(gameObject);
    }

}
