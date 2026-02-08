using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using static StorageVariable;


public class GsInterludeCombatHandler : ServerPacketHandler
{
    private PlayerPositionSender _pp_sender;
    private SynchronizationContext _synchronizationContext;
    public GsInterludeCombatHandler()
    {
        _pp_sender = new PlayerPositionSender();
        _synchronizationContext = SynchronizationContext.Current;
    }
    public override void HandlePacket(IData itemQueue)
    {
        ItemServer item = (ItemServer)itemQueue;
        GSInterludeCombatPacketType type = (GSInterludeCombatPacketType)item.ByteType();

        switch (type)
        {
            case GSInterludeCombatPacketType.MyTargetSelected:
                OnMyTargetSelected(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.AUTO_ATTACK_START:
                AutoAttackStart(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.AUTO_ATTACK_STOP:
                AutoAttackStop(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.MagicSkillUse:
                MagicSkillUse(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.SetupGauge:
                SetupGauge(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.MagicSkillLaunched:
                MagicSkillLaunched(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.InventoryUpdate:
                OnInventoryUpdate(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.EnchantResult:
                OnEnchantResult(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.ItemList:
                OnCharItemList(itemQueue.DecodeData());
                break;
            case GSInterludeCombatPacketType.ChooseInventoryItem:
                OnChooseInventoryItem(itemQueue.DecodeData());
                break;
            default:
                var s = 1;
                break;

        }

    }

    public void OnCharItemList(byte[] data)
    {
        ItemList itemList = new ItemList(data);

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            var _items = itemList.Items;
            var ShowWindow = itemList.ShowWindow;
            StorageItems.getInstance().AddItems(_items);
            StorageItems.getInstance().AddShow(ShowWindow);
            StorageItems.getInstance().AddEquipItems(itemList.EquipItems);
        }
        else
        {
            var _items = itemList.Items;
            var showWindow = itemList.ShowWindow;
            PlayerInventory.Instance.SetInventory(_items, itemList.EquipItems, showWindow, itemList.AdenaCount , itemList.Items.Count + itemList.EquipItems.Count);
        }

    }

    private void OnInventoryUpdate(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            InventoryUpdate packet = new InventoryUpdate(data);
            PlayerInventory.Instance.UpdateInventory(packet.Items, packet.EquipItems);
        }
    }

    private void OnChooseInventoryItem(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            ChooseInventoryItem packet = new ChooseInventoryItem(data);

            EventProcessor.Instance.QueueEvent(() => {
                EnchantWindow.Instance.ShowWindow(packet.Item);
            });
        }

    }

    private void OnEnchantResult(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EnchantResult packet = new EnchantResult(data);
            
            UnityMainThreadDispatcher.Instance().Enqueue(() => {
                EnchantWindow.Instance.EnchantResult(packet.Result);
            });
        }

    }




    public void OnMyTargetSelected(byte[] data)
    {
        MyTargetSelect tagetPacket = new MyTargetSelect(data);

        EventProcessor.Instance.QueueEvent(() =>{
            TargetManager.Instance.NextTargetById(tagetPacket.ObjectId, tagetPacket.Color);
            _pp_sender.SendServerArrivedPosition(PlayerController.Instance.transform.position);
        });
    }



    private void MagicSkillUse(byte[] data)
    {
        var magicSkill = new MagicSkillUse(data);
        if (magicSkill == null) return;

        EventProcessor.Instance.QueueEvent(() =>
        {
            SkillbarWindow.Instance.ShowCooldown(magicSkill.SkillId, Shortcut.TYPE_SKILL , magicSkill.Reusedelay);

            PlayerStateMachine.Instance.ChangeIntention(
                magicSkill.SkillGrp.IsMagic == 1 ?
                    Intention.INTENTION_MAGIC_ATTACK :
                    Intention.INTENTION_PHYSICAL_SKILLS_ATTACK,
                magicSkill);
        });

    }

    private void SetupGauge(byte[] data)
    {
        SetupGauge magicSkill = new SetupGauge(data);
        Debug.Log("");

    }

  
    private void MagicSkillLaunched(byte[] data)
    {
        MagicSkillLaunched magicSkillLaunched = new  MagicSkillLaunched(data);
        //PlayerStateMachine.Instance.ChangeState(PlayerState.MAGIC_CAST_SHOT);

    }



    public void AutoAttackStart(byte[] data)
    {
        AutoAttackStartPacket packet = new AutoAttackStartPacket(data);

        EventProcessor.Instance.QueueEvent(async () => {
            Entity entity = await World.Instance.GetEntityNoLock(packet.EntityId);
            if (entity != null)
            {
                if (entity.GetType() == typeof(PlayerEntity))
                {
                    PlayerEntity ent1 = (PlayerEntity)entity;
                    if (!ent1.IsDead()) PlayerStateMachine.Instance.NotifyEvent(Event.WAIT_RETURN);
                    ent1.isAutoAttack = true;
                   // Debug.Log("AutoAttackStart Event  state " + PlayerStateMachine.Instance.State);
                }
            }
        });
    }

    public void AutoAttackStop(byte[] data)
    {

        AutoAttackStopPacket packet = new AutoAttackStopPacket(data);
        StopAutoAttack(packet.EntityId);
    }

    public async Task StopAutoAttack(int target_id)
    {
        EventProcessor.Instance.QueueEvent(async () => {
            Entity entity = await World.Instance.GetEntityNoLock(target_id);
            if(entity != null)
            {
                if (entity.GetType() == typeof(PlayerEntity))
                {
                    PlayerEntity ent1 = (PlayerEntity)entity;
                    ent1.isAutoAttack = false;
                    if (!PlayerController.Instance.RunningToDestination & !ent1.IsDead())
                    {
                        PlayerStateMachine.Instance.ChangeState(PlayerState.IDLE);
                        PlayerStateMachine.Instance.NotifyEvent(Event.ARRIVED);
                        
                    }
                    //bug.Log("StopAutoAttack Event  state " + PlayerStateMachine.Instance.State);
                }
                

            }
        });
    }

    
  

    protected override byte[] DecryptPacket(byte[] data)
    {
        throw new System.NotImplementedException();
    }
}
