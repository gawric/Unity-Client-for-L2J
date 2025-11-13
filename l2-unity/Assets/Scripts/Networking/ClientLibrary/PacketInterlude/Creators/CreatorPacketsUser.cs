using FMOD.Studio;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class CreatorPacketsUser 
{
    public static RequestSkillCoolTime CreatSkillCoolTime()
    {
        return new RequestSkillCoolTime();
    }
    public static MoveBackwardToLocation CreateMoveToLocation(Vector3 position, Vector3 target)
    {
        return new MoveBackwardToLocation(position, target);
    }

    public static UseItem CreateUseItem(int _objectId, int _ctrlPressed)
    {
        return new UseItem(_objectId, _ctrlPressed);
    }

    public static RequestDestroyItem CreateDestroyItem(int _objectId, int _count)
    {
        return new RequestDestroyItem(_objectId, _count);
    }

    public static RequestEnchantItem CreateEnchantItem(int _objectId)
    {
        return new RequestEnchantItem(_objectId);
    }

    public static RequestOustPledgeMember CreateRequestOustPledgeMember(string memberName)
    {
        return new RequestOustPledgeMember(memberName);
    }

    public static RequestGiveNickName CreateRequestGiveNickName(string memberName, string title)
    {
        return new RequestGiveNickName(memberName, title);
    }

    public static RequestJoinPledge CreateRequestJoinPledge(int _objectId)
    {
        return new RequestJoinPledge(_objectId);
    }

    public static RequestPledgePower CreateRequestPledgePower(int rank, int action, int privs)
    {
        return new RequestPledgePower(rank, action, privs);
    }

    public static RequestPackageSendableItemList CreateSendableItemList(int _objectId)
    {
        return new RequestPackageSendableItemList(_objectId);
    }

    public static MultiSellChoose CreateMultiSellChoose(int listId, int entryId, int amoun)
    {
        return new MultiSellChoose(listId , entryId , amoun);
    }

    public static RequestBuyItem CreateRequestBuyItem(int listId, List<Product> buyList)
    {
        return new RequestBuyItem(listId,  buyList);
    }

    public static RequestPackageSend RequestPackageSend(int objectId, List<Product> productList)
    {
        return new RequestPackageSend(objectId, productList);
    }

    public static RequestPreviewItem CreateRequestPreviewList(int listId, List<Product> buyList)
    {
        return new RequestPreviewItem(listId, buyList);
    }

    public static RequestPledgeInfo CreateRequestPledgeInfo(int clanId)
    {
        return new RequestPledgeInfo(clanId);
    }

    public static RequestPledgeMemberInfo CreateRequestPladgeMemberInfo(string memberName)
    {
        return new RequestPledgeMemberInfo(memberName);
    }

    public static RequestPledgeMemberPowerInfo CreateRequestPledgeMemberPowerInfo(string memberName)
    {
        return new RequestPledgeMemberPowerInfo(memberName);
    }

    public static RequestPledgePowerGradeList CreateRequestPledgePowerGradeList()
    {
        return new RequestPledgePowerGradeList();
    }

    public static RequestPledgeSetMemberPowerGrade CreateRequestPledgeSetMemberPowerGrade(string memberName, int powerGrade)
    {
        return new RequestPledgeSetMemberPowerGrade(memberName, powerGrade);
    }

    public static RequestWithdrawPledge CreateRequestWithdrawPledge()
    {
        return new RequestWithdrawPledge();
    }

    public static RequestSellItem CreateRequestSellItem(int listId, List<Product> buyList)
    {
        return new RequestSellItem(listId, buyList);
    }

    public static RequestQuestAbort CreateRequestQuestAbort(int questId)
    {
        return new RequestQuestAbort(questId);
    }


    public static RequestRecipeItemMakeSelf CreateRequestRecipeItemMakeSelf(int recipeId)
    {
        return new RequestRecipeItemMakeSelf(recipeId);
    }

    public static RequestRecipeBookDestroy CreateRequestRecipeBookDestroy(int recipeId)
    {
        return new RequestRecipeBookDestroy(recipeId);
    }


    public static RequestRecipeBookOpen CreateRequestRecipeBookOpen(int isDwarven)
    {
        return new RequestRecipeBookOpen(isDwarven);
    }
    public static RequestRecipeItemMakeInfo CreateRequestRecipeItemMakeInfo(int recipeId)
    {
        return new RequestRecipeItemMakeInfo(recipeId);
    }

    public static SendWarehouseDepositList CreateWHDepositList(List<Product> depositList)
    {
        return new SendWarehouseDepositList(depositList);
    }

    public static SendWarehouseWithdrawList CreateWHWithdrawList(List<Product> withdrawList)
    {
        return new SendWarehouseWithdrawList(withdrawList);
    }


    public static RequestShortCutDel CreateDestroyShortCut(int slot)
    {
        return new RequestShortCutDel(slot);
    }

    public static RequestUserCommand CreateRequestUserCommand(int idCommand)
    {
        return new RequestUserCommand(idCommand);
    }

    public static RequestShortCutReg CreateRegShortCut(int typeId, int slot, int id, int level)
    {
        return new RequestShortCutReg(typeId, slot, id, level);
    }

    public static ClickAction CreateActiont(int objectId, int originX, int originY, int originZ, int actionId)
    {
        return new ClickAction(objectId, originX, originY, originZ , actionId);
    }

    public static RequestMagicSkillUse CreateMagicSkilluse(int skillId)
    {
        return new RequestMagicSkillUse(skillId , 0 , 0);
    }
    public static Appearing CreateAppearing()
    {
        return new Appearing();
    }

    public static RequestAcquireSkillInfo CreateRequestAcquireSkillInfo(int skillId, int skillLevel, int skillType)
    {
        return new RequestAcquireSkillInfo(skillId, skillLevel, skillType);
    }

    public static RequestAcquireSkill CreateRequestAcquireSkill(int skillId, int skillLevel, int skillType)
    {
        return new RequestAcquireSkill(skillId, skillLevel, skillType);
    }

    public static ValidatePosition CreateValidatePosition(float x, float y, float z)
    {
        return new ValidatePosition(x, y, z);
    }

    public static RequestRestartPoint CreateRestartPoint()
    {
        return new RequestRestartPoint();
    }

    public static RequestBypassToServer CreateByPassPacket(string bypasscommand)
    {
        return new RequestBypassToServer(bypasscommand);
    }

}
