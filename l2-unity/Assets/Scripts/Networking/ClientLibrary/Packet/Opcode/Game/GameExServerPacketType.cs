using UnityEngine;

public enum GameExServerPacketType : byte
{
    ExDuelAskStart = 0x4B,
    ExDuelReady = 0x4C,
    ExDuelStart = 0x4D,
    ExDuelEnd = 0x4E,
    ExDuelUpdateUserInfo = 0x4F,
    ExShowQuestInfo = 0x19,
    ExShowSellCropList = 0x21,
    ExShowSeedInfo = 0x1C,
    ExShowCropInfo = 0x1D,
    ExShowManorDefaultInfo = 0x1E,
    ExPledgeReceiveMemberInfo = 0x3d,
    ExPledgeReceivePowerInfo = 0x3c,
    ExOnExPledgePowerGradeList = 0x3b
}
