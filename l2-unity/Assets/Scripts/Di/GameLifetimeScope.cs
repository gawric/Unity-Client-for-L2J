using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        RegisterIfFound<World>(builder);
        RegisterIfFound<WorldCombat>(builder);
        RegisterIfFound<TargetManager>(builder);
        RegisterIfFound<HitManager>(builder);
        RegisterIfFound<ClickManager>(builder);
        RegisterIfFound<NpcCursorManager>(builder);
        RegisterIfFound<PlayerInventory>(builder);
        RegisterIfFound<PlayerShortcuts>(builder);
        RegisterIfFound<EffectManager>(builder);
        RegisterIfFound<ProjectileManager>(builder);
        RegisterIfFound<ObjectPoolManager>(builder);
        RegisterIfFound<CameraController>(builder);
        RegisterIfFound<InputManager>(builder);
        RegisterIfFound<PositionValidationController>(builder);
        RegisterIfFound<SkillExecutor>(builder);
        RegisterIfFound<CombatFacingService>(builder);
        RegisterIfFound<EventBus>(builder);
        builder.Register<L2ActorFade>(Lifetime.Singleton);
        builder.RegisterEntryPoint<AppearFadeService>().AsSelf();
        RegisterIfFound<DeadManager>(builder);
        RegisterIfFound<MoveAllCharacters>(builder);
        RegisterIfFound<GravityNpc>(builder);
        RegisterIfFound<FNManagerLight>(builder);
        RegisterIfFound<SwordCollisionService>(builder);
        RegisterIfFound<EffectSkillsmanager>(builder);
        RegisterIfFound<PathFinderController>(builder);
        RegisterIfFound<Geodata>(builder);
        RegisterIfFound<AudioManager>(builder);
        RegisterIfFound<MusicManager>(builder);
        RegisterIfFound<SkillsManager>(builder);
        RegisterIfFound<PlayerActions>(builder);
        RegisterIfFound<ListenerCharacterGear>(builder);
        RegisterIfFound<CharacterBuilder>(builder);

        RegisterIfFound<L2GameUI>(builder);
        RegisterIfFound<ChatWindow>(builder);
        RegisterIfFound<HtmlWindow>(builder);
        RegisterIfFound<DealerWindow>(builder);
        RegisterIfFound<MultiSellWindow>(builder);
        RegisterIfFound<TradeWindow>(builder);
        RegisterIfFound<TradeRequestWindow>(builder);
        RegisterIfFound<PartyInvitationWindow>(builder);
        RegisterIfFound<EnchantWindow>(builder);
        RegisterIfFound<RecipeBookWindow>(builder);
        RegisterIfFound<CraftingItemWindow>(builder);
        RegisterIfFound<SkillLearnWindow>(builder);
        RegisterIfFound<DescriptionSkillWindow>(builder);
        RegisterIfFound<SkillListWindow>(builder);
        RegisterIfFound<QuestWindow>(builder);
        RegisterIfFound<QuestListWindow>(builder);
        RegisterIfFound<ClanWindow>(builder);
        RegisterIfFound<ShowListWindow>(builder);
        RegisterIfFound<SeedInfoWindow>(builder);
        RegisterIfFound<SellCropListWindow>(builder);
        RegisterIfFound<SkillbarWindow>(builder);
        RegisterIfFound<SystemMessageWindow>(builder);
        RegisterIfFound<BufferPanel>(builder);
        RegisterIfFound<InventoryWindow>(builder);
        RegisterIfFound<DeadWindow>(builder);
        RegisterIfFound<StatusWindow>(builder);
        RegisterIfFound<CharacterInfoWindow>(builder);

        builder.Register<PlayerSpawner>(Lifetime.Singleton);
        builder.Register<BowArrowVisual>(Lifetime.Singleton);
        builder.RegisterFactory<UserEntity, UserBowArrowEvents>(container =>
        {
            BowArrowVisual bowArrow = container.Resolve<BowArrowVisual>();
            IAnimationManager animations = container.Resolve<IAnimationManager>();
            return user => new UserBowArrowEvents(user, bowArrow, animations);
        }, Lifetime.Singleton);
        builder.Register<UserSpawner>(Lifetime.Singleton);
        builder.Register<NpcSpawner>(Lifetime.Singleton);
        builder.Register<MonsterSpawner>(Lifetime.Singleton);
        builder.Register<ItemSpawner>(Lifetime.Singleton);
        builder.Register<ItemDropLayerService>(Lifetime.Singleton);
        builder.Register<ItemDropClickAreaService>(Lifetime.Singleton);
        builder.Register<ItemDropPicker>(Lifetime.Singleton);
        builder.Register<ItemDropGrpCatalog>(Lifetime.Singleton);
        builder.Register<ItemDropPrefabLoader>(Lifetime.Singleton);
        builder.Register<ItemDropMaterialService>(Lifetime.Singleton);
        builder.Register<ItemDropWeaponAligner>(Lifetime.Singleton);
        builder.Register<ItemDropVisualService>(Lifetime.Singleton);
        builder.Register<ItemDropPresentationService>(Lifetime.Singleton);
        builder.Register<PlayerPositionSender>(Lifetime.Singleton);
        builder.Register<L2PawnRange>(Lifetime.Singleton);
        builder.RegisterEntryPoint<EntityActionMachine>().AsSelf();
        builder.Register<PlayerWorldApply>(Lifetime.Singleton);
        builder.Register<NpcWorldApply>(Lifetime.Singleton);
        builder.Register<MonsterWorldApply>(Lifetime.Singleton);
        builder.Register<UserWorldApply>(Lifetime.Singleton);
        builder.Register<WorldPacketApply>(Lifetime.Singleton);
        builder.Register<MessagePacketApply>(Lifetime.Singleton);
        builder.Register<GameRuntime>(Lifetime.Singleton);

        builder.RegisterBuildCallback(container =>
        {
            App.GameContainer = container;
            try
            {
                GameRuntime runtime = container.Resolve<GameRuntime>();
                WorldPacketApply worldApply = container.Resolve<WorldPacketApply>();
                MessagePacketApply messageApply = container.Resolve<MessagePacketApply>();
                runtime.BindApply(worldApply, messageApply);
                IncomingPacketActions.BindGame(runtime, worldApply, messageApply);
            }
            catch (Exception ex)
            {
                Debug.LogError("GameLifetimeScope BindGame failed: " + ex);
            }
        });
    }

    private void RegisterIfFound<T>(IContainerBuilder builder) where T : MonoBehaviour
    {
        T component = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (component != null)
            builder.RegisterComponent(component);
    }
}
