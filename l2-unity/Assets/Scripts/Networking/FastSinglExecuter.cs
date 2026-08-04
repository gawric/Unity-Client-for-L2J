using System;
using System.Threading;
using UnityEngine;

public class FastSinglExecuter : MonoBehaviour
{
    private const string PKT_ORD_LOG = "[PKT_ORD]";
    private static int _pktSeq;

    private SynchronizationContext synchronizationContext;

    private static FastSinglExecuter _instance;
    public static FastSinglExecuter Instance { get { return _instance; } }

    private static int NextPktSeq() => Interlocked.Increment(ref _pktSeq);

    private static string PktStamp()
    {
        // Queue/receive thread must not touch UnityEngine.Time.
        return $"utcMs={DateTime.UtcNow:HH:mm:ss.fff}";
    }

    private static string PktStampMain()
    {
        return $"frame={Time.frameCount} t={Time.time:F3} rt={Time.realtimeSinceStartup:F3} utcMs={DateTime.UtcNow:HH:mm:ss.fff}";
    }

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
        int seq = NextPktSeq();
        Attack attackPacket = new Attack(data);
        Debug.Log(
            $"{PKT_ORD_LOG} #{seq} ATTACK QUEUE {PktStamp()} " +
            $"attackerId={attackPacket.AttackerObjId} targetId={attackPacket.TargetId} " +
            $"dmg={attackPacket.Damage} (parsed on queue thread → Post main)");
        AttackTest(attackPacket, seq);
    }


 
    private void AttackTest(Attack attackPacket, int seq)
    {
        synchronizationContext.Post(_ =>
        {
            Debug.Log(
                $"{PKT_ORD_LOG} #{seq} ATTACK MAIN_BEGIN {PktStampMain()} " +
                $"attackerId={attackPacket.AttackerObjId} targetId={attackPacket.TargetId} dmg={attackPacket.Damage}");

            Entity targetEntity = World.Instance.GetEntityNoLockSync(attackPacket.TargetId);
            Entity attakerEntity = World.Instance.GetEntityNoLockSync(attackPacket.AttackerObjId);

            bool attackerDead = attakerEntity != null && attakerEntity.IsDead();
            bool targetDead = targetEntity != null && targetEntity.IsDead();
            string playerState = PlayerStateMachine.Instance != null
                ? PlayerStateMachine.Instance.State.ToString()
                : "null";
            string playerIntention = PlayerStateMachine.Instance != null
                ? PlayerStateMachine.Instance.Intention.ToString()
                : "null";

            Debug.Log(
                $"{PKT_ORD_LOG} #{seq} ATTACK MAIN_CTX {PktStampMain()} " +
                $"attackerNull={attakerEntity == null} targetNull={targetEntity == null} " +
                $"attackerDead={attackerDead} targetDead={targetDead} " +
                $"playerState={playerState} intention={playerIntention}");

            if (attakerEntity != null) PlayerAttack(attackPacket, attakerEntity, targetEntity, seq);
            if (targetEntity != null) MonsterAttack(attakerEntity, attackPacket);

            Debug.Log($"{PKT_ORD_LOG} #{seq} ATTACK MAIN_END {PktStampMain()}");

        }, null);

    }

    private void PlayerAttack(Attack attackPacket, Entity attakerEntity, Entity targetEntity, int seq)
    {
        if (attakerEntity.GetType() == typeof(PlayerEntity))
        {
            if (attakerEntity == null | targetEntity == null)
            {
                Debug.LogWarning($"{PKT_ORD_LOG} #{seq} ATTACK SKIP null entity");
                return;
            }

            if (attakerEntity.IsDead() == true | targetEntity.IsDead() == true)
            {
                Debug.LogWarning(
                    $"{PKT_ORD_LOG} #{seq} ATTACK SKIP alreadyDead " +
                    $"attackerDead={attakerEntity.IsDead()} targetDead={targetEntity.IsDead()} " +
                    $"{PktStampMain()} " +
                    $"(Die likely processed before this Attack on main thread)");
                return;
            }
       
            Debug.Log(
                $"{PKT_ORD_LOG} #{seq} ATTACK APPLY→INTENTION_ATTACK {PktStampMain()} " +
                $"targetId={attackPacket.TargetId} dmg={attackPacket.Damage}");
            PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_ATTACK, attackPacket);
           
            OnEventPlaVsMonster(attakerEntity, targetEntity);
        }
    }


    private void MonsterAttack(Entity attakerEntity, Attack attackPacket)
    {
        if(attakerEntity == null)
        {
            Debug.LogWarning("FastSinglExecuter>MonsterAttack: attakerEntity its null");
            return;
        }


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
            //пїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ-пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ. пїЅ пїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ,
            //пїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ
            MonsterStateMachine targetMonster = targetEntity.GetComponent<MonsterStateMachine>();
            if (targetMonster.State == MonsterState.RUNNING | targetMonster.State == MonsterState.WALKING)
            {
               // targetMonster.NotifyEvent(Event.CANCEL);
            }

        }
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
        if(stopMovePacket == null)
        {
            Debug.LogError("FastSinglExecuter->StopMoveUpdate: РїСЂРёС€РµР» РїР°РєРµС‚ null");
            return;
        }

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
        int seq = NextPktSeq();
        Die diePacket = new Die(data);

        Debug.Log(
            $"{PKT_ORD_LOG} #{seq} DIE QUEUE {PktStamp()} objectId={diePacket.ObjectId} " +
            $"(parsed on queue thread → Post main)");

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            Debug.Log($"{PKT_ORD_LOG} #{seq} DIE deferred→AddPacketsInit (world init)");
            InitPacketsLoadWord.getInstance().AddPacketsInit(diePacket);
        }
        else
        {
            synchronizationContext.Post(_ =>
            {
                Debug.Log(
                    $"{PKT_ORD_LOG} #{seq} DIE MAIN_BEGIN {PktStampMain()} " +
                    $"objectId={diePacket.ObjectId}");
                WhoDied(diePacket, seq);
                Debug.Log($"{PKT_ORD_LOG} #{seq} DIE MAIN_END {PktStampMain()}");
            }, null);
        }

    }

    private void WhoDied(Die diePacket, int seq)
    {
        Entity entity = World.Instance.GetEntityNoLockSync(diePacket.ObjectId);

        string playerState = PlayerStateMachine.Instance != null
            ? PlayerStateMachine.Instance.State.ToString()
            : "null";
        string playerIntention = PlayerStateMachine.Instance != null
            ? PlayerStateMachine.Instance.Intention.ToString()
            : "null";
        bool isAttack = PlayerEntity.Instance != null && PlayerEntity.Instance.IsAttack;

        Debug.Log(
            $"{PKT_ORD_LOG} #{seq} DIE WhoDied {PktStampMain()} " +
            $"objectId={diePacket.ObjectId} entityNull={entity == null} " +
            $"entityType={(entity != null ? entity.GetType().Name : "null")} " +
            $"playerState={playerState} intention={playerIntention} IsAttack={isAttack}");

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

                // Clear attack latch so JAtk SwitchToIdle can leave the swing even if
                // a follow-up Attack already set IsAttack=true before Die was processed.
                if (PlayerEntity.Instance != null &&
                    PlayerEntity.Instance.TargetId == diePacket.ObjectId)
                {
                    PlayerEntity.Instance.IsAttack = false;
                }

                Debug.Log(
            $"{PKT_ORD_LOG} #{seq} DIE → OnWaitReturn (monster dead) {PktStampMain()} " +
            $"playerState={playerState}");
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
