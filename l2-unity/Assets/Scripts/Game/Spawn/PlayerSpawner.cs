using UnityEngine;

public sealed class PlayerSpawner
{
    private readonly CameraController _camera;
    private readonly CharacterInfoWindow _characterInfo;
    private readonly CharacterBuilder _characterBuilder;

    public PlayerSpawner(
        CameraController camera,
        CharacterInfoWindow characterInfo,
        CharacterBuilder characterBuilder)
    {
        _camera = camera;
        _characterInfo = characterInfo;
        _characterBuilder = characterBuilder;
    }

    public PlayerEntity Spawn(
        EntityIdentity identity,
        PlayerStatus status,
        PlayerStats stats,
        PlayerAppearance appearance,
        IWorldSpawnContext world)
    {
        if (identity == null || world == null)
            return null;

        identity.SetPosY(world.GetGroundHeight(identity.Position));
        identity.EntityType = EntityType.Player;

        CharacterRace race = (CharacterRace)appearance.Race;
        CharacterRaceAnimation raceId = CharacterRaceAnimationParser.ParseRaceInterlude(
            race, appearance.Sex, appearance.BaseClass);

        GameObject go = _characterBuilder.BuildCharacterBaseInterlude(raceId, appearance, identity.EntityType);
        if (go == null)
            return null;

        EntitySpawnShared.Place(go, world.UsersContainer, identity.Position, identity.Heading, identity.Name);

        PlayerEntity player = go.GetComponent<PlayerEntity>();
        player.Status = status;
        player.Identity = identity;
        player.Stats = stats;
        player.Appearance = appearance;
        player.Race = race;
        player.RaceId = raceId;
        player.Running = appearance.Running;
        player.SetDead(false);

        go.GetComponent<NetworkTransformShare>().enabled = true;
        PlayerController controller = go.GetComponent<PlayerController>();
        controller.enabled = true;
        controller.Initialize();
        App.InjectGameObject(go);
        go.SetActive(true);

        PlayerAnimationController animation = go.GetComponentInChildren<PlayerAnimationController>();
        animation.Initialize();
        EntitySpawnShared.RegisterAnimation(world, identity.Id, animation, player);

        Gear gear = go.GetComponent<Gear>();
        if (gear != null)
            gear.Initialize(player.Identity.Id, player.RaceId);

        PlayerStats statsIntr = (PlayerStats)player.Stats;
        player.Initialize();
        player.UpdateRunSpeed(statsIntr.RunRealSpeed);
        player.UpdateWalkSpeed(statsIntr.WalkRealSpeed);
        player.UpdatePAtkSpeedPlayer((int)statsIntr.BasePAtkSpeed);
        player.UpdateMAtkSpeed((int)statsIntr.MAtkSpd);

        CameraController camera = _camera != null ? _camera : CameraController.Instance;
        if (camera != null)
        {
            camera.enabled = true;
            camera.SetTarget(go);
            camera.SetHeading(identity.OrigHeading);
        }

        if (_characterInfo != null)
            _characterInfo.UpdateValues();
        if (PlayerStateMachine.Instance != null)
            PlayerStateMachine.Instance.Player = player;

        world.RegisterPlayer(player);
        return player;
    }

    public void UpdateInfo(Entity entity, UserInfoDto userInfo)
    {
        if (entity == null || userInfo == null || !(entity is PlayerEntity))
            return;

        PlayerEntity player = (PlayerEntity)entity;
        GearFlowLog.Info("PlayerSpawner.UpdateInfo " +
            GearFlowLog.Entity(player) + " " + GearFlowLog.Paperdoll(player));
        PlayerStats statsIntr = userInfo.PlayerInfoInterlude.Stats;
        player.UpdateRunSpeed(statsIntr.RunRealSpeed);
        player.UpdateWalkSpeed(statsIntr.WalkRealSpeed);
        player.UpdatePAtkSpeedPlayer((int)statsIntr.BasePAtkSpeed);
        player.UpdateMAtkSpeed((int)statsIntr.MAtkSpd);
        player.RefreshVisuals();
    }
}
