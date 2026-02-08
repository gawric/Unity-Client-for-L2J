using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


public class GameServerInterludePacketHandler : ServerPacketHandler
{
    private bool isKeyAuthСompleted = false;
    public override void HandlePacket(IData itemQueue)
    {
        ItemServer item = (ItemServer)itemQueue;

        if (!IsExPacket(item))
        {
            //Debug.Log($"PacketName: {item.ToString()}");
            UsePacket(item, itemQueue);
        }
        else
        {
            int type = item.ExPacketType();
            UseExPacket(type, itemQueue);
        }
    }


    private void UsePacket(ItemServer item, IData itemQueue)
    {
        switch (item.PaketType())
        {
            case GameInterludeServerPacketType.InterludeKeyPacket:

                OnKeyReceive(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.CharSelectionInfo:

                OnCharSelectionInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.CharTemplate:

                OnCharTemplate(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.CharCreateOk:

                OnCharCreateOk(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.CharSelected:

                OnCharSelected((itemQueue.DecodeData()));
                break;
            case GameInterludeServerPacketType.CharCreateFail:

                OnCharCreateFail((itemQueue.DecodeData()));
                break;
            case GameInterludeServerPacketType.SkillList:

                OnCharSkillList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.UserInfo:

                OnCharUserInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.SkillCoolTime:

                OnCharSkillCoolTime(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.MacroList:

                OnCharMacroList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.NpcHtmlMessage:

                OnNpcHtmlMessage(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PackageToList:

                OnPackageToList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TutorialShowHtml:

                OnTutorialShowHtml(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.BuyList:

                OnBuyList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ShopPreviewList:
                OnShopPreviewList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ShopPreviewInfo:
                OnShopPreviewInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.MultiSellList:

                OnMultisellList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.SellList:

                OnSellList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.WhDepositList:

                OnWhDepositList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PackageSendableList:

                OnPackageSendableList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.WhWithdrawList:

                OnWhWithdrawList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.BuyListSeed:

                OnBuyListSeed(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ShortCutInit:

                OnCharShortCutInit(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ShortCutRegister:

                OnCharShortCutRegister(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ShortCutDel:

                OnCharShortCutDel(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.SocialAction:

                OnSocialAction(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TeleportToLocation:

                OnTeleportToLocation(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.Revive:

                OnRevive(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.HennaInfo:

                OnCharHennaInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.QuestList:

                OnCharQuestList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeShowMemberListUpdate:

                OnPledgeShowMemberListUpdate(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeShowMemberListDelete:

                OnPledgeShowMemberListDelete(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeShowMemberListAll:

                OnPledgeShowMemberListAll(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeShowMemberListDeleteAll:

                OnPledgeShowMemberListDeleteAll(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeShowMemberListAdd:

                OnPledgeShowMemberListAdd(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeInfo:

                OnPledgeInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ManagePledgePower:

                OnManagePledgePower(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.PledgeStatusChanged:

                OnPledgeStatusChanged(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.NpcInfo:

                OnCharNpcInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.DeleteObject:

                OnDeleteObject(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.CharMoveToLocation:

                OnMoveToLocation(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ValidateLocation:

                OnValidateLocation(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.FriendList:

                OnFriendList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.EtcStatusUpdate:

                OnEtcStatusUpdate(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.StatusUpdate:

                OnStatusUpdate(itemQueue.DecodeData());
                break;

            case GameInterludeServerPacketType.TargetUnselected:

                OnTargetUnselected(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.AbnormalStatusUpdate:

                OnAbnormalStatusUpdate(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.ShortBuffStatusUpdate:

                OnShortBuffStatusUpdate(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.AcquireSkillList:

                OnAcquireSkillList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.AcquireSkillInfo:

                OnAcquireSkillInfo(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.RecipeBookItemList:

                OnRecipeBookItemList(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.RecipeItemMakeInfo:

                OnRecipeItemMakeInfo(itemQueue.DecodeData());
                break;

            case GameInterludeServerPacketType.AskJoinParty:
                OnAskJoinParty(itemQueue.DecodeData());
                break;

            case GameInterludeServerPacketType.SendTradeRequest:
                OnTradeRequest(itemQueue.DecodeData());
                break;

            case GameInterludeServerPacketType.TradeStart:
                OnTradeStart(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TradeDone:
                OnTradeDone(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TradeOtherAdd:
                OnTradeOtherAdd(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TradeOwnAdd:
                OnTradeOwnAdd(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TradePressOtherOk:
                OnTradeOtherOk(itemQueue.DecodeData());
                break;
            case GameInterludeServerPacketType.TradePressOwnOk:
                OnTradeOwnOk(itemQueue.DecodeData());
                break;

            case GameInterludeServerPacketType.GetItem:
                break;
            case GameInterludeServerPacketType.DropItem:
                break;
            case GameInterludeServerPacketType.CharInfo:
                break;

            default:
                var s = 1;
                break;

        }
    }

    private void UseExPacket(int exPacket, IData itemQueue)
    {
        switch (exPacket)
        {
            case (int)GameInterludeExServerPacketType.ExShowQuestInfo:
                OnExShowQuestInfo(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExShowSellCropList:
                OnExShowSellCropList(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExShowSeedInfo:
                OnExShowSeedInfo(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExShowCropInfo:
                OnExShowCropInfo(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExShowManorDefaultInfo:
                OnExShowManorDefaultInfo(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExPledgeReceiveMemberInfo:
                OnExPledgeReceiveMemberInfo(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExOnExPledgePowerGradeList:
                OnExPledgePowerGradeList(itemQueue.DecodeExData());
                break;
            case (int)GameInterludeExServerPacketType.ExPledgeReceivePowerInfo:
                OnExPledgeReceivePowerInfo(itemQueue.DecodeExData());
                break;
            default:
                break;
        }
    }



    //3 repeated packages are coming ignoring
    private void OnKeyReceive(byte[] data)
    {
        InterludeKeyPacket packet = new InterludeKeyPacket(data);
        if (!isKeyAuthСompleted)
        {
            if (!packet.AuthAllowed)
            {

                //Debug.LogWarning("WAAAARRRNING  Gameserver connect not allowed.");
                EventProcessor.Instance.QueueEvent(() => GameClient.Instance.Disconnect());
                EventProcessor.Instance.QueueEvent(() => LoginClient.Instance.Disconnect());


                return;
            }
            if (packet.UseBlowfish)
            {
                byte[] equalsKey = BlowFishStaticKey.GetCreateFullKeyBlowFish(packet.BlowFishKey);
                GameClient.Instance.EnableCrypt(equalsKey);
            }

            //Debug.Log("Data1" + LoginClient.Instance.Account);
            //Debug.Log("Data2" + GameClient.Instance.PlayKey1);
            //Debug.Log("Data3" + GameClient.Instance.PlayKey2);
            //Debug.Log("Data4" + GameClient.Instance.SessionKey1);
            //Debug.Log("Data5" + GameClient.Instance.SessionKey2);

            var sendPaket = CreatorPacketsGameLobby.CreateAuthLogin(LoginClient.Instance.Account, GameClient.Instance.PlayKey1, GameClient.Instance.PlayKey2,
                GameClient.Instance.SessionKey1, GameClient.Instance.SessionKey2);

            bool enable = GameClient.Instance.IsCryptEnabled();

            SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
        }

        isKeyAuthСompleted = true;
        //_eventProcessor.QueueEvent(() => ((GameClientPacketHandler)_clientPacketHandler).SendAuth());
        //_eventProcessor.QueueEvent(() => ((GameClientPacketHandler)_clientPacketHandler).SendPing());
    }

    private void OnCharSelectionInfo(byte[] data)
    {
        CharSelectionInfo packet = new CharSelectionInfo(data);
        MapClassId.Init();
        EventProcessor.Instance.QueueEvent(() => {
            CharSelectWindow.Instance.SetInterludeCharacterList(packet.Characters);
            CharSelectWindow.Instance.SelectInterludeSlot(packet.SelectedSlotId);
            CharacterSelector.Instance.SetCharacterInterludeList(packet.Characters);
            CharacterSelector.Instance.SelectInterludeCharacter(packet.SelectedSlotId);
            LoginClient.Instance.Disconnect();
            GameClient.Instance.OnAuthAllowed();
        });
    }

    public void OnAbnormalStatusUpdate(byte[] data)
    {
        AbnormalStatusUpdate packet = new AbnormalStatusUpdate(data);
        EventProcessor.Instance.QueueEvent(() => {
            foreach (var item in packet.ListEffect)
            {
                BufferPanel.Instance.AddDataCellToTime(item.Id, item.Value, item.Duration);
            }
        });
    }

    public void OnShortBuffStatusUpdate(byte[] data)
    {
        ShortBuffStatusUpdate packet = new ShortBuffStatusUpdate(data);
        EventProcessor.Instance.QueueEvent(() => {
                var item = packet.Effect;
                BufferPanel.Instance.AddDataCellToTime(item.Id, item.Value, item.Duration);
        });
    }

    public void OnAcquireSkillList(byte[] data)
    {
        AcquireSkillList packet = new AcquireSkillList(data);
        EventProcessor.Instance.QueueEvent(() => {
            SkillLearnWindow.Instance.AddData(packet.AcquireList);
            SkillLearnWindow.Instance.ShowWindow();
        });
    }

    public void OnAcquireSkillInfo(byte[] data)
    {
        AcquireSkillInfo packet = new AcquireSkillInfo(data);
        EventProcessor.Instance.QueueEvent(() => {
            SkillLearnWindow.Instance.HideWindow();
            DescriptionSkillWindow.Instance.AddData(packet);
            DescriptionSkillWindow.Instance.ShowWindow();
        });
    }

    private void OnCharTemplate(byte[] data)
    {
        CharTemplates templates = new CharTemplates(data);
        var list = templates.PlayerTemplates;
        EventProcessor.Instance.QueueEvent(() => {
            GameManager.Instance.OnCreateUser(list);
        });

    }

    private void OnCharCreateOk(byte[] data)
    {
        CharCreateOk charOk = new CharCreateOk(data);
        bool create = charOk.IsCreate;
    }

    private void OnCharCreateFail(byte[] data)
    {
        CharCreateFail charOk = new CharCreateFail(data);
        string text = charOk.Text;
        EventProcessor.Instance.QueueEvent(() => {
            GameManager.Instance.OnCreateUserFail(text);
        });
    }

    private void OnCharSkillList(byte[] data)
    {
        SkillList skillListPacket = new SkillList(data);
        PlayerInfoInterlude info1 = GameClient.Instance.PlayerInfo;
        //_eventProcessor.QueueEvent(() => {
        //    SkillListWindow.Instance.SetSkillList(skillListPacket.Skills);
        //});
        //SkillListWindow.Instance.Set

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(skillListPacket);
        }
        else
        {
            _eventProcessor.QueueEvent(() => {
                SkillListWindow.Instance.UpdateSkillList(skillListPacket.Skills);
            });
        }

    }



    private void OnCharSelected(byte[] data)
    {

        CharSelected charOk = new CharSelected(data);
        GameClient.Instance.PlayerInfo = charOk.PlayeInfo;
        GameClient.Instance.SetDataPreparationCompleted(false);
        InitPacketsLoadWord.getInstance().IsInit = true;
        // Task.Run(() =>
        //{
        _eventProcessor.QueueEvent(() => {
            GameClient.Instance.PlayerInfo = charOk.PlayeInfo;
            GameManager.Instance.OnCharacterSelect();
        });
        //});


        var sendPaket = CreatorPacketsGameLobby.CreateEnterWorld();
        bool enable = GameClient.Instance.IsCryptEnabled();
        //Debug.Log("GameServerPacket OnCharUserInfo : �������� ������� ����� EnterWorld!!!! ");
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
    }

    private void OnCharUserInfo(byte[] data)
    {
        UserInfo userInfo = new UserInfo(data, GameClient.Instance.PlayerInfo);
        StorageNpc.getInstance().AddUserInfo(userInfo);
        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            GameClient.Instance.SetDataPreparationCompleted(true);
            Debug.Log("GameServerPacket OnCharUserInfo : ��������� ��������� ������ UserInfo Init Packet");
        }
        else
        {
            EventProcessor.Instance.QueueEvent(() =>
            {
                World.Instance.UserInfoUpdateCharacter(userInfo);
                UserInfoCharacterCombat(userInfo);
            });
            Debug.Log("GameServerPacket OnCharUserInfo : ��������� ��������� ������ UserInfo noraml packet");
        }


    }

    private void UserInfoCharacterCombat(UserInfo userInfo)
    {
        World.Instance.UpdateUserInfo(PlayerEntity.Instance, userInfo);
    }

    private void OnCharSkillCoolTime(byte[] data)
    {
        //Debug.Log("GameServerPacket SkillCoolTime : ���������� �� �� ��������� �.� �� ���������� �������� ");
        SkillCoolTime skillCoolTime = new SkillCoolTime(data);
        //Debug.Log("GameServerPacket OnCharSkillCoolTime : ��������� ");

    }
    private void OnCharMacroList(byte[] data)
    {
        //Debug.Log("GameServerPacket OnCharMacroList : ���������� �� �� ��������� �.� �� ���������� �������� ");
        MacroList macroList = new MacroList(data);
        //Debug.Log("GameServerPacket OnCharMacroList : ��������� ");
    }

    private void OnNpcHtmlMessage(byte[] data)
    {

        NpcHtmlMessage npcHtmlMessage = new NpcHtmlMessage(data);

        EventProcessor.Instance.QueueEvent(() => {
            Entity npc = World.Instance.GetEntityNoLockSync(npcHtmlMessage.GetNpcId());

            //npcId> 0 - the current use
            if (npcHtmlMessage.GetNpcId() == 0) ShowHtmlBrightToFront(npcHtmlMessage);

            if (npc == null) return;

            var nsm = npc.GetComponent<NpcStateMachine>();

            if (nsm != null)
            {

                nsm.ChangeIntention(NpcIntention.STARTED_TALKING, npcHtmlMessage);
            }
            else
            {
                Vector3 position = PlayerEntity.Instance.transform.position;
                ManualRotate(npc.transform, position);
                ShowHtmlPage(npcHtmlMessage);
            }

        });
    }

    private void ShowHtmlPage(NpcHtmlMessage npcHtmlMessage)
    {
        HtmlWindow.Instance.InjectToWindow(npcHtmlMessage.Elements());
        HtmlWindow.Instance.ShowWindowToCenter();
    }

    private void ShowHtmlBrightToFront(NpcHtmlMessage npcHtmlMessage)
    {
        HtmlWindow.Instance.InjectToWindow(npcHtmlMessage.Elements());
        HtmlWindow.Instance.ShowWindowToCenterAndBringToFront();

    }

    private void OnPackageToList(byte[] data)
    {

        PackageToList packageToList = new PackageToList(data);
        List<string> listName = packageToList.GetListName();

        EventProcessor.Instance.QueueEvent(() => {
            ShowListWindow.Instance.AddList(packageToList.Players);
            ShowListWindow.Instance.ShowWindow();
        });
    }

    private void ManualRotate(Transform npc, Vector3 player)
    {
        Vector3 direction = player - npc.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        npc.rotation = lookRotation;
    }

    private void OnTutorialShowHtml(byte[] data)
    {

        TutorialShowHtml htmlMessage = new TutorialShowHtml(data);

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(htmlMessage);

        }
        else
        {
            EventProcessor.Instance.QueueEvent(() => {
                HtmlWindow.Instance.InjectToWindow(htmlMessage.Elements());
                HtmlWindow.Instance.ShowWindowToCenter();
            });
        }

    }

    private void OnBuyList(byte[] data)
    {
        BuyList buyList = new BuyList(data);

        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetWindowName("Shop");
            DealerWindow.Instance.SetHeaderNameSellPanel("Sell");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Buy");
            DealerWindow.Instance.SetProductType(ProductType.BUY);
            DealerWindow.Instance.UpdateBuyData(buyList.Products, false, buyList.ListID);
            DealerWindow.Instance.UpdateDataForm(buyList.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindowToCenter();
        });

    }

    public void OnShopPreviewList(byte[] data)
    {
        ShopPreviewList shopPreviewList = new ShopPreviewList(data);
        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetHeaderNameSellPanel("Attempt");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Selection list");
            DealerWindow.Instance.SetProductType(ProductType.WEAR);
            DealerWindow.Instance.UpdateBuyData(shopPreviewList.Products, false, shopPreviewList.ListID);
            DealerWindow.Instance.UpdateDataForm(shopPreviewList.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindowToCenter();
        });
    }

    public void OnShopPreviewInfo(byte[] data)
    {
        ShopPreviewInfo shopPreviewList = new ShopPreviewInfo(data);
        Debug.Log("There is no implementation of this package.> OnShopPreviewInfo");
    }

    private void OnMultisellList(byte[] data)
    {
        MultiSellList multiSellList = new MultiSellList(data);

        EventProcessor.Instance.QueueEvent(() => {
            MultiSellWindow.Instance.AddData(multiSellList.GetOnlyItems(), multiSellList);
            MultiSellWindow.Instance.ShowWindow();
        });
    }


    private void OnSellList(byte[] data)
    {
        SellList sellList = new SellList(data);

        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetWindowName("Shop");
            DealerWindow.Instance.SetHeaderNameSellPanel("Inventory");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Sell");
            DealerWindow.Instance.SetProductType(ProductType.SELL);
            DealerWindow.Instance.UpdateBuyData(sellList.Products, true, sellList.ListID);
            DealerWindow.Instance.UpdateDataForm(sellList.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindow();
        });

    }

    private void OnWhDepositList(byte[] data)
    {
        WarehouseDepositList whList = new WarehouseDepositList(data);
        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetWindowName("Personal storage");
            DealerWindow.Instance.SetHeaderNameSellPanel("Inventory");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Items in stock");
            DealerWindow.Instance.SetProductType(ProductType.WHDepositList);
            DealerWindow.Instance.UpdateBuyData(whList.Products, true, -1);
            DealerWindow.Instance.UpdateDataForm(whList.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindow();
        });
    }

    public void OnPackageSendableList(byte[] data)
    {
        PackageSendableList list = new PackageSendableList(data);

        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetWindowName("Send a parcel");
            DealerWindow.Instance.SetHeaderNameSellPanel("Inventory");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Shipping list");
            DealerWindow.Instance.SetProductType(ProductType.PackageSendableList);
            DealerWindow.Instance.UpdateBuyData(list.Items, true, list.PlayerObject);
            DealerWindow.Instance.UpdateDataForm(list.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindow();
        });

    }

    public void OnWhWithdrawList(byte[] data)
    {
        WarehouseWithdrawList whList = new WarehouseWithdrawList(data);
        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetWindowName("Personal storage");
            DealerWindow.Instance.SetHeaderNameSellPanel("Items in Warehouse");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Items taken away");
            DealerWindow.Instance.SetProductType(ProductType.WHWithdrawList);
            DealerWindow.Instance.UpdateBuyData(whList.Products, true, -1);
            DealerWindow.Instance.UpdateDataForm(whList.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindow();
        });
    }

    public void OnBuyListSeed(byte[] data)
    {
        BuyListSeed listSeed = new BuyListSeed(data);
        EventProcessor.Instance.QueueEvent(() => {
            UserInfo info = StorageNpc.getInstance().GetFirstUser();
            DealerWindow.Instance.SetWindowName("Estate");
            DealerWindow.Instance.SetHeaderNameSellPanel("Sale");
            DealerWindow.Instance.SetHeaderNameBuyPanel("Purchase");
            DealerWindow.Instance.SetProductType(ProductType.BUY_SEED);
            DealerWindow.Instance.UpdateBuyData(listSeed.List, true, listSeed.ManorId);
            DealerWindow.Instance.UpdateDataForm(listSeed.CurrentMoney, info.PlayerInfoInterlude.Stats.WeightPercent(), info.PlayerInfoInterlude.Stats.CurrWeight, info.PlayerInfoInterlude.Stats.MaxWeight);
            DealerWindow.Instance.ShowWindow();
        });
    }



    private void OnCharShortCutInit(byte[] data)
    {
        Debug.Log("GameServerPacket OnCharShortCutInit : ������� �����");
        ShortCutInit shortCutList = new ShortCutInit(data);
        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            StorageItems.getInstance().AddShortCuts(shortCutList.ShortCuts);
            Debug.Log("GameServerPacket OnCharShortCutInit : ��������� ���� init �� �������!");
        }
        else
        {
            //EventProcessor.Instance.QueueEvent(() => SkillbarWindow.Instance.UpdateAllShortcuts(shortCutList.ShortCuts));
            EventProcessor.Instance.QueueEvent(() => PlayerShortcuts.Instance.SetShortcutList(shortCutList.ShortCuts));
            Debug.Log("GameServerPacket OnCharShortCutInit : �������� �� ����=========");
        }


        //Debug.Log("GameServerPacket OnCharShortCutInit : ��������� ");
    }

    private void OnCharShortCutRegister(byte[] data)
    {
        ShortCutRegister shortCutPacket = new ShortCutRegister(data);
        EventProcessor.Instance.QueueEvent(() => PlayerShortcuts.Instance.RegisterShortcut(shortCutPacket.Shortcut));
    }

    private void OnCharShortCutDel(byte[] data)
    {
        ShortCutDel shortCutDel = new ShortCutDel(data);
        EventProcessor.Instance.QueueEvent(() => PlayerShortcuts.Instance.RemoveShotcutLocally(shortCutDel.Slot));
    }

    private void OnSocialAction(byte[] data)
    {
        SocialAction socialAction = new SocialAction(data);
        //Debug.Log("������ ����� �� ������������ Social Action ObjectId +++" + socialAction.ObjectId + " ID +++" + socialAction.ActionId);
    }

    private void OnTeleportToLocation(byte[] data)
    {
        TeleportToLocation teleportLocation = new TeleportToLocation(data);
        _eventProcessor.QueueEvent(() => {
            World.Instance.TeleportTo(teleportLocation.TarObjId, teleportLocation.TeleportPos);
        });

    }

    public void OnRevive(byte[] data)
    {
        //Debug.Log("DoRevive 1");
        Revive revive = new Revive(data);
        EventProcessor.Instance.QueueEvent(() => World.Instance.Revive(revive.ObjectId));
        PlayerEntity.Instance.SetDead(false);
        // PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_IDLE, data);
        //Debug.Log("DoRevive 2");
    }


    private void OnCharHennaInfo(byte[] data)
    {
        //Debug.Log("GameServerPacket HennaInfo : ���������� �� �� ��������� �.� �� ���������� �������� ");
        HennaInfo hennaInfo = new HennaInfo(data);
        //Debug.Log("GameServerPacket HennaInfo : ��������� ");
    }

    private void OnCharQuestList(byte[] data)
    {

        QuestList questPacket = new QuestList(data);

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(questPacket);
        }
        else
        {
            _eventProcessor.QueueEvent(() => {
                QuestWindow.Instance.AddData(questPacket.Quest);
            });
        }

    }

    private void OnPledgeInfo(byte[] data)
    {
        PledgeInfo pledgeInfo = new PledgeInfo(data);

        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.UpdatePledge(pledgeInfo));
        }
    }

    private void OnManagePledgePower(byte[] data)
    {
        ManagePledgePower managerPledge = new ManagePledgePower(data);

        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => {
                ClanWindow.Instance.UpdateDetailedInfo(managerPledge);
            });
            //EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.UpdatePledge(pledgeInfo));
        }
    }

    private void OnPledgeShowMemberListUpdate(byte[] data)
    {
        PledgeShowMemberListUpdate memberUpdate = new PledgeShowMemberListUpdate(data);
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.UpdateMemberData(memberUpdate));
        }
    }

    private void OnPledgeShowMemberListDelete(byte[] data)
    {
        PledgeShowMemberListDelete memberDelete = new PledgeShowMemberListDelete(data);

        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.DeleteMemberData(memberDelete));
        }
    }

    private void OnPledgeShowMemberListAll(byte[] data)
    {
        PledgeShowMemberListAll allMembers = new PledgeShowMemberListAll(data);
        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(allMembers);
        }
        else
        {
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.AddClanData(allMembers));
        }

    }

    private void OnPledgeShowMemberListDeleteAll(byte[] data)
    {
        PledgeShowMemberListAll allMembers = new PledgeShowMemberListAll(data);

        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            Debug.Log("Delete All Data");
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.DeleteMemberData());
        }


    }
    //The packet order is broken due to asynchrony. PledgeShowMemberListAll arrives first, followed by OnPledgeShowMemberListDeleteAll. It should be the other way around.
    private void OnPledgeShowMemberListAdd(byte[] data)
    {
        PledgeShowMemberListAdd packetAdd = new PledgeShowMemberListAdd(data);

        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.AddMemberData(packetAdd));
        }


    }
    private void OnPledgeStatusChanged(byte[] data)
    {
        PledgeStatusChanged pledgeStatusChanged = new PledgeStatusChanged(data);
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => ClanWindow.Instance.UpdateClanIdInfo(pledgeStatusChanged));
        }
    }

    private void OnCharNpcInfo(byte[] data)
    {
        //Debug.Log("GameServerPacket NpcInfo  : ������ ��������� ������ ");
        NpcInfo npcInfo = new NpcInfo(data);
        StorageNpc.getInstance().AddNpcInfo(npcInfo);

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(npcInfo);
        }
        else
        {
            _eventProcessor.QueueEvent(() => {
                UpdateNpc(npcInfo);
            });
        }
    }

    private void UpdateNpc(NpcInfo npcInfo)
    {
        Entity entity = World.Instance.GetEntityNoLockSync(npcInfo.Identity.Id);

        if (entity == null)
        {
            World.Instance.SpawnNpcInterlude(npcInfo.Identity, npcInfo.Status, npcInfo.Stats);
        }
        else
        {
            World.Instance.UpdateNpcInfo(entity, npcInfo);
        }
    }


    public void OnDeleteObject(byte[] data)
    {
        DeleteObject deletepacket = new DeleteObject(data);
        _eventProcessor.QueueEvent(() => { World.Instance.DeleteObject(deletepacket.ObjectId); });
    }

    private void OnMoveToLocation(byte[] data)
    {
        //Debug.Log("GameServerPacket OnMoveToLocation  : ������ �����");
        CharMoveToLocation charMoveToLocation = new CharMoveToLocation(data);
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            EventProcessor.Instance.QueueEvent(() => MoveTo(charMoveToLocation.ObjId, charMoveToLocation.NewPosition, charMoveToLocation.OldPosition, charMoveToLocation));
        }
        else
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(charMoveToLocation);
        }
    }

    public async Task MoveTo(int objId, Vector3 charMovePosition, Vector3 currentPosition, CharMoveToLocation moveToLocation)
    {
        Entity entity = await World.Instance.GetEntityNoLock(objId);
        float distance = GetDistance(entity, charMovePosition);

        if (entity.name.Equals("Elpy")) return;

        if (entity.GetType() == typeof(PlayerEntity))
        {
            PlayerMove(moveToLocation);
        }
        else if (entity.GetType() == typeof(NpcEntity))
        {
            var npc = (NpcEntity)entity;
            NpcMove(npc, moveToLocation);
        }
        else if (entity.GetType() == typeof(MonsterEntity))
        {
            var monster = (MonsterEntity)entity;
            if (!monster.IsDead())
            {
                DebugLineDraw.ShowDrawLineDebug(objId, charMovePosition, currentPosition, Color.red);
                MonsterMove(monster, charMovePosition);
            }

        }
        //}

    }




    private float GetDistance(Entity entity, Vector3 charMovePosition)
    {
        //monsterStatemachine.MoveMonster.SetFollow(false);
        //var gravityOffTransform = new Vector3(entity.transform.position.x, 0, entity.transform.position.z);
        //var gravityOffTarget = new Vector3(charMovePosition.x, 0, charMovePosition.z);

        return VectorUtils.Distance2D(entity.transform.position, charMovePosition);
    }


    private async Task PlayerMove(CharMoveToLocation charMovePosition)
    {
        //PlayerController.Instance.StopMove();
        PlayerStateMachine.Instance.ChangeIntention(Intention.INTENTION_MOVE_TO, charMovePosition);
    }
    private async Task MonsterMove(MonsterEntity monster, Vector3 monsterMovePosition)
    {
        var msm = monster.GetComponent<MonsterStateMachine>();
        msm.ChangeIntention(MonsterIntention.INTENTION_MOVE_TO, monsterMovePosition);
    }

    private async Task NpcMove(NpcEntity npc, CharMoveToLocation moveToLocation)
    {
        var nsm = npc.GetComponent<NpcStateMachine>();
        if (nsm != null)
        {
            nsm.ChangeIntention(NpcIntention.INTENTION_MOVE_TO, moveToLocation);
        }

    }

    private void OnValidateLocation(byte[] data)
    {

        ValidateLocation validateLocation = new ValidateLocation(data);

        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            PositionValidationController.Instance.AddValidateLocation(validateLocation);
        }
    }

    private void OnFriendList(byte[] data)
    {

        Debug.Log("Friend List SUccess");
    }

    private void OnEtcStatusUpdate(byte[] data)
    {
        EtcStatusUpdate etcStatusUpdate = new EtcStatusUpdate(data);

        if (InitPacketsLoadWord.getInstance().IsInit)
        {
            InitPacketsLoadWord.getInstance().AddPacketsInit(etcStatusUpdate);
        }
        else
        {
            _eventProcessor.QueueEvent(() => {
                BufferPanel.Instance.RefreshPenalty(etcStatusUpdate);
            });
        }

    }

    private void OnExShowQuestInfo(byte[] data)
    {
        ExShowQuestInfo etcStatusUpdate = new ExShowQuestInfo(data);
        if (EventProcessor.Instance != null)
        {
            if (World.Instance != null)
            {
                EventProcessor.Instance.QueueEvent(() => QuestListWindow.Instance.ShowWindow());
            }

        }
        Debug.Log("Event Open ExShowQuestInfo Info");
    }

    public void OnExShowSellCropList(byte[] data)
    {
        ExShowSellCropList showSellCropList = new ExShowSellCropList(data);

        EventProcessor.Instance.QueueEvent(() => {
            SellCropListWindow.Instance.ShowWindow();
            SellCropListWindow.Instance.SetDataTable(showSellCropList);

        });
    }

    public void OnExShowSeedInfo(byte[] data)
    {
        ExShowSeedInfo exShowSeedInfo = new ExShowSeedInfo(data);
        EventProcessor.Instance.QueueEvent(() => {

            SeedInfoWindow.Instance.ShowWindowActiveTabSeed();
            SeedInfoWindow.Instance.SetDataSeedInfo(exShowSeedInfo.List);
        });
    }

    public void OnExShowCropInfo(byte[] data)
    {
        ExShowCropInfo showSellCropList = new ExShowCropInfo(data);
        EventProcessor.Instance.QueueEvent(() => {
            SeedInfoWindow.Instance.SetDataCropInfo(showSellCropList.List);
            SeedInfoWindow.Instance.ShowWindow();
        });


    }

    public void OnExShowManorDefaultInfo(byte[] data)
    {
        ExShowManorDefaultInfo showManorDefaultInfo = new ExShowManorDefaultInfo(data);
        EventProcessor.Instance.QueueEvent(() => {
            SeedInfoWindow.Instance.SetDataDefaultManorInfo(showManorDefaultInfo.List);
            SeedInfoWindow.Instance.ShowWindowActiveTabAllDefault();
        });
    }

    public void OnExPledgeReceiveMemberInfo(byte[] data)
    {
        PledgeReceiveMemberInfo showManorDefaultInfo = new PledgeReceiveMemberInfo(data);
        EventProcessor.Instance.QueueEvent(() => {
            ClanWindow.Instance.UpdateDetailedInfo(showManorDefaultInfo);
        });
    }

    public void OnExPledgePowerGradeList(byte[] data)
    {
        PledgePowerGradeList pledgePowerGradeList = new PledgePowerGradeList(data);
        EventProcessor.Instance.QueueEvent(() => {
            ClanWindow.Instance.ShowGradeInfo(pledgePowerGradeList);
        });
    }

    private void OnRecipeBookItemList(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            RecipeBookItemList packet = new RecipeBookItemList(data);

            EventProcessor.Instance.QueueEvent(() => {
                RecipeBookWindow.Instance.AddData(packet);
                RecipeBookWindow.Instance.ShowWindow();
            });
        }

    }

    #region ItemsOnTheGroundRegion
    private void OnItemDrop(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            RecipeBookItemList packet = new RecipeBookItemList(data);

            EventProcessor.Instance.QueueEvent(() => {
                RecipeBookWindow.Instance.AddData(packet);
                RecipeBookWindow.Instance.ShowWindow();
            });
        }

    }
    private void OnItemGet(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            RecipeBookItemList packet = new RecipeBookItemList(data);

            EventProcessor.Instance.QueueEvent(() => {
                RecipeBookWindow.Instance.AddData(packet);
                RecipeBookWindow.Instance.ShowWindow();
            });
        }

    }
    #endregion


    #region PartyRegion
    private void OnAskJoinParty(byte[] data)
    {
        
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            AskJoinParty packet = new AskJoinParty(data);

            EventProcessor.Instance.QueueEvent(() => {
                PartyInvitationWindow.Instance.AddData(packet);
                PartyInvitationWindow.Instance.ShowWindow();
            });
        }
    }

    #endregion


    #region TradeRegion

    private void OnTradeStart(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            TradeStart packet = new TradeStart(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                TradeWindow.Instance.AddData(packet);
                TradeWindow.Instance.ShowWindow();
            });
        }
    }

    private void OnTradeDone(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            //TradeStart packet = new TradeStart(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                //TradeWindow.Instance.AddData(packet);
                TradeWindow.Instance.HideWindow();
            });
        }
    }

    private void OnTradeOtherAdd(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            //TradeStart packet = new TradeStart(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                //TradeWindow.Instance.AddData(packet);
                //TradeWindow.Instance.HideWindow();
            });
        }
    }

    private void OnTradeOwnAdd(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            //TradeStart packet = new TradeStart(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                //TradeWindow.Instance.AddData(packet);
                //TradeWindow.Instance.HideWindow();
            });
        }
    }
    private void OnTradeOwnOk(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            //TradeStart packet = new TradeStart(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                //TradeWindow.Instance.AddData(packet);
                //TradeWindow.Instance.HideWindow();
            });
        }
    }

    private void OnTradeOtherOk(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            //TradeStart packet = new TradeStart(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                //TradeWindow.Instance.AddData(packet);
                //TradeWindow.Instance.HideWindow();
            });
        }
    }

    private void OnTradeRequest(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            SendTradeRequest packet = new SendTradeRequest(data);

            EventProcessor.Instance.QueueEvent(() =>
            {
                TradeRequestWindow.Instance.AddData(packet);
                TradeRequestWindow.Instance.ShowWindow();
            });
        }
    }
    #endregion

    private void OnRecipeItemMakeInfo(byte[] data)
    {
        if (!InitPacketsLoadWord.getInstance().IsInit)
        {
            RecipeItemMakeInfo packet = new RecipeItemMakeInfo(data);

            EventProcessor.Instance.QueueEvent(() => {
                RecipeBookWindow.Instance.HideWindow();
                CraftingItemWindow.Instance.AddData(packet);
                CraftingItemWindow.Instance.ShowWindow();
            });
        }

    }

    public void OnExPledgeReceivePowerInfo(byte[] data)
    {
        PledgeReceivePowerInfo showPowerInfo = new PledgeReceivePowerInfo(data);
        EventProcessor.Instance.QueueEvent(() => {
            ClanWindow.Instance.UpdateDetailedInfo(showPowerInfo);
        });
    }

    private void OnStatusUpdate(byte[] data)
    {
        StatusUpdate packet = new StatusUpdate(data);

        if(EventProcessor.Instance != null)
        {
            if(World.Instance != null)
            {
                EventProcessor.Instance.QueueEvent(() => World.Instance.StatusUpdate(packet.ObjectId, packet.Attributes));
            }
           
        }
    }

    private void OnTargetUnselected(byte[] data)
    {
       // Abso: This packet is responsible for the player's head animation.
       // When the player selects a target, the head turns toward the target's position,
       // this packet(TargetUnselected) restores the head back to its normal position.
       // ObjId will always be the current player's object ID, not the target's ID.
       // Unselecting a target is implemented using RequestTargetCanceled.

        //TargetUnselected packet = new TargetUnselected(data);
        //EventProcessor.Instance.QueueEvent(() => TargetManager.Instance.UnselectedTarget(packet.ObjId));
    }



    
  

    protected override byte[] DecryptPacket(byte[] data)
    {
        throw new System.NotImplementedException();
    }
}
