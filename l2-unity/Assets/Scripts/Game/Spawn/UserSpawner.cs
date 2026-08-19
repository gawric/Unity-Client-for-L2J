using System;
using UnityEngine;

public sealed class UserSpawner
{
    private readonly CharacterBuilder _characterBuilder;
    private readonly Func<UserEntity, UserBowArrowEvents> _createBowArrowEvents;

    public UserSpawner(
        CharacterBuilder characterBuilder,
        Func<UserEntity, UserBowArrowEvents> createBowArrowEvents)
    {
        _characterBuilder = characterBuilder;
        _createBowArrowEvents = createBowArrowEvents;
    }

    public UserEntity Spawn(CharInfoDto info, IWorldSpawnContext world)
    {
        if (info == null || info.Identity == null || world == null)
            return null;

        EntityIdentity identity = info.Identity;
        PlayerAppearance appearance = info.Appearance;
        identity.SetPosY(world.GetGroundHeight(identity.Position));
        identity.EntityType = EntityType.User;

        CharacterRace race = (CharacterRace)appearance.Race;
        CharacterRaceAnimation raceId = ResolveRaceAnimation(race, appearance, identity);

        CharacterBuilder builder = _characterBuilder != null ? _characterBuilder : CharacterBuilder.Instance;
        if (builder == null)
            return null;

        GameObject go = builder.BuildCharacterBase(raceId, appearance, EntityType.User);
        if (go == null)
            return null;

        EntitySpawnShared.Place(go, world.UsersContainer, identity.Position, identity.Heading, identity.Name);

        UserEntity user = go.GetComponent<UserEntity>();
        if (user == null)
        {
            UnityEngine.Object.Destroy(go);
            return null;
        }

        user.Status = info.Status;
        user.Identity = identity;
        user.Stats = info.Stats;
        user.Appearance = appearance;
        user.Race = race;
        user.RaceId = raceId;
        user.Running = appearance.Running;
        user.SetDead(info.AlikeDead);
        Debug.Log("[EntityAction:User] nick=" + identity.Name +
            " spawn hp=" + user.Status.GetHp() +
            " maxHp=" + user.Stats.MaxHp +
            " deadFlag=" + user.GetDead() +
            " isDead=" + user.IsDead() +
            " alikeDead=" + info.AlikeDead +
            " cp=" + (user.Status is PlayerStatus ps ? ps.Cp : 0));

        PlayerController playerController = go.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;

        NetworkTransformShare share = go.GetComponent<NetworkTransformShare>();
        if (share != null)
            share.enabled = false;

        NetworkTransformReceive receive = go.GetComponent<NetworkTransformReceive>();
        if (receive != null)
            receive.enabled = false;

        EntitySpawnShared.SanitizeCharacterControllerStepOffset(go);
        App.InjectGameObject(go);
        go.SetActive(true);
        EntitySpawnShared.ReapplyGroundAfterActivate(go, identity);

        UserGear gear = go.GetComponent<UserGear>();
        if (gear != null)
            gear.Initialize(identity.Id, raceId);

        NetworkAnimationController animation = go.GetComponent<NetworkAnimationController>();
        if (animation != null)
            animation.Initialize();

        user.Initialize();

        if (animation != null)
            EntitySpawnShared.RegisterAnimation(world, identity.Id, animation, user);

        if (_createBowArrowEvents != null)
            user.BindCharInfoAnimEvents(_createBowArrowEvents(user));

        user.UpdateRunSpeed(info.Stats.RunRealSpeed);
        user.UpdateWalkSpeed(info.Stats.WalkRealSpeed);
        user.UpdatePAtkSpeed((int)info.Stats.PAtkSpd);
        user.UpdateMAtkSpeed((int)info.Stats.MAtkSpd);
        user.LogSpeed("spawn packet RunReal=" + info.Stats.RunRealSpeed +
            " WalkReal=" + info.Stats.WalkRealSpeed +
            " BaseRun=" + info.Stats.BaseRunSpeed +
            " BaseWalk=" + info.Stats.BaseWalkingSpeed);

        world.RegisterUser(user);
        if (info.AlikeDead || user.IsDead())
            EntityActionVisual.PlayDeath(user, true);
        return user;
    }

    public void UpdateInfo(Entity entity, CharInfoDto info)
    {
        if (entity == null || info == null || !(entity is UserEntity))
            return;

        UserEntity user = (UserEntity)entity;
        EntityIdentity identity = info.Identity;
        user.Identity.Position = identity.Position;
        user.Identity.Heading = identity.Heading;
        user.Identity.OrigHeading = identity.OrigHeading;
        user.Identity.Name = identity.Name;
        user.Identity.Title = identity.Title;
        user.Identity.PlayerClass = identity.PlayerClass;
        user.Identity.IsRunning = identity.IsRunning;
        user.Identity.ClanId = identity.ClanId;

        GearFlowLog.Info("UserSpawner.UpdateInfo BEFORE nick=" + user.Nick +
            " id=" + (user.Identity != null ? user.Identity.Id : 0) +
            " " + GearFlowLog.Paperdoll(user));
        GearFlowLog.Info("UserSpawner.UpdateInfo PACKET nick=" + identity.Name +
            " id=" + identity.Id + " " + GearFlowLog.Paperdoll(info.Appearance));

        bool wasCorpse = user.GetDead();
        user.Status = info.Status;
        user.Stats = info.Stats;
        user.Appearance = info.Appearance;
        user.Running = info.Appearance.Running;
        user.SetDead(info.AlikeDead);

        user.UpdateRunSpeed(info.Stats.RunRealSpeed);
        user.UpdateWalkSpeed(info.Stats.WalkRealSpeed);
        user.UpdatePAtkSpeed((int)info.Stats.PAtkSpd);
        user.UpdateMAtkSpeed((int)info.Stats.MAtkSpd);
        user.LogSpeed("update CharInfo RunReal=" + info.Stats.RunRealSpeed +
            " WalkReal=" + info.Stats.WalkRealSpeed);
        GearFlowLog.Info("UserSpawner.RefreshVisuals nick=" + user.Nick);
        user.RefreshVisuals();

        EntitySpawnShared.ApplyGroundedTransform(user.gameObject, identity.Position, identity.Heading);
        EntitySpawnShared.ReapplyGroundAfterActivate(user.gameObject, user.Identity);

        if (info.AlikeDead)
        {
            EntityActionCombatLog.LogIfWatch(user,
                "CharInfo dead nick=" + user.Nick +
                " wasCorpse=" + wasCorpse +
                " alikeDead=" + info.AlikeDead);
            if (wasCorpse)
                return;
            if (EntityActionMachine.Instance != null)
                EntityActionMachine.Instance.Die(user, false);
            else
                EntityActionVisual.PlayDeath(user, false);
        }
        else if (wasCorpse && EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Revive(user);
    }

    static CharacterRaceAnimation ResolveRaceAnimation(
        CharacterRace race,
        PlayerAppearance appearance,
        EntityIdentity identity)
    {
        int baseClass = appearance.BaseClass;
        if (baseClass != (int)BaseClass.Fighter && baseClass != (int)BaseClass.MMagic)
        {
            bool isMage = CharacterClassParser.IsMage((CharacterClass)identity.PlayerClass);
            baseClass = isMage ? (int)BaseClass.MMagic : (int)BaseClass.Fighter;
        }

        return CharacterRaceAnimationParser.ParseRaceInterlude(race, appearance.Sex, baseClass);
    }
}
