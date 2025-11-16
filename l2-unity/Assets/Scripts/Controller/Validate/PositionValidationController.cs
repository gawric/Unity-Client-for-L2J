using System.Collections.Generic;
using UnityEngine;

public class PositionValidationController : MonoBehaviour
{
    //<197 unit - он передвигается пешком(игнорирует если он двигается)
    //>197 unit он прыгает останавливая движение и возвращая его когда будет перемещен

    private List<ValidateLocation> _validateList;
    private List<ValidateLocation> _validateRemove;
    private List<CharMoveToLocation> _validateInitPosition;
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
            _validateList = new List<ValidateLocation>();
            _validateRemove = new List<ValidateLocation>();
            _validateInitPosition = new List<CharMoveToLocation>();
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
                ValidateLocation validateLocation = _validateList[i];

                if (validateLocation != null)
                {
                    Entity entity = World.Instance.GetEntityNoLockSync(validateLocation.ObjectId);

                    if (entity != null && !entity.IsDead())
                    {
                        Vector3 activePosition = entity.transform.position;
                        Vector3 newPosition = validateLocation.Position;
                        float distance = VectorUtils.Distance2D(activePosition, newPosition);


                        if (distance > 0.15f && distance < _trigger)
                        {
                            StartWalk(entity, newPosition);
                        }
                        else if (distance > _trigger)
                        {
                            Jump(entity, newPosition);
                        }

                        _validateRemove.Add(validateLocation);

                    }
                    else
                    {
                        _validateRemove.Add(validateLocation);
                    }
                    //Debug.Log("Position Validate Controller---> " + entity.IdentityInterlude.Name);

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
            
            var stateMachine = monsterEntity.GetStateMachine();
            if (stateMachine != null) ReStartAnimation(monsterEntity.IdentityInterlude.Id , stateMachine);

        }else if (entity.GetType() == typeof(PlayerEntity))
        {
            Dictionary<string, float> floatValues  = AnimationManager.Instance.PlayerGetAllFloat();
            entity.HideObject();
            NewCalcGravity(PlayerController.Instance , newPosition);
            entity.ShowObject();

            AnimationManager.Instance.PlayerSetAllFloat(floatValues);
            ReStartAnimationPlayer(PlayerStateMachine.Instance);

        }else if (entity.GetType() == typeof(NpcEntity))
        {
            NpcEntity npcEntity = (NpcEntity)entity;
            npcEntity.HideObject();
            //NewCalcGravityMonster(npcEntity, newPosition);
            npcEntity.ShowObject();

            var stateMachine = npcEntity.GetStateMachine();
            if (stateMachine != null) ReStartAnimationNpc(npcEntity.IdentityInterlude.Id, stateMachine);
        }
    }

    private void NewCalcGravityNpc(Entity npcEntity, Vector3 newPosition)
    {
        newPosition.y = 0;
        Vector3 newPositionPlusGravity = VectorUtils.ApplyGravityGround(newPosition, Time.time);
        npcEntity.transform.position = newPositionPlusGravity;
    }


    private void NewCalcGravityMonster(MonsterEntity monsterEntity , Vector3 newPosition)
    {
        newPosition.y = 0;
        Vector3 newPositionPlusGravity = VectorUtils.ApplyGravityGround(newPosition, Time.time);
        monsterEntity.transform.position = newPositionPlusGravity;
    }
    private void NewCalcGravity(PlayerController playerController , Vector3 newPosition)
    {
        //reset gravity
        newPosition.y = 0;
        Vector3 newPositionPlusGravity = VectorUtils.ApplyGravityGround(newPosition, Time.time);
        playerController.transform.position = newPositionPlusGravity;
    }

    private void ReStartAnimationPlayer(PlayerStateMachine stateMachine)
    {
        if (PlayerController.Instance.RunningToDestination)
        {
            if (stateMachine.State == PlayerState.WALKING) stateMachine.ChangeState(PlayerState.WALKING);
            if (stateMachine.State == PlayerState.RUNNING) stateMachine.ChangeState(PlayerState.RUNNING);
            stateMachine.NotifyEvent(Event.MOVE_TO);
        }
    }

    private void ReStartAnimation(int mObjId , MonsterStateMachine stateMachine)
    {
        if (MoveAllCharacters.Instance.IsMoving(mObjId))
        {
            if (stateMachine.State == MonsterState.WALKING) stateMachine.ChangeState(MonsterState.WALKING);
            if (stateMachine.State == MonsterState.RUNNING) stateMachine.ChangeState(MonsterState.RUNNING);
            stateMachine.NotifyEvent(Event.MOVE_TO);
        }
    }

    private void ReStartAnimationNpc(int mObjId, NpcStateMachine stateMachine)
    {
        if (MoveAllCharacters.Instance.IsMoving(mObjId))
        {
            if (stateMachine.State == NpcState.WALKING) stateMachine.ChangeState(NpcState.WALKING);
            if (stateMachine.State == NpcState.RUNNING) stateMachine.ChangeState(NpcState.RUNNING);
            stateMachine.NotifyEvent(Event.MOVE_TO);
        }
    }
    private void StartWalk(Entity entity , Vector3 newPosition)
    {
        if (entity.GetType() == typeof(MonsterEntity))
        {
            MonsterEntity monsterEntity = (MonsterEntity)entity;
            var stateMachine = monsterEntity.GetStateMachine();
            if (stateMachine == null || (stateMachine.State == MonsterState.WALKING || stateMachine.State == MonsterState.RUNNING)) return;
            stateMachine.ChangeIntention(MonsterIntention.INTENTION_MOVE_TO , newPosition);

        }else if (entity.GetType() == typeof(NpcEntity))
        {
            NpcEntity npcEntity = (NpcEntity)entity;
            var stateMachine = npcEntity.GetStateMachine();
            if (stateMachine == null || (stateMachine.State == NpcState.WALKING || stateMachine.State == NpcState.RUNNING)) return;
            stateMachine.ChangeIntention(NpcIntention.INTENTION_MOVE_TO, newPosition);
        }

    }
    public void AddValidateLocation(ValidateLocation validateLocation)
    {
        if (!_validateList.Contains(validateLocation))
        {
            _validateList.Add(validateLocation);
        }
    }

    public void AddInitPosition(CharMoveToLocation location)
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
                CharMoveToLocation location =  _validateInitPosition[i];
                Entity entity = World.Instance.GetEntityNoLockSync(location.ObjId);

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
