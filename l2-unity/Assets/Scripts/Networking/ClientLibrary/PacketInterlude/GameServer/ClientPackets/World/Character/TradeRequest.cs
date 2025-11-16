//public class TradeRequest : ClientPacket
//{
//    private int _objectId;

//    public TradeRequest() : base((byte)GameInterludeClientPacketType.TradeRequest)
//    {
//    }

//    //protected override void RunImpl()
//    //{
//    //    var player = GetPlayer();
//    //    if (player == null)
//    //    {
//    //        return;
//    //    }

//    //    if (!player.AccessLevel.AllowTransaction())
//    //    {
//    //        player.SendMessage("Transactions are disabled for your current Access Level.");
//    //        player.SendPacket(ActionFailed.STATIC_PACKET);
//    //        return;
//    //    }

//    //    var target = World.Instance.FindObject(_objectId);

//    //    // If there is no target, target is far away or
//    //    // they are in different instances (except multiverse)
//    //    // trade request is ignored and there is no system message.
//    //    if ((target == null) || !player.IsInSurroundingRegion(target) || ((target.InstanceId != player.InstanceId) && (player.InstanceId != -1)))
//    //    {
//    //        return;
//    //    }

//    //    // If target and acting player are the same, trade request is ignored
//    //    // and the following system message is sent to acting player.
//    //    if (target.ObjectId == player.ObjectId)
//    //    {
//    //        player.SendPacket(SystemMessageId.THAT_IS_THE_INCORRECT_TARGET);
//    //        return;
//    //    }

//    //    if (!target.IsPlayer())
//    //    {
//    //        player.SendPacket(SystemMessageId.INVALID_TARGET);
//    //        return;
//    //    }

//    //    var partner = target.AsPlayer();
//    //    if (partner.IsInOlympiadMode() || player.IsInOlympiadMode())
//    //    {
//    //        player.SendMessage("A user currently participating in the Olympiad cannot accept or request a trade.");
//    //        return;
//    //    }

//    //    // Customs: Karma punishment
//    //    if (!Config.ALT_GAME_KARMA_PLAYER_CAN_TRADE && (player.Karma > 0))
//    //    {
//    //        player.SendMessage("You cannot trade while you are in a chaotic state.");
//    //        return;
//    //    }

//    //    if (!Config.ALT_GAME_KARMA_PLAYER_CAN_TRADE && (partner.Karma > 0))
//    //    {
//    //        player.SendMessage("You cannot request a trade while your target is in a chaotic state.");
//    //        return;
//    //    }

//    //    if (Config.JAIL_DISABLE_TRANSACTION && (player.IsJailed() || partner.IsJailed()))
//    //    {
//    //        player.SendMessage("You cannot trade while you are in in Jail.");
//    //        return;
//    //    }

//    //    if (player.IsInStoreMode() || partner.IsInStoreMode())
//    //    {
//    //        player.SendPacket(SystemMessageId.WHILE_OPERATING_A_PRIVATE_STORE_OR_WORKSHOP_YOU_CANNOT_DISCARD_DESTROY_OR_TRADE_AN_ITEM);
//    //        return;
//    //    }

//    //    if (player.IsProcessingTransaction())
//    //    {
//    //        player.SendPacket(SystemMessageId.YOU_ARE_ALREADY_TRADING_WITH_SOMEONE);
//    //        return;
//    //    }

//    //    SystemMessage sm;
//    //    if (partner.IsProcessingRequest() || partner.IsProcessingTransaction())
//    //    {
//    //        sm = new SystemMessage(SystemMessageId.S1_IS_ALREADY_TRADING_WITH_ANOTHER_PERSON_PLEASE_TRY_AGAIN_LATER);
//    //        sm.AddString(partner.Name);
//    //        player.SendPacket(sm);
//    //        return;
//    //    }

//    //    if (partner.TradeRefusal)
//    //    {
//    //        player.SendMessage("That person is in trade refusal mode.");
//    //        return;
//    //    }

//    //    if (BlockList.IsBlocked(partner, player))
//    //    {
//    //        sm = new SystemMessage(SystemMessageId.S1_HAS_PLACED_YOU_ON_HIS_HER_IGNORE_LIST);
//    //        sm.AddString(partner.Name);
//    //        player.SendPacket(sm);
//    //        return;
//    //    }

//    //    if (player.CalculateDistance3D(partner) > 150)
//    //    {
//    //        player.SendPacket(SystemMessageId.YOUR_TARGET_IS_OUT_OF_RANGE);
//    //        return;
//    //    }

//    //    player.OnTransactionRequest(partner);
//    //    partner.SendPacket(new SendTradeRequest(player.ObjectId));
//    //    sm = new SystemMessage(SystemMessageId.YOU_HAVE_REQUESTED_A_TRADE_WITH_S1);
//    //    sm.AddString(partner.Name);
//    //    player.SendPacket(sm);
//    //}
//}