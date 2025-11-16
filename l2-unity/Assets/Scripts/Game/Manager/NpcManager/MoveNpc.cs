using UnityEngine;



public class MoveNpc : MonoBehaviour
{
    //setting Rotate
    private Transform _targetObj;

    private float rotationSpeed = 40f; // �������� ��������
    private bool _isRotate = false;
    public bool _isFollow = false;
    private bool _isMove = false;
    private Quaternion _lastRotation; // ������ ������� 
    public float rotationThreshold = 0.01f; // ����� ��������� ��� ����������� ��������

    //setting Follow
    private ModelMovePawn _targetModelObj;
    //private Vector3 _targetPosFollow;
    private Vector3 _targetMove;
    private float _verticalVelocity = 0;
    private float _gravity = 28;
    private CharacterController _controller;
    private NpcEntity _entity;
    private NpcStateMachine _stateMachine;

    private Vector3 _flatTransformPos;
    private Vector3 _lastPos;

    private bool _isTeleport = false;
    private bool _detectedIsMove = false;
    private Vector3 _lastPosSpecial = Vector3.zero;
    private DrawLine _debugLine;
    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _entity = GetComponent<NpcEntity>();
        _stateMachine = GetComponent<NpcStateMachine>();
        //var _debugObject = GameObject.FindWithTag("DebugMove");
        //_debugLine = _debugObject.GetComponent<DrawLine>();
    }


    public bool IsSpecialDetectedIsMoveObject()
    {
        if (_isFollow == true | _isMove == true) return true;

        if (transform.position == _lastPosSpecial)
        {
            return false;
        }

        _lastPosSpecial = transform.position;

        return _detectedIsMove;
    }



    private void FixedUpdate()
    {
    
            if (_isRotate)
            {
                // ���������, ��������� �� �������
                if (IsRotating())
                {
                    Debug.Log("�������� ��������������. " + transform.position);
                }
                else
                {
                    if (_targetObj != null)
                    {
                        RotateObject();
                    }

                }
                _lastRotation = transform.rotation;
            }

            if (_isFollow)
            {

                _flatTransformPos = new Vector3(transform.position.x, 0, transform.position.z);

                if (!_entity.IsDead())
                {

                    var cVector = new Vector3(_targetModelObj.TarEntity().transform.position.x, 0, _targetModelObj.TarEntity().transform.position.z);

                    if (Vector2.Distance(cVector, _flatTransformPos) <= _targetModelObj.Distance())
                    {
                        OnFinish(_targetModelObj.TarEntity().transform.position);
                        _isFollow = false;
                    }

                    if (Vector2.Distance(cVector, _flatTransformPos) >= _targetModelObj.Distance())
                    {
                        MoveToPoint(_targetModelObj.TarEntity().transform.position, true);
                    }
                }
            }
            else if (_isMove == true)
            {
                if (_targetMove == null)
                    return;
                //Debug.Log("NPC MOVEEEEEEEEEEEE POSIIITIIIION");
                MoveToPoint(_targetMove, false);
            }

        
    }



    public void CancelMove()
    {
        if (_isMove)
        {
            _isMove = false;
        }
    }

    public float stoppingDistance = 0.1f; // 
    //public float monsterSpeed = 0.38f;
    private void MoveToPoint(Vector3 target, bool ignoreDist)
    {

        if (target != null)
        {
            var gravityOffTransform = new Vector3(transform.position.x, 0, transform.position.z);
            var gravityOffTarget = new Vector3(target.x, 0, target.z);
            float monsterSpeed = GetMonsterSpeed();
            // ��������� ���������� �� ����
            float distance = Vector2.Distance(gravityOffTarget, gravityOffTransform);


            if (distance >= stoppingDistance | ignoreDist == true)
            {

                Vector3 point = gravityOffTarget - gravityOffTransform;
                Vector3 direction = point.normalized;
                direction = ApplyGravity(direction);

                if (_isTeleport == false)
                {
                    _detectedIsMove = true;
                    _controller.Move(direction * monsterSpeed * Time.deltaTime);

                    // ������������ NPC ����� � ����
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5.0f); // ������� �������
                }

                if (_lastPos == transform.position)
                {
                    OnFinish(target);
                }
            }
            else
            {
                OnFinish(target);
            }

            _lastPos = transform.position;
        }
    }

    private void OnFinish(Vector3 target)
    {
        // Debug.Log("MoveMonster: �� �������� ����� ���������� �������������� � ����!!!! " + this.name);
        _isMove = false;

        var teset = new Vector3(target.x, 0, target.z);
        transform.position = teset;
        _detectedIsMove = false;
        _stateMachine.NotifyEvent(Event.ARRIVED);
    }

    public bool IsMoving()
    {
        return _isMove;
    }



    public void SetAccessTeleport(bool isTeleport)
    {
        //this._isTeleport = isTeleport;
    }

    private float GetMonsterSpeed()
    {
        return (_entity.Running) ? _entity.Stats.UnitySpeedRun : _entity.Stats.UnitySpeedWalking;
    }


    private bool IsRotating()
    {
        // ��������� ������� ����� ������� � ���������� ���������
        float rotationDifference = Quaternion.Angle(_lastRotation, transform.rotation);

        // ���� ������� ������ ������, ������ ������ ��������������
        return rotationDifference > rotationThreshold;
    }
    public bool IsRotate { get { return _isRotate; } }

    public void RotateInTargetObject(Transform targetObj)
    {
        _targetObj = targetObj;
        _isRotate = true;
        _lastRotation = transform.rotation;
    }

    public void StopRotateObject()
    {
        _isRotate = false;
    }

    public void MoveToPawn(ModelMovePawn targetObj)
    {
        _isFollow = true;
        _targetModelObj = targetObj;
        //ShowDrawLineDebug(transform.position, targetObj.TarObj());
    }


    public void MoveToTargetPosition(Vector3 targetVector)
    {
        _isMove = true;
        _targetMove = targetVector;
        _lastPos = transform.position;
        
    }

    public void RotateObject()
    {
        // ��������� ����������� � ����
        Vector3 direction = _targetObj.position - transform.position;

        // ���������� ��� Y, ���� ����� (��������, ��� �������� �� ���������)
        direction.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }


 


    private Vector3 ApplyGravity(Vector3 dir)
    {
        /* Handle gravity */
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < -1.25f)
            {
                _verticalVelocity = -1.25f;
            }
        }
        else
        {
            _verticalVelocity -= _gravity * Time.deltaTime;
        }
        dir.y = _verticalVelocity;

        return dir;
    }








}
