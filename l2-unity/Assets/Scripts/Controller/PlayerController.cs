using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /* Components */
    private CharacterController _controller;
    /*Rotate*/
    private float _finalAngle;

    /* Movement */
     private Vector3 _moveDirection;
     private bool _running = true;

    /* Gravity */
    private float _verticalVelocity = 0;
    [SerializeField] private float _jumpForce = 10;
    [SerializeField] private float _gravity = 28;


    /* Target */
    private MovementTarget _movementTarget;

    private bool _isMove = false;
    private Transform _lookAtTarget;
    private GearEffects _gearEffects;


    public void UpdateRunSpeed(float defaultRunSpeed)
    {
        _calcSpeed.UpdateSpeedRun(defaultRunSpeed);
    }

    public void UpdateWalkSpeed(float defaultWalkSpeed)
    {
        _calcSpeed.UpdateSpeedWalk(defaultWalkSpeed);
    }

    public bool RunningToDestination { get { return _isMove; } }
    public bool Running { get { return _running; } set { _running = value; } }
    public Vector3 MoveDirection { get { return _moveDirection; } }

    private static PlayerController _instance;
    public static PlayerController Instance { get { return _instance; } }

    private Entity _monster;
    private bool _isRotate = false;
    private bool _isFinish = true;
    private bool _turnsAround = true;

    private float _moveTimer = 0f;
    private float _elapsedTime = 0;

    
    private bool _isFirst = false;
  
    private bool _behindPlayer = false;
    private bool _switchWalkToRun = false;
    private bool _isRotateAttacker = false;
    private Vector3 _attackerPosition;
    public float angleThreshold = 3f;

    //newObject

    private PlayerPositionSender _pp_sender;
    private ICalcSpeed _calcSpeed;
    public void Initialize()
    {
        if (_instance == null)
        {
            _instance = this;


            _calcSpeed = new PlayerMovementSpeedCalculator();
            _pp_sender = new PlayerPositionSender();
        }
        else
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _gearEffects = GetComponent<GearEffects>();
    }


    public void Update()
    {
        if (_isRotateAttacker & !_isMove)
        {
            if (!VectorUtils.IsFacingAttacker(transform , _attackerPosition , angleThreshold))
            {
               // Debug.Log("Rotate to object Attacker running : ");
                TurnTowardsAttacker(_attackerPosition);
            }
            else
            {
               //Debug.Log("Rotate to object Attacker stop: ");
                _isRotateAttacker = false;
            }

        }
    }
 
    public void FixedUpdate()
    {
        //Если мы не двигаемся но мы в состоянии MoveToPawnDto == true тогда мы следим за обьектом двинется он или нет если двинется мы подбегаем к нему
        //Если мы получим MoveToPawnDto == false мы останавливаем слежение
        RestartMoveElseMoveToPawnTrue(ref _isMove);

        if (_isMove)
        {
            Vector3 characterPosition = VectorUtils.To2D(transform.position);
            Vector3 targetPosition = VectorUtils.To2D(_movementTarget.GetTarget());

            if(VectorUtils.Distance2D(characterPosition, targetPosition) > _movementTarget.GetDistance())
            {

                _elapsedTime = Time.time;
                _moveTimer += Time.deltaTime;
                SwitchWalkToRun();

                float finalAngle = GetFinalAngle(targetPosition, characterPosition);

                float speed = GetMovementSpeed();
                Vector3 newPosition = GetNewPosition(targetPosition, characterPosition, speed);
                Vector3 newPositionPlusGravity = ApplyGravity(newPosition , _elapsedTime);

                Quaternion targetRotation = GetNewRotate(finalAngle);
                float r_speed = _calcSpeed.GetSpeedRotate(_behindPlayer);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation , r_speed);
                
                _controller.Move(newPositionPlusGravity * Time.deltaTime);


                _isFirst = false;
                _pp_sender.TickWhileMoving(transform.position, PlayerPositionSender.ToL2Heading(transform.eulerAngles.y), Time.deltaTime);
            }
            else
            {
                 //Debug.Log("IsMoveToPawn мы на месте " + VectorUtils.Distance2D(characterPosition, targetPosition));
                 SetPositionServer(_movementTarget, targetPosition);
                 NotifyEvent(PlayerStateMachine.Instance.State);
                 StopMove();

               // Debug.Log("IsMoveToPawn запуск повтора т.е StopMove " + PlayerStateMachine.Instance.IsMoveToPawn + " state " + PlayerStateMachine.Instance.State);
            }
           

        }
    }

    private void NotifyEvent(PlayerState state)
    {
        if (state == PlayerState.RUNNING | state == PlayerState.WALKING)
        {
            PlayerStateMachine.Instance.NotifyEvent(Event.ARRIVED);
        }
    }


    public void RestartMoveElseMoveToPawnTrue(ref bool _isMove)
    {
        if (PlayerStateMachine.Instance.IsMoveToPawn & _isMove == false)
        {
            Vector3 targetPosition = VectorUtils.To2D(_movementTarget.GetTarget());
            float distance = VectorUtils.Distance2D(transform.position, targetPosition);
            if (distance > _movementTarget.GetDistance())
            {
                Debug.Log("IsMoveToPawn запуск повтора т.е IsMoveToPawn " + PlayerStateMachine.Instance.IsMoveToPawn + " state " + PlayerStateMachine.Instance.State + " Distance " + distance);
                _isMove = true;
            }
        }
    }

    public void RotateToAttacker(Vector3 attackerPosition)
    {
        _isRotateAttacker = true;
        _attackerPosition = attackerPosition;
    }

    private void TurnTowardsAttacker(Vector3 attackerPosition)
    {
        Vector3 directionToAttacker = (_attackerPosition - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToAttacker);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

   


    private void SetPositionServer(MovementTarget _movementTarget , Vector3 targetPosition)
    {
        if (_movementTarget.GetDistance() == 0.1f)
        {
            var _targetPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
            transform.position = _targetPosition;
        }
    }
    private void SwitchWalkToRun()
    {
        if (_switchWalkToRun)
            return;
        if (L2PlayerMoveGait.IsWalkStart(_moveTimer, RunMode(), false))
            return;

        _switchWalkToRun = true;
        PlayerStateMachine.Instance.ChangeState(PlayerState.RUNNING);
        PlayerStateMachine.Instance.NotifyEvent(Event.MOVE_TO);
    }

    private bool RunMode()
    {
        if (PlayerEntity.Instance != null)
            return PlayerEntity.Instance.Running;
        return _movementTarget != null && _movementTarget.IsRunningServer();
    }

    private float GetMovementSpeed()
    {
        bool walking = L2PlayerMoveGait.IsWalkStart(_moveTimer, RunMode(), false);
        return _calcSpeed.GetSpeed(!walking);
    }

    public float GetMoveTimer()
    {
        return _moveTimer;
    }

    public bool IsWalkGait()
    {
        return L2PlayerMoveGait.IsWalkStart(_moveTimer, RunMode(), false);
    }

    public float GetLiveMoveSpeed()
    {
        return GetMovementSpeed();
    }

    public float GetLiveRotateSpeed()
    {
        return _calcSpeed != null ? _calcSpeed.GetSpeedRotate(_behindPlayer) : 0f;
    }

    public float GetDesiredYawDegrees()
    {
        if (_movementTarget == null)
            return transform.eulerAngles.y;
        Vector3 characterPosition = VectorUtils.To2D(transform.position);
        Vector3 targetPosition = VectorUtils.To2D(_movementTarget.GetTarget());
        return GetFinalAngle(targetPosition, characterPosition);
    }



  

    protected float GetFinalAngle(Vector3 targetPosition, Vector3 currentPosition)
    {
        Vector3 relativeDirection = targetPosition - currentPosition;
        float angleInRadians = Mathf.Atan2(relativeDirection.x, relativeDirection.z);
        float angleInDegrees = Mathf.Rad2Deg * angleInRadians;
        angleInDegrees = (angleInDegrees + 360) % 360; // Нормализация угла
        return angleInDegrees;
    }

    private Quaternion GetNewRotate(float finalAngle)
    {
        return Quaternion.Euler(Vector3.up * finalAngle);
    }

 
    private Vector3 GetNewPosition(Vector3 targetPosition, Vector3 characterPosition , float currentSpeed)
    {

        Vector3 relativeDirection = targetPosition - characterPosition;
        return relativeDirection.normalized * currentSpeed;
    }



 

    public void MoveToPoint(MovementTarget movementTarget)
    {
        bool wasMoving = _isMove;
        ActivateIfNotMoving(_isMove);
        if (!wasMoving)
            _moveTimer = 0f;
        _movementTarget = movementTarget;
        _isMove = true;
        _behindPlayer = VectorUtils.IsTargetBehindPlayer(movementTarget.GetTarget(), transform);
    }

    public void StopMove()
    {
        _isMove = false;
        ClickManager.Instance.HideLocator();
        _moveTimer = 0f;
        _switchWalkToRun = false;

    }

    private void ActivateIfNotMoving(bool is_move)
    {
         if (!is_move)
         {
            _switchWalkToRun = false;
            _isFirst = true;
         }
    }

  



    public bool IsTurnsAround()
    {
        return _turnsAround;
    }
    

    public void StartRotateFollow(Entity monster)
    {
        _monster = monster;
        _isRotate = true;
        _turnsAround = true;
    }


    
 

    public void ThinkMoveToPawn( ModelMovePawn model)
    {
        //PlayerStateMachine.Instance.IsMoveToPawn = true;
        
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_MOVE_TO_PAWN, model);
    }

    public void InitMoveToPawn(MoveToPawnDto moveToPawnPacket)
    {
        World world = World.Instance;
        if (world == null || PlayerEntity.Instance == null)
            return;

        // Engine OnMoveToPawn uses the packet's pawn directly. It does not require
        // that pawn to be the UI-selected target. During replay the first attacked
        // goblin is still selected while this packet already follows the second one.
        Entity target = world.GetEntityNoLockSync(moveToPawnPacket.TarObjid);
        if (target == null)
        {
            ReplayAttackTrace.Write(
                "MOVE_TO_PAWN_SKIP",
                PlayerEntity.Instance,
                null,
                "targetId=" + moveToPawnPacket.TarObjid);
            return;
        }

        ReplayAttackTrace.Write(
            "MOVE_TO_PAWN",
            PlayerEntity.Instance,
            target,
            "distanceUu=" + moveToPawnPacket.DistanceUu +
            " distanceMeters=" + moveToPawnPacket.Distance.ToString("F3") +
            " selectedTargetId=" + PlayerEntity.Instance.TargetId);
        ThinkMoveToPawn(new ModelMovePawn(PlayerEntity.Instance, target, moveToPawnPacket.Distance));
    }

    
    public Vector3 ApplyGravity(Vector3 dir , float elapsedTime)
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
            _verticalVelocity -= _gravity * elapsedTime;
        }
        dir.y = _verticalVelocity;

        return dir;
    }

   


    public void UpdateFinalAngleToLookAt(Transform target)
    {
        if (target == null)
        {
            return;
        }

        float angle = Mathf.Atan2(target.position.x - transform.position.x, target.position.z - transform.position.z) * Mathf.Rad2Deg;
        angle = Mathf.Round(angle / 45f);
        angle *= 45f;
        _finalAngle = angle;
    }



    public Vector3 GetPlayerPosition()
    {
        return transform.position;
    }

    public Vector3 GetBodyPosition()
    {
        Transform tr = _gearEffects.GetBodyCenter();
        return tr.position;
    }


    public Vector3 GetCollisionSelf(Transform target)
    {
        float height = target.GetComponent<Entity>().Appearance.CollisionHeight;
        float radius = target.GetComponent<Entity>().Appearance.CollisionRadius;

        float heightC = VectorUtils.ConvertL2jDataOffset(height);
        float radiusC = VectorUtils.ConvertL2jDataOffsetRadius(radius);
        return VectorUtils.GetCollisionEffect(transform, target, heightC, radiusC);
    }

}
