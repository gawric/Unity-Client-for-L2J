using System;

public static class IncomingPacketActions
{
    public static WorldPacketApply WorldApply { get; private set; }
    public static MessagePacketApply MessageApply { get; private set; }

    private static PacketApplyQueue _applyQueue;
    private static EventProcessor _events;
    private static World _world;
    private static GameClient _game;
    private static LoginClient _login;
    private static GameManager _manager;
    private static GameRuntime _runtime;
    private static LoginRuntime _loginRuntime;
    private static IAnimationManager _animations;

    public static void BindApp(
        EventProcessor events,
        GameClient game,
        LoginClient login,
        GameManager manager,
        IAnimationManager animations,
        PacketApplyQueue applyQueue)
    {
        if (events != null)
            _events = events;
        if (game != null)
            _game = game;
        if (login != null)
            _login = login;
        if (manager != null)
            _manager = manager;
        if (animations != null)
            _animations = animations;
        if (applyQueue != null)
            _applyQueue = applyQueue;
    }

    public static void BindLogin(LoginRuntime runtime)
    {
        _loginRuntime = runtime;
    }

    public static void BindGame(GameRuntime runtime, WorldPacketApply worldApply = null, MessagePacketApply messageApply = null)
    {
        _runtime = runtime;
        if (runtime == null)
            return;
        if (runtime.Events != null)
            _events = runtime.Events;
        if (runtime.World != null)
            _world = runtime.World;
        if (runtime.Game != null)
            _game = runtime.Game;
        if (runtime.Animations != null)
            _animations = runtime.Animations;
        if (worldApply != null)
            WorldApply = worldApply;
        if (messageApply != null)
            MessageApply = messageApply;
    }

    private static T Live<T>(T runtimeValue, T fallback) where T : class
    {
        return runtimeValue != null ? runtimeValue : fallback;
    }

    public static GameClient Game
    {
        get { return Live(_runtime != null ? _runtime.Game : null, _game != null ? _game : GameClient.Instance); }
    }

    public static LoginClient Login
    {
        get { return Live(_runtime != null ? _runtime.Login : null, _login != null ? _login : LoginClient.Instance); }
    }

    public static GameManager Manager
    {
        get { return _manager != null ? _manager : GameManager.Instance; }
    }

    public static PlayerInventory Inventory
    {
        get { return Live(_runtime != null ? _runtime.Inventory : null, PlayerInventory.Instance); }
    }

    public static TargetManager Targets
    {
        get { return Live(_runtime != null ? _runtime.Targets : null, TargetManager.Instance); }
    }

    public static PlayerController Player
    {
        get { return Live(_runtime != null ? _runtime.Player : null, PlayerController.Instance); }
    }

    public static ClickManager Clicks
    {
        get { return Live(_runtime != null ? _runtime.Clicks : null, ClickManager.Instance); }
    }

    public static PlayerShortcuts Shortcuts
    {
        get { return Live(_runtime != null ? _runtime.Shortcuts : null, PlayerShortcuts.Instance); }
    }

    public static PlayerActions Actions
    {
        get { return Live(_runtime != null ? _runtime.Actions : null, PlayerActions.Instance); }
    }

    public static InputManager Input
    {
        get { return Live(_runtime != null ? _runtime.Input : null, InputManager.Instance); }
    }

    public static PositionValidationController PositionValidation
    {
        get { return Live(_runtime != null ? _runtime.PositionValidation : null, PositionValidationController.Instance); }
    }

    public static EffectManager Effects
    {
        get { return Live(_runtime != null ? _runtime.Effects : null, EffectManager.Instance); }
    }

    public static EventBus Bus
    {
        get { return Live(_runtime != null ? _runtime.Bus : null, EventBus.Instance); }
    }

    public static CameraController Camera
    {
        get { return Live(_runtime != null ? _runtime.Camera : null, CameraController.Instance); }
    }

    public static IMoveAllCharacters Moves
    {
        get { return Live(_runtime != null ? _runtime.Moves : null, MoveAllCharacters.Instance); }
    }

    public static World GameWorld
    {
        get { return _world != null ? _world : (_runtime != null ? _runtime.World : World.Instance); }
    }

    public static IAnimationManager Animations
    {
        get { return _animations != null ? _animations : (_runtime != null ? _runtime.Animations : AnimationManager.Instance); }
    }

    public static EffectSkillsmanager EffectSkills
    {
        get { return Live(_runtime != null ? _runtime.EffectSkills : null, EffectSkillsmanager.Instance); }
    }

    public static SkillExecutor SkillRunner
    {
        get { return Live(_runtime != null ? _runtime.SkillRunner : null, SkillExecutor.Instance); }
    }

    public static SkillsManager SkillCombos
    {
        get { return Live(_runtime != null ? _runtime.SkillCombos : null, SkillsManager.Instance); }
    }

    public static L2LoginUI LoginUi
    {
        get { return _loginRuntime != null && _loginRuntime.Ui != null ? _loginRuntime.Ui : L2LoginUI.Instance; }
    }

    public static LoginWindow LoginWindow
    {
        get { return _loginRuntime != null && _loginRuntime.LoginWindow != null ? _loginRuntime.LoginWindow : LoginWindow.Instance; }
    }

    public static LicenseWindow LicenseWindow
    {
        get { return _loginRuntime != null && _loginRuntime.License != null ? _loginRuntime.License : LicenseWindow.Instance; }
    }

    public static ServerSelectWindow ServerSelect
    {
        get { return _loginRuntime != null && _loginRuntime.Servers != null ? _loginRuntime.Servers : ServerSelectWindow.Instance; }
    }

    public static CharSelectWindow CharSelect
    {
        get { return _loginRuntime != null && _loginRuntime.CharSelect != null ? _loginRuntime.CharSelect : CharSelectWindow.Instance; }
    }

    public static CharCreationWindow CharCreate
    {
        get { return _loginRuntime != null && _loginRuntime.CharCreate != null ? _loginRuntime.CharCreate : CharCreationWindow.Instance; }
    }

    public static LoginCameraManager LoginCamera
    {
        get { return _loginRuntime != null && _loginRuntime.Camera != null ? _loginRuntime.Camera : LoginCameraManager.Instance; }
    }

    public static CharacterSelector Characters
    {
        get { return _loginRuntime != null && _loginRuntime.Selector != null ? _loginRuntime.Selector : CharacterSelector.Instance; }
    }

    public static CharacterCreator Creator
    {
        get { return _loginRuntime != null && _loginRuntime.Creator != null ? _loginRuntime.Creator : CharacterCreator.Instance; }
    }

    public static L2GameUI Ui
    {
        get { return Live(_runtime != null ? _runtime.Ui : null, L2GameUI.Instance); }
    }

    public static ChatWindow Chat
    {
        get { return Live(_runtime != null ? _runtime.Chat : null, ChatWindow.Instance); }
    }

    public static HtmlWindow Html
    {
        get { return Live(_runtime != null ? _runtime.Html : null, HtmlWindow.Instance); }
    }

    public static DealerWindow Dealer
    {
        get { return Live(_runtime != null ? _runtime.Dealer : null, DealerWindow.Instance); }
    }

    public static MultiSellWindow MultiSell
    {
        get { return Live(_runtime != null ? _runtime.MultiSell : null, MultiSellWindow.Instance); }
    }

    public static TradeWindow Trade
    {
        get { return Live(_runtime != null ? _runtime.Trade : null, TradeWindow.Instance); }
    }

    public static TradeRequestWindow TradeRequest
    {
        get { return Live(_runtime != null ? _runtime.TradeRequest : null, TradeRequestWindow.Instance); }
    }

    public static PartyInvitationWindow PartyInvite
    {
        get { return Live(_runtime != null ? _runtime.PartyInvite : null, PartyInvitationWindow.Instance); }
    }

    public static EnchantWindow Enchant
    {
        get { return Live(_runtime != null ? _runtime.Enchant : null, EnchantWindow.Instance); }
    }

    public static RecipeBookWindow RecipeBook
    {
        get { return Live(_runtime != null ? _runtime.RecipeBook : null, RecipeBookWindow.Instance); }
    }

    public static CraftingItemWindow Crafting
    {
        get { return Live(_runtime != null ? _runtime.Crafting : null, CraftingItemWindow.Instance); }
    }

    public static SkillLearnWindow SkillLearn
    {
        get { return Live(_runtime != null ? _runtime.SkillLearn : null, SkillLearnWindow.Instance); }
    }

    public static DescriptionSkillWindow SkillDesc
    {
        get { return Live(_runtime != null ? _runtime.SkillDesc : null, DescriptionSkillWindow.Instance); }
    }

    public static SkillListWindow SkillList
    {
        get { return Live(_runtime != null ? _runtime.SkillList : null, SkillListWindow.Instance); }
    }

    public static QuestWindow Quest
    {
        get { return Live(_runtime != null ? _runtime.Quest : null, QuestWindow.Instance); }
    }

    public static QuestListWindow QuestList
    {
        get { return Live(_runtime != null ? _runtime.QuestList : null, QuestListWindow.Instance); }
    }

    public static ClanWindow Clan
    {
        get { return Live(_runtime != null ? _runtime.Clan : null, ClanWindow.Instance); }
    }

    public static ShowListWindow ShowList
    {
        get { return Live(_runtime != null ? _runtime.ShowList : null, ShowListWindow.Instance); }
    }

    public static SeedInfoWindow SeedInfo
    {
        get { return Live(_runtime != null ? _runtime.SeedInfo : null, SeedInfoWindow.Instance); }
    }

    public static SellCropListWindow SellCrop
    {
        get { return Live(_runtime != null ? _runtime.SellCrop : null, SellCropListWindow.Instance); }
    }

    public static SkillbarWindow Skillbar
    {
        get { return Live(_runtime != null ? _runtime.Skillbar : null, SkillbarWindow.Instance); }
    }

    public static SystemMessageWindow SystemMessageUi
    {
        get { return Live(_runtime != null ? _runtime.SystemMessageUi : null, SystemMessageWindow.Instance); }
    }

    public static BufferPanel Buffer
    {
        get { return Live(_runtime != null ? _runtime.Buffer : null, BufferPanel.Instance); }
    }

    public static InventoryWindow InventoryUi
    {
        get { return Live(_runtime != null ? _runtime.InventoryUi : null, InventoryWindow.Instance); }
    }

    public static DeadWindow Dead
    {
        get { return Live(_runtime != null ? _runtime.Dead : null, DeadWindow.Instance); }
    }

    public static StatusWindow Status
    {
        get { return Live(_runtime != null ? _runtime.Status : null, StatusWindow.Instance); }
    }

    public static bool IsWorldSpawnReady()
    {
        GameManager manager = Manager;
        return manager != null && manager.WorldSpawnReady && GameWorld != null && WorldApply != null;
    }

    public static void ApplyWorld(Action<WorldPacketApply> apply)
    {
        if (WorldApply != null && apply != null)
            apply(WorldApply);
    }

    public static void QueueWorld(Action<WorldPacketApply> apply)
    {
        if (apply == null)
            return;
        Queue(() => ApplyWorld(apply));
    }

    public static void ApplyMessage(Action<MessagePacketApply> apply)
    {
        if (MessageApply != null && apply != null)
            apply(MessageApply);
    }

    public static void Queue(Action action)
    {
        if (_applyQueue != null)
        {
            _applyQueue.Queue(action);
            return;
        }

        EventProcessor processor = _events != null ? _events : (_runtime != null ? _runtime.Events : EventProcessor.Instance);
        if (processor != null)
            processor.QueueEvent(action);
        else
            action();
    }

    public static void QueueApply(Action action)
    {
        if (_applyQueue != null)
            _applyQueue.QueueApply(action);
        else
            Queue(action);
    }
}
