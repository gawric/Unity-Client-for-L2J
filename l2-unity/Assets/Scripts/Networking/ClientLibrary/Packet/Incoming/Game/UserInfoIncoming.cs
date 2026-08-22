using UnityEngine;

[IncomingGamePacket(GameServerPacketType.UserInfo)]
public sealed class UserInfoIncoming : IncomingWirePacket<UserInfoDto>
{
    protected override UserInfoDto CreateDto()
    {
        return new UserInfoDto(IncomingPacketActions.Game.PlayerInfo);
    }

    public override void Apply(UserInfoDto userInfo)
    {
        StorageNpc.getInstance().AddUserInfo(userInfo);
        IncomingPacketActions.Game.SetDataPreparationCompleted(true);
        PlayerAppearance app = userInfo != null ? userInfo.PlayerInfoInterlude.Appearance : null;
        int uid = userInfo != null && userInfo.PlayerInfoInterlude.Identity != null
            ? userInfo.PlayerInfoInterlude.Identity.Id
            : 0;
        GearFlowLog.Info("UserInfo RECV id=" + uid + " " + GearFlowLog.Paperdoll(app) +
            " local=" + (PlayerEntity.Instance != null ? GearFlowLog.Entity(PlayerEntity.Instance) : "PlayerEntity=null"));
        IncomingPacketActions.Queue(() =>
        {
            if (IncomingPacketActions.GameWorld != null)
            {
                IncomingPacketActions.GameWorld.UserInfoUpdateCharacter(userInfo);
                if (PlayerEntity.Instance != null)
                {
                    IncomingPacketActions.GameWorld.UpdateUserInfo(PlayerEntity.Instance, userInfo);
                }
            }

            if (userInfo.PlayerInfoInterlude.Identity.ClanId != 0)
            {
                IncomingPacketActions.Game.Send(new RequestPledgeInfoCommand(userInfo.PlayerInfoInterlude.Identity.ClanId));
            }
        });
    }
}
