using System.Collections.Generic;
using UnityEngine;

public sealed class MoveToCommand : INetworkCommand
{
    public Vector3 From;
    public Vector3 To;

    public MoveToCommand(Vector3 from, Vector3 to)
    {
        From = from;
        To = to;
    }
}

public sealed class UseItemCommand : INetworkCommand
{
    public int ObjectId;
    public int CtrlPressed;

    public UseItemCommand(int objectId, int ctrlPressed)
    {
        ObjectId = objectId;
        CtrlPressed = ctrlPressed;
    }
}

public sealed class ClickActionCommand : INetworkCommand
{
    public int ObjectId;
    public int OriginX;
    public int OriginY;
    public int OriginZ;
    public int ActionId;

    public ClickActionCommand(int objectId, int originX, int originY, int originZ, int actionId)
    {
        ObjectId = objectId;
        OriginX = originX;
        OriginY = originY;
        OriginZ = originZ;
        ActionId = actionId;
    }
}

public sealed class ValidatePositionCommand : INetworkCommand
{
    public float X;
    public float Y;
    public float Z;

    public ValidatePositionCommand(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public sealed class AppearingCommand : INetworkCommand
{
}

public sealed class EnterWorldCommand : INetworkCommand
{
}

public sealed class NewCharacterCommand : INetworkCommand
{
}

public sealed class ProtocolVersionCommand : INetworkCommand
{
    public int Protocol;

    public ProtocolVersionCommand(int protocol)
    {
        Protocol = protocol;
    }
}

public sealed class CharacterSelectCommand : INetworkCommand
{
    public int Slot;

    public CharacterSelectCommand(int slot)
    {
        Slot = slot;
    }
}

public sealed class CharacterDeleteCommand : INetworkCommand
{
    public int Slot;

    public CharacterDeleteCommand(int slot)
    {
        Slot = slot;
    }
}

public sealed class CharacterCreateCommand : INetworkCommand
{
    public List<PlayerTemplates> Templates;
    public string ClassName;
    public string Sex;
    public string HairColor;
    public string HairStyle;
    public string Face;
    public string RaceName;
    public string Name;

    public CharacterCreateCommand(
        List<PlayerTemplates> templates,
        string className,
        string sex,
        string hairColor,
        string hairStyle,
        string face,
        string raceName,
        string name)
    {
        Templates = templates;
        ClassName = className;
        Sex = sex;
        HairColor = hairColor;
        HairStyle = hairStyle;
        Face = face;
        RaceName = raceName;
        Name = name;
    }
}

public sealed class AuthLoginCommand : INetworkCommand
{
    public string Account;
    public int PlayKey1;
    public int PlayKey2;
    public int LoginKey1;
    public int LoginKey2;

    public AuthLoginCommand(string account, int playKey1, int playKey2, int loginKey1, int loginKey2)
    {
        Account = account;
        PlayKey1 = playKey1;
        PlayKey2 = playKey2;
        LoginKey1 = loginKey1;
        LoginKey2 = loginKey2;
    }
}

public sealed class RequestSkillCoolTimeCommand : INetworkCommand
{
}

public sealed class RequestTargetCanceldCommand : INetworkCommand
{
}

public sealed class RequestRestartPointCommand : INetworkCommand
{
}

public sealed class RequestShowBoardCommand : INetworkCommand
{
}

public sealed class RequestWithdrawPledgeCommand : INetworkCommand
{
}

public sealed class RequestMagicSkillUseCommand : INetworkCommand
{
    public int SkillId;
    public int CtrlPressed;
    public byte ShiftPressed;

    public RequestMagicSkillUseCommand(int skillId, int ctrlPressed, byte shiftPressed)
    {
        SkillId = skillId;
        CtrlPressed = ctrlPressed;
        ShiftPressed = shiftPressed;
    }
}

public sealed class RequestSay2Command : INetworkCommand
{
    public ChatTypeData Data;
    public string Message;
    public string TargetName;

    public RequestSay2Command(ChatTypeData data, string message, string targetName)
    {
        Data = data;
        Message = message;
        TargetName = targetName;
    }
}

public sealed class RequestBypassToServerCommand : INetworkCommand
{
    public string Bypass;

    public RequestBypassToServerCommand(string bypass)
    {
        Bypass = bypass;
    }
}

public sealed class RequestUserCommandCommand : INetworkCommand
{
    public int Id;

    public RequestUserCommandCommand(int id)
    {
        Id = id;
    }
}

public sealed class RequestDestroyItemCommand : INetworkCommand
{
    public int ObjectId;
    public int Count;

    public RequestDestroyItemCommand(int objectId, int count)
    {
        ObjectId = objectId;
        Count = count;
    }
}

public sealed class RequestDropItemCommand : INetworkCommand
{
    public int ObjectId;
    public int Count;
    public int X;
    public int Y;
    public int Z;

    public RequestDropItemCommand(int objectId, int count, int x, int y, int z)
    {
        ObjectId = objectId;
        Count = count;
        X = x;
        Y = y;
        Z = z;
    }
}

public sealed class RequestEnchantItemCommand : INetworkCommand
{
    public int ObjectId;

    public RequestEnchantItemCommand(int objectId)
    {
        ObjectId = objectId;
    }
}

public sealed class RequestQuestAbortCommand : INetworkCommand
{
    public int QuestId;

    public RequestQuestAbortCommand(int questId)
    {
        QuestId = questId;
    }
}

public sealed class RequestJoinPledgeCommand : INetworkCommand
{
    public int ObjectId;

    public RequestJoinPledgeCommand(int objectId)
    {
        ObjectId = objectId;
    }
}

public sealed class RequestGiveNickNameCommand : INetworkCommand
{
    public string MemberName;
    public string Title;

    public RequestGiveNickNameCommand(string memberName, string title)
    {
        MemberName = memberName;
        Title = title;
    }
}

public sealed class RequestOustPledgeMemberCommand : INetworkCommand
{
    public string MemberName;

    public RequestOustPledgeMemberCommand(string memberName)
    {
        MemberName = memberName;
    }
}

public sealed class RequestPledgePowerCommand : INetworkCommand
{
    public int Rank;
    public int Action;
    public int Privs;

    public RequestPledgePowerCommand(int rank, int action, int privs)
    {
        Rank = rank;
        Action = action;
        Privs = privs;
    }
}

public sealed class RequestPledgeInfoCommand : INetworkCommand
{
    public int ClanId;

    public RequestPledgeInfoCommand(int clanId)
    {
        ClanId = clanId;
    }
}

public sealed class RequestShortCutRegCommand : INetworkCommand
{
    public int TypeId;
    public int WorldSlot;
    public int Id;
    public int Level;

    public RequestShortCutRegCommand(int typeId, int worldSlot, int id, int level)
    {
        TypeId = typeId;
        WorldSlot = worldSlot;
        Id = id;
        Level = level;
    }
}

public sealed class RequestShortCutDelCommand : INetworkCommand
{
    public int WorldSlot;

    public RequestShortCutDelCommand(int worldSlot)
    {
        WorldSlot = worldSlot;
    }
}

public sealed class RequestRecipeBookDestroyCommand : INetworkCommand
{
    public int RecipeId;

    public RequestRecipeBookDestroyCommand(int recipeId)
    {
        RecipeId = recipeId;
    }
}

public sealed class RequestRecipeItemMakeInfoCommand : INetworkCommand
{
    public int RecipeId;

    public RequestRecipeItemMakeInfoCommand(int recipeId)
    {
        RecipeId = recipeId;
    }
}

public sealed class RequestRecipeItemMakeSelfCommand : INetworkCommand
{
    public int RecipeId;

    public RequestRecipeItemMakeSelfCommand(int recipeId)
    {
        RecipeId = recipeId;
    }
}

public sealed class RequestRecipeBookOpenCommand : INetworkCommand
{
    public int IsDwarven;

    public RequestRecipeBookOpenCommand(int isDwarven)
    {
        IsDwarven = isDwarven;
    }
}

public sealed class RequestAcquireSkillCommand : INetworkCommand
{
    public int SkillId;
    public int SkillLevel;
    public int SkillType;

    public RequestAcquireSkillCommand(int skillId, int skillLevel, int skillType)
    {
        SkillId = skillId;
        SkillLevel = skillLevel;
        SkillType = skillType;
    }
}

public sealed class RequestAcquireSkillInfoCommand : INetworkCommand
{
    public int SkillId;
    public int SkillLevel;
    public int SkillType;

    public RequestAcquireSkillInfoCommand(int skillId, int skillLevel, int skillType)
    {
        SkillId = skillId;
        SkillLevel = skillLevel;
        SkillType = skillType;
    }
}

public sealed class RequestAnswerJoinPartyCommand : INetworkCommand
{
    public int Response;

    public RequestAnswerJoinPartyCommand(int response)
    {
        Response = response;
    }
}

public sealed class AnswerTradeRequestCommand : INetworkCommand
{
    public int Response;

    public AnswerTradeRequestCommand(int response)
    {
        Response = response;
    }
}

public sealed class TradeDoneCommand : INetworkCommand
{
    public int Response;

    public TradeDoneCommand(int response)
    {
        Response = response;
    }
}

public sealed class AddTradeItemCommand : INetworkCommand
{
    public int Trade;
    public int ObjectId;
    public int Count;

    public AddTradeItemCommand(int trade, int objectId, int count)
    {
        Trade = trade;
        ObjectId = objectId;
        Count = count;
    }
}

public sealed class MultiSellChooseCommand : INetworkCommand
{
    public int ListId;
    public int EntryId;
    public int Amount;

    public MultiSellChooseCommand(int listId, int entryId, int amount)
    {
        ListId = listId;
        EntryId = entryId;
        Amount = amount;
    }
}

public sealed class RequestBuyItemCommand : INetworkCommand
{
    public int ListId;
    public List<Product> BuyList;

    public RequestBuyItemCommand(int listId, List<Product> buyList)
    {
        ListId = listId;
        BuyList = buyList;
    }
}

public sealed class RequestSellItemCommand : INetworkCommand
{
    public int ListId;
    public List<Product> SellList;

    public RequestSellItemCommand(int listId, List<Product> sellList)
    {
        ListId = listId;
        SellList = sellList;
    }
}

public sealed class RequestPreviewItemCommand : INetworkCommand
{
    public int ListId;
    public List<Product> BuyList;

    public RequestPreviewItemCommand(int listId, List<Product> buyList)
    {
        ListId = listId;
        BuyList = buyList;
    }
}

public sealed class SendWarehouseDepositListCommand : INetworkCommand
{
    public List<Product> SellList;

    public SendWarehouseDepositListCommand(List<Product> sellList)
    {
        SellList = sellList;
    }
}

public sealed class SendWarehouseWithdrawListCommand : INetworkCommand
{
    public List<Product> SellList;

    public SendWarehouseWithdrawListCommand(List<Product> sellList)
    {
        SellList = sellList;
    }
}

public sealed class RequestPackageSendCommand : INetworkCommand
{
    public int ObjectId;
    public List<Product> BuyList;

    public RequestPackageSendCommand(int objectId, List<Product> buyList)
    {
        ObjectId = objectId;
        BuyList = buyList;
    }
}

public sealed class RequestPackageSendableItemListCommand : INetworkCommand
{
    public int ObjectId;

    public RequestPackageSendableItemListCommand(int objectId)
    {
        ObjectId = objectId;
    }
}

public sealed class AuthGameGuardCommand : INetworkCommand
{
    public int SessionId;
    public int[] Gg;

    public AuthGameGuardCommand(int sessionId, int[] gg)
    {
        SessionId = sessionId;
        Gg = gg;
    }
}

public sealed class RequestAuthLoginCommand : INetworkCommand
{
    public string Account;
    public string Password;
    public int Response;

    public RequestAuthLoginCommand(string account, string password, int response)
    {
        Account = account;
        Password = password;
        Response = response;
    }
}

public sealed class RequestServerListCommand : INetworkCommand
{
    public int SessionKey1;
    public int SessionKey2;

    public RequestServerListCommand(int sessionKey1, int sessionKey2)
    {
        SessionKey1 = sessionKey1;
        SessionKey2 = sessionKey2;
    }
}

public sealed class RequestServerLoginCommand : INetworkCommand
{
    public int ServerId;
    public int SessionKey1;
    public int SessionKey2;

    public RequestServerLoginCommand(int serverId, int sessionKey1, int sessionKey2)
    {
        ServerId = serverId;
        SessionKey1 = sessionKey1;
        SessionKey2 = sessionKey2;
    }
}
