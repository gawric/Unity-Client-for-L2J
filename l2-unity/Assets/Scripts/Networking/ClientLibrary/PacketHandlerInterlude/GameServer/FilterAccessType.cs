using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterAccessType
{
    private static readonly Dictionary<GameInterludeServerPacketType, bool> GamePacketTypes = new Dictionary<GameInterludeServerPacketType, bool>
    {
        { GameInterludeServerPacketType.InterludeKeyPacket, true },
        { GameInterludeServerPacketType.CharSelectionInfo, true },
        { GameInterludeServerPacketType.CharTemplate, true },
        { GameInterludeServerPacketType.CharCreateOk, true },
        { GameInterludeServerPacketType.SocialAction, true },
        { GameInterludeServerPacketType.CharSelected, true },
        { GameInterludeServerPacketType.CharCreateFail, true },
        { GameInterludeServerPacketType.SkillList, true },
        { GameInterludeServerPacketType.UserInfo, true },
        { GameInterludeServerPacketType.SkillCoolTime, true },
        { GameInterludeServerPacketType.MacroList, true },
        { GameInterludeServerPacketType.NpcHtmlMessage, true },
        { GameInterludeServerPacketType.PackageToList, true },
        { GameInterludeServerPacketType.TutorialShowHtml, true },
        { GameInterludeServerPacketType.BuyList, true },
        { GameInterludeServerPacketType.ShopPreviewList, true },
        { GameInterludeServerPacketType.ShopPreviewInfo, true },
        { GameInterludeServerPacketType.MultiSellList, true },
        { GameInterludeServerPacketType.SellList, true },
        { GameInterludeServerPacketType.WhDepositList, true },
        { GameInterludeServerPacketType.PackageSendableList, true },
        { GameInterludeServerPacketType.WhWithdrawList, true },
        { GameInterludeServerPacketType.BuyListSeed, true },
        { GameInterludeServerPacketType.ShortCutInit, true },
        { GameInterludeServerPacketType.ShortCutRegister, true },
        { GameInterludeServerPacketType.ShortCutDel, true },
        { GameInterludeServerPacketType.HennaInfo, true },
        { GameInterludeServerPacketType.QuestList, true },
        { GameInterludeServerPacketType.PledgeShowMemberListUpdate, true },
        { GameInterludeServerPacketType.PledgeShowMemberListDelete, true },
        { GameInterludeServerPacketType.PledgeShowMemberListAll, true },
        { GameInterludeServerPacketType.PledgeShowMemberListDeleteAll, true },
        { GameInterludeServerPacketType.PledgeShowMemberListAdd, true },
        { GameInterludeServerPacketType.PledgeInfo, true },
        { GameInterludeServerPacketType.ManagePledgePower, true },
        { GameInterludeServerPacketType.PledgeStatusChanged, true },
        { GameInterludeServerPacketType.NpcInfo, true },
        { GameInterludeServerPacketType.DeleteObject, true },
        { GameInterludeServerPacketType.CharMoveToLocation, true },
        { GameInterludeServerPacketType.ValidateLocation, true },
        { GameInterludeServerPacketType.FriendList, true },
        { GameInterludeServerPacketType.EtcStatusUpdate, true },
        { GameInterludeServerPacketType.ExTypePacket, true },
        { GameInterludeServerPacketType.StatusUpdate, true },
        { GameInterludeServerPacketType.TargetUnselected, true },
        { GameInterludeServerPacketType.TeleportToLocation, true },
        { GameInterludeServerPacketType.Revive, true },
        { GameInterludeServerPacketType.AbnormalStatusUpdate, true },
        { GameInterludeServerPacketType.ShortBuffStatusUpdate, true },
        { GameInterludeServerPacketType.AcquireSkillList, true },
        { GameInterludeServerPacketType.AcquireSkillInfo, true },
        { GameInterludeServerPacketType.RecipeBookItemList, true },
        { GameInterludeServerPacketType.RecipeItemMakeInfo, true },
        { GameInterludeServerPacketType.SendTradeRequest, true },
        { GameInterludeServerPacketType.AskJoinParty, true },
        { GameInterludeServerPacketType.TradeStart, true },
        { GameInterludeServerPacketType.TradeUpdate, true },
        { GameInterludeServerPacketType.TradeDone, true },
        { GameInterludeServerPacketType.TradeOwnAdd, true },
        { GameInterludeServerPacketType.TradeOtherAdd, true },
        { GameInterludeServerPacketType.TradePressOtherOk, true },
        { GameInterludeServerPacketType.TradePressOwnOk, true },
        { GameInterludeServerPacketType.CharInfo, true },
        { GameInterludeServerPacketType.DropItem, true },
        { GameInterludeServerPacketType.GetItem, true },

    };

    private static readonly Dictionary<GSInterludeCombatPacketType, bool> CombatPacketTypes = new Dictionary<GSInterludeCombatPacketType, bool>
    {
        { GSInterludeCombatPacketType.MyTargetSelected, true },
        { GSInterludeCombatPacketType.MoveToPawn, true },
        { GSInterludeCombatPacketType.STOP_MOVE, true },
        { GSInterludeCombatPacketType.ATTACK, true },
        { GSInterludeCombatPacketType.ActionFailed, true },
        { GSInterludeCombatPacketType.AUTO_ATTACK_START, true },
        { GSInterludeCombatPacketType.AUTO_ATTACK_STOP, true },
        { GSInterludeCombatPacketType.DIE, true },
        { GSInterludeCombatPacketType.MagicSkillUse, true },
        { GSInterludeCombatPacketType.SetupGauge, true },
        { GSInterludeCombatPacketType.ItemList, true },
        { GSInterludeCombatPacketType.InventoryUpdate, true },
        { GSInterludeCombatPacketType.ChooseInventoryItem, true },
        { GSInterludeCombatPacketType.EnchantResult, true },
        { GSInterludeCombatPacketType.MagicSkillLaunched, true }

    };

    private static readonly Dictionary<GSInterludeMessagePacketType, bool> MessagePacketTypes = new Dictionary<GSInterludeMessagePacketType, bool>
    {
        { GSInterludeMessagePacketType.SystemMessage, true },
        { GSInterludeMessagePacketType.CreatureSay, true },
        { GSInterludeMessagePacketType.NpcSay, true }
    };

    public static bool IsAccessTypeGame(ItemServer item)
    {
        return GamePacketTypes.ContainsKey(item.PaketType());
    }

    public static bool IsAccessTypeCombat(ItemServer item)
    {
        GSInterludeCombatPacketType type;
        if (Enum.TryParse(item.ByteType().ToString(), out type))
        {
            return CombatPacketTypes.ContainsKey(type);
        }
        return false;
    }

    public static bool IsAccessTypeMessage(ItemServer item)
    {
        GSInterludeMessagePacketType type;
        if (Enum.TryParse(item.ByteType().ToString(), out type))
        {
            return MessagePacketTypes.ContainsKey(type);
        }
        return false;
    }
}
