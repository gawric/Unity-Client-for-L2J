using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class PositionValidationController : MonoBehaviour
{
    [Inject] World _world;

    private World GameWorld
    {
        get { return _world != null ? _world : World.Instance; }
    }

    private PlayerController Player
    {
        get { return PlayerController.Instance; }
    }
    //<197 unit - он передвигается пешком(игнорирует если он двигается)
    //>197 unit он прыгает останавливая движение и возвращая его когда будет перемещен

    private List<ValidateLocationDto> _validateList;
    private List<ValidateLocationDto> _validateRemove;
    private List<CharMoveToLocationDto> _validateInitPosition;
    private bool validTest = false;
    //197 unit | 3.743f metr
    private float _trigger = 3.743f;

    private static PositionValidationController _instance;
    public static PositionValidationController Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            _validateList = new List<ValidateLocationDto>();
            _validateRemove = new List<ValidateLocationDto>();
            _validateInitPosition = new List<CharMoveToLocationDto>();
        }
        else
        {
            Destroy(this);
        }
    }

    
    void Update()
    {
        try
        {

            ValidInitNpsPosition();

            if (_validateList.Count == 0) return;

            for (int i = 0; i < _validateList.Count; i++)
            {
                ValidateLocationDto validateLocation = _validateList[i];

                if (validateLocation != null)
                {
                    World world = GameWorld;
                    if (world == null)
                        continue;
                    Entity entity = world.GetEntityNoLockSync(validateLocation.ObjectId);

                    if (entity != null && !entity.IsDead())
                    {
                        Vector3 activePosition = entity.transform.position;
                        Vector3 newPosition = validateLocation.Position;
                        float distance = VectorUtils.Distance2D(activePosition, newPosition);


                        if (distance > 0.15f && distance < _trigger)
                        {
                            EntityActionCombatLog.LogCiPawn(entity,
                                "VL StartWalk nick=" + EntityActionCombatLog.NameOf(entity) +
                                " d=" + distance.ToString("F2") +
                                " action=" + entity.ActionSlot.Action +
                                " pos=" + EntityActionCombatLog.Vec(activePosition) +
                                " vl=" + EntityActionCombatLog.Vec(newPosition) +
                                " pawn=" + EntityActionCombatLog.Describe(EntityActionCombatLog.PawnOf(entity)));
                            StartWalk(entity, newPosition);
                        }
                        else if (distance > _trigger)
                        {
                            EntityActionCombatLog.LogCiPawn(entity,
                                "VL JUMP nick=" + EntityActionCombatLog.NameOf(entity) +
                                " d=" + distance.ToString("F2") +
                                " action=" + entity.ActionSlot.Action +
                                " type=" + entity.GetType().Name +
                                " pos=" + EntityActionCombatLog.Vec(activePosition) +
                                " vl=" + EntityActionCombatLog.Vec(newPosition) +
                                " pawn=" + EntityActionCombatLog.Describe(EntityActionCombatLog.PawnOf(entity)) +
                                " " + EntityActionCombatLog.ClassifyDest(newPosition, EntityActionCombatLog.PawnOf(entity)));
                            Jump(entity, newPosition);
                        }

                        _validateRemove.Add(validateLocation);

                    }
                    else
                    {
                        _validateRemove.Add(validateLocation);
                    }
                    //Debug.Log("Position Validate Controller---> " + entity.Identity.Name);

                }
            }

            _validateList.RemoveAll(n => _validateRemove.Contains(n));

            _validateRemove.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
       
    }


    private void Jump(Entity entity, Vector3 newPosition)
    {
        if (entity.GetType() == typeof(MonsterEntity))
        {
            MonsterEntity monsterEntity = (MonsterEntity)entity;
            monsterEntity.HideObject();
            NewCalcGravityMonster(monsterEntity, newPosition);
            monsterEntity.ShowObject();
            
            ReplayMoveIfNeeded(monsterEntity);

        }else if (entity.GetType() == typeof(PlayerEntity))
        {
            int objectId = entity.Identity.Id;
            Dictionary<string, float> floatValues  = IncomingPacketActions.Animations.PlayerGetAllFloat(objectId);
            entity.HideObject();
            NewCalcGravity(Player, newPosition);
            entity.ShowObject();

            IncomingPacketActions.Animations.PlayerSetAllFloat(objectId , floatValues);
            ReStartAnimationPlayer(PlayerStateMachine.Instance);

        }else if (entity.GetType() == typeof(NpcEntity))
        {
            NpcEntity npcEntity = (NpcEntity)entity;
            npcEntity.HideObject();
            NewCalcGravityNpc(npcEntity, newPosition);
            npcEntity.ShowObject();

            ReplayMoveIfNeeded(npcEntity);
        }
        else if (entity is UserEntity)
        {
            EntityActionCombatLog.LogCiPawn(entity,
                "VL JUMP UserEntity NO_HANDLER nick=" + EntityActionCombatLog.NameOf(entity) +
                " dest=" + EntityActionCombatLog.Vec(newPosition));
        }
    }

    private void NewCalcGravityNpc(Entity npcEntity, Vector3 newPosition)
    {
        npcEntity.transform.position = GroundSnapHelper.SnapToGroundOrKeep(newPosition);
    }


    private void NewCalcGravityMonster(MonsterEntity monsterEntity , Vector3 newPosition)
    {
        monsterEntity.transform.position = GroundSnapHelper.SnapToGroundOrKeep(newPosition);
    }
    private void NewCalcGravity(PlayerController playerController , Vector3 newPosition)
    {
        playerController.transform.position = GroundSnapHelper.SnapToGroundOrKeep(newPosition);
    }

    private void ReStartAnimationPlayer(PlayerStateMachine stateMachine)
    {
        if (Player != null && Player.RunningToDestination)
        {
            if (stateMachine.State == PlayerState.WALKING) stateMachine.ChangeState(PlayerState.WALKING);
            if (stateMachine.State == PlayerState.RUNNING) stateMachine.ChangeState(PlayerState.RUNNING);
            stateMachine.NotifyEvent(Event.MOVE_TO);
        }
    }

    private void ReplayMoveIfNeeded(Entity entity)
    {
        if (entity == null || entity.Identity == null || MoveAllCharacters.Instance == null)
            return;
        if (!MoveAllCharacters.Instance.IsMoving(entity.Identity.Id))
            return;
        EntityActionVisual.StartMove(entity, !entity.Running);
    }
    private void StartWalk(Entity entity , Vector3 newPosition)
    {
        if (entity.GetType() == typeof(MonsterEntity))
        {
            MonsterEntity monsterEntity = (MonsterEntity)entity;
            if (monsterEntity.ActionSlot.Action == EntityActionKind.Move)
                return;
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.Set(monsterEntity, EntityActionKind.Move, newPosition);

        }else if (entity.GetType() == typeof(NpcEntity))
        {
            NpcEntity npcEntity = (NpcEntity)entity;
            if (npcEntity.ActionSlot.Action == EntityActionKind.Move)
                return;
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.Set(npcEntity, EntityActionKind.Move, newPosition);
        }
        else if (entity is UserEntity)
        {
            EntityActionCombatLog.LogCiPawn(entity,
                "VL StartWalk UserEntity NO_HANDLER nick=" + EntityActionCombatLog.NameOf(entity) +
                " dest=" + EntityActionCombatLog.Vec(newPosition));
        }

    }
    public void AddValidateLocation(ValidateLocationDto validateLocation)
    {
        if (!_validateList.Contains(validateLocation))
        {
            _validateList.Add(validateLocation);
        }
    }

    public void AddInitPosition(CharMoveToLocationDto location)
    {
        _validateInitPosition.Add(location);
    }


    //This function is a test function.It solves the problem with position synchronization when the server sends data that the client needs to move.
    //But the client did not load at this moment.Therefore, we collect these packages and after loading, we find the oldest one in time and move the NPC immediately to the end of this movement vector.
    //Ideally, you need to calculate the path traveled and move the npc there, but for the sake of 2 npc it makes no sense
    private void ValidInitNpsPosition()
    {
        if (_validateInitPosition.Count > 0)
        {
            for (int i = 0; i < _validateInitPosition.Count; i++)
            {
                CharMoveToLocationDto location =  _validateInitPosition[i];
                World world = GameWorld;
                if (world == null)
                    continue;
                Entity entity = world.GetEntityNoLockSync(location.ObjId);

                if(entity != null)
                {
                    // Debug.Log("object position 1 " + entity.transform.position + " go name " + entity.name + " end position " + location.NewPosition);
                    if (entity.isActiveAndEnabled)
                    {
                        entity.HideObject();
                        NewCalcGravityNpc(entity, location.NewPosition);
                        entity.ShowObject();
                    }
                    //Debug.Log("object position 2 " + entity.transform.position + " go name " + entity.name + " end position " + location.NewPosition);
                }

            }

            _validateInitPosition.Clear();
        }
    }

    void OnDestroy()
    {
        _instance = null;
    }
}
