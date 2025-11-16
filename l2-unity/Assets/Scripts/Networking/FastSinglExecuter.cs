using System.Threading;
using UnityEngine;

public class FastSinglExecuter : MonoBehaviour
{
    private SynchronizationContext synchronizationContext;

    private static FastSinglExecuter _instance;
    public static FastSinglExecuter Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            synchronizationContext = SynchronizationContext.Current;
        }
        else if (_instance != this)
        {
            Destroy(this);
        }
    }


    public  void Execute(IData itemQueue)
    {
        ItemServer item = (ItemServer)itemQueue;
        GSInterludeCombatPacketType type = (GSInterludeCombatPacketType)item.ByteType();
        switch (type)
        {
            case GSInterludeCombatPacketType.MoveToPawn:
                MoveToPawn(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.DIE:
                Die(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.STOP_MOVE:
                StopMove(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.ATTACK:
                Attack(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.ActionFailed:
                ActionFailed(itemQueue.DecodeData());
                break;

        }

  
    }


    public void ActionFailed(byte[] data)
    {
        ActionFailed attackPacket = new ActionFailed(data);

        //synchronizationContext.Post(_ =>
        //{
         //   if (PlayerStateMachine.Instance.State == PlayerState.ATTACKING)
          //  {
           //     PlayerStateMachine.Instance.NotifyEvent(Event.CANCEL);
           // }
        //}, null);

    }

   

    private void Attack(byte[] data)
    {
        Attack attackPacket = new Attack(data);
        AttackTest(attackPacket);
        SaveVariable(attackPacket);
    }


    //private async Task AttackTest(int attaker_id , int target_id , UnityEngine.Vector3 attackerPos , UnityEngine.Vector3 targetPos)
    private void AttackTest(Attack attackPacket)
    {
        synchronizationContext.Post(_ =>
        {
            Entity targetEntity = World.Instance.GetEntityNoLockSync(attackPacket.TargetId);
            Entity attakerEntity = World.Instance.GetEntityNoLockSync(attackPacket.AttackerObjId);

            if (attakerEntity != null)
            {
                PlayerAttack(attackPacket, attakerEntity, targetEntity);
            }

            if (targetEntity != null)
            {
                MonsterAttack(attakerEntity, attackPacket);
                //Debug.Log("Monster Attack First ");
            }
        }, null);


    }

    private void PlayerAttack(Attack attackPacket, Entity attakerEntity, Entity targetEntity)
    {
        if (attakerEntity.GetType() == typeof(PlayerEntity))
        {
            //TimeUtils.PrintFullTime("Attack Packet Time Packet 1 ");
            if (attakerEntity.IsDead() == true | targetEntity.IsDead() == true) return;
            //TimeUtils.PrintFullTime("Attack Packet Time Packet 2 ");
            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_ATTACK, attackPacket);
            //TimeUtils.PrintFullTime("Attack Packet Time Packet 3 ");
            OnEventPlaVsMonster(attakerEntity, targetEntity);
        }
    }


    private void MonsterAttack(Entity attakerEntity, Attack attackPacket)
    {
        if (attakerEntity.GetType() == typeof(MonsterEntity))
        {
            if (attakerEntity.IsDead() == true | attakerEntity.IsDead() == true) return;
            MonsterStateMachine monsterStatemachine = attakerEntity.GetComponent<MonsterStateMachine>();
            if (monsterStatemachine != null)
            {
                monsterStatemachine.ChangeIntention(MonsterIntention.INTENTION_ATTACK, attackPacket);
            }
        }
    }


    //Player Attack to Monster Create Cancel 
    private void OnEventPlaVsMonster(Entity attakerEntity, Entity targetEntity)
    {
        if (attakerEntity.GetType() == typeof(PlayerEntity) & targetEntity.GetType() == typeof(MonsterEntity))
        {
            // WorldCombat.Instance.InflictAttack(attakerEntity.transform, targetEntity.transform);
            //���� �� ����� �������� �������� ��� ����� �������� ��-�� ��������� �������. � ���� ����� � ������� ����������� ��������,
            //� ����� ����� ������ �� ������� � ������ ��������� ������
            MonsterStateMachine targetMonster = targetEntity.GetComponent<MonsterStateMachine>();
            if (targetMonster.State == MonsterState.RUNNING | targetMonster.State == MonsterState.WALKING)
            {
               // targetMonster.NotifyEvent(Event.CANCEL);
            }

        }
    }

    private void SaveVariable(Attack attackPacket)
    {
        EventProcessor.Instance.QueueEvent(() => {
            string name = World.Instance.getEntityName(attackPacket.TargetId);
            //example "$c1 hit you for $s2 damage."
            //system message info c1
            //StorageVariable.getInstance().Add�1Items(new VariableItem(name, attackPacket.AttackerObjId));
            //system message info s1

        });
        //StorageVariable.getInstance().AddS1Items(new VariableItem(attackPacket.Damage.ToString(), attackPacket.AttackerObjId));
        //system message info s2
        //StorageVariable.getInstance().AddS2Items(new VariableItem(attackPacket.Damage.ToString(), attackPacket.AttackerObjId));

    }

    private void MoveToPawn(byte[] data)
    {
        MoveToPawn moveToPawnPacket = new MoveToPawn(data);

        synchronizationContext.Post(_ => {
            Entity entity = World.Instance.GetEntityNoLockSync(moveToPawnPacket.ObjId);
            if (entity != null)
            {
                if (entity.GetType() == typeof(PlayerEntity))
                {
                   
                    PlayerGoMove(moveToPawnPacket);
                }
                else if (entity.GetType() == typeof(MonsterEntity))
                {
                    MonsterEntity mEntity = (MonsterEntity)entity;
                    MonsterMoveToPawn(moveToPawnPacket , mEntity);
                }
            }
        }, null);
    }
    public void PlayerGoMove(MoveToPawn moveToPawnPacket)
    {
        PlayerController.Instance.InitMoveToPawn(moveToPawnPacket);
    }

    public void MonsterMoveToPawn(MoveToPawn moveToPawnPacket , MonsterEntity mEntity)
    {
          MonsterStateMachine msm = mEntity.GetComponent<MonsterStateMachine>();
          msm.ChangeIntention(MonsterIntention.INTENTION_FOLLOW , moveToPawnPacket);
    }

    public async void StopMove(byte[] data)
    {
        StopMove stopMovePacket = new StopMove(data);

        synchronizationContext.Post(_ =>
        {
            StopMoveUpdate(stopMovePacket);
        }, null);
    }

    private void StopMoveUpdate(StopMove stopMovePacket)
    {
        Entity entity = World.Instance.GetEntityNoLockSync(stopMovePacket.ObjId);

        if(entity == null) return;

        if (entity.GetType() == typeof(PlayerEntity))
        {
            if (PlayerStateMachine.Instance.State == PlayerState.DEAD) return;

            if (!PlayerEntity.Instance.GetDead())
            {
                //Debug.Log("STOP MOVE STATE " + PlayerStateMachine.Instance.State);

                PlayerEntity entity1 = (PlayerEntity)entity;
                //StopAttackElseTargetDie(entity1);
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_STOP_MOVE, stopMovePacket);
                //StopMove(entity, stopMovePacket);
            }


        }
        else
        {
            //Entity entity = await GetEntity(stopMovePacket);

            if (entity.GetType() == typeof(MonsterEntity))
            {
                if (!entity.IsDead())
                {
                    MonsterEntity entity1 = (MonsterEntity)entity;
                    MonsterStateMachine monsterStatemachine = entity.GetComponent<MonsterStateMachine>();
                    monsterStatemachine.ChangeIntention(MonsterIntention.INTENTION_STOP_MOVE, stopMovePacket);

                }

            }
        }
    }



    private void Die(byte[] data)
    {

        Die diePacket = new Die(data);

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(diePacket);
        }
        else
        {
            synchronizationContext.Post(_ =>
            {
                WhoDied(diePacket);
            }, null);
        }

    }

    private void WhoDied(Die diePacket)
    {
        Entity entity = World.Instance.GetEntityNoLockSync(diePacket.ObjectId);

        if (entity != null)
        {
            if (entity.GetType() == typeof(PlayerEntity))
            {
                entity.SetDead(true);
                PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_DEAD , diePacket);
                
            }
            else if (entity.GetType() == typeof(MonsterEntity))
            {
                entity.SetDead(true);
                var monsterEnity = (MonsterEntity)entity;
                MonsterDead(monsterEnity);
                PlayerStateMachine.Instance.OnWaitReturn();
            }
        }

        //PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_DEAD);
        //World.Instance.Die(diePacket.ObjectId);
    }

    private void MonsterDead(MonsterEntity deadEnity)
    {
        MonsterStateMachine monsterStatemachine = deadEnity.GetComponent<MonsterStateMachine>();
        if (monsterStatemachine != null)
        {
            monsterStatemachine.ChangeState(MonsterState.DEAD);
            monsterStatemachine.NotifyEvent(Event.DEAD);
        }
    }










}
