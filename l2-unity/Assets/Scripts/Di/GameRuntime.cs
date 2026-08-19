public sealed class GameRuntime
{
    private PlayerController _player;
    private PlayerActions _actions;
    private World _world;
    private PlayerInventory _inventory;
    private TargetManager _targets;
    private HitManager _hits;
    private ClickManager _clicks;
    private PlayerShortcuts _shortcuts;
    private InputManager _input;
    private PositionValidationController _positionValidation;
    private EffectManager _effects;
    private EventBus _bus;
    private CameraController _camera;
    private IMoveAllCharacters _moves;
    private CombatFacingService _facing;
    private IAnimationManager _animations;
    private EffectSkillsmanager _effectSkills;
    private SkillExecutor _skillRunner;
    private SkillsManager _skillCombos;
    private L2GameUI _ui;
    private ChatWindow _chat;
    private HtmlWindow _html;
    private DealerWindow _dealer;
    private MultiSellWindow _multiSell;
    private TradeWindow _trade;
    private TradeRequestWindow _tradeRequest;
    private PartyInvitationWindow _partyInvite;
    private EnchantWindow _enchant;
    private RecipeBookWindow _recipeBook;
    private CraftingItemWindow _crafting;
    private SkillLearnWindow _skillLearn;
    private DescriptionSkillWindow _skillDesc;
    private SkillListWindow _skillList;
    private QuestWindow _quest;
    private QuestListWindow _questList;
    private ClanWindow _clan;
    private ShowListWindow _showList;
    private SeedInfoWindow _seedInfo;
    private SellCropListWindow _sellCrop;
    private SkillbarWindow _skillbar;
    private SystemMessageWindow _systemMessageUi;
    private BufferPanel _buffer;
    private InventoryWindow _inventoryUi;
    private DeadWindow _dead;
    private StatusWindow _status;

    public EventProcessor Events { get; }
    public GameClient Game { get; }
    public LoginClient Login { get; }
    public GameManager Manager { get; }
    public World World { get { return Live(ref _world, global::World.Instance); } }
    public PlayerInventory Inventory { get { return Live(ref _inventory, PlayerInventory.Instance); } }
    public TargetManager Targets { get { return Live(ref _targets, TargetManager.Instance); } }
    public HitManager Hits { get { return Live(ref _hits, HitManager.Instance); } }
    public ClickManager Clicks { get { return Live(ref _clicks, ClickManager.Instance); } }
    public PlayerShortcuts Shortcuts { get { return Live(ref _shortcuts, PlayerShortcuts.Instance); } }
    public InputManager Input { get { return Live(ref _input, InputManager.Instance); } }
    public PositionValidationController PositionValidation { get { return Live(ref _positionValidation, PositionValidationController.Instance); } }
    public EffectManager Effects { get { return Live(ref _effects, EffectManager.Instance); } }
    public EventBus Bus { get { return Live(ref _bus, EventBus.Instance); } }
    public CameraController Camera { get { return Live(ref _camera, CameraController.Instance); } }
    public IMoveAllCharacters Moves { get { return Live(ref _moves, MoveAllCharacters.Instance); } }
    public CombatFacingService Facing { get { return Live(ref _facing, CombatFacingService.Instance); } }
    public IAnimationManager Animations { get { return Live(ref _animations, AnimationManager.Instance); } }
    public EffectSkillsmanager EffectSkills { get { return Live(ref _effectSkills, EffectSkillsmanager.Instance); } }
    public SkillExecutor SkillRunner { get { return Live(ref _skillRunner, SkillExecutor.Instance); } }
    public SkillsManager SkillCombos { get { return Live(ref _skillCombos, SkillsManager.Instance); } }

    public L2GameUI Ui { get { return Live(ref _ui, L2GameUI.Instance); } }
    public ChatWindow Chat { get { return Live(ref _chat, ChatWindow.Instance); } }
    public HtmlWindow Html { get { return Live(ref _html, HtmlWindow.Instance); } }
    public DealerWindow Dealer { get { return Live(ref _dealer, DealerWindow.Instance); } }
    public MultiSellWindow MultiSell { get { return Live(ref _multiSell, MultiSellWindow.Instance); } }
    public TradeWindow Trade { get { return Live(ref _trade, TradeWindow.Instance); } }
    public TradeRequestWindow TradeRequest { get { return Live(ref _tradeRequest, TradeRequestWindow.Instance); } }
    public PartyInvitationWindow PartyInvite { get { return Live(ref _partyInvite, PartyInvitationWindow.Instance); } }
    public EnchantWindow Enchant { get { return Live(ref _enchant, EnchantWindow.Instance); } }
    public RecipeBookWindow RecipeBook { get { return Live(ref _recipeBook, RecipeBookWindow.Instance); } }
    public CraftingItemWindow Crafting { get { return Live(ref _crafting, CraftingItemWindow.Instance); } }
    public SkillLearnWindow SkillLearn { get { return Live(ref _skillLearn, SkillLearnWindow.Instance); } }
    public DescriptionSkillWindow SkillDesc { get { return Live(ref _skillDesc, DescriptionSkillWindow.Instance); } }
    public SkillListWindow SkillList { get { return Live(ref _skillList, SkillListWindow.Instance); } }
    public QuestWindow Quest { get { return Live(ref _quest, QuestWindow.Instance); } }
    public QuestListWindow QuestList { get { return Live(ref _questList, QuestListWindow.Instance); } }
    public ClanWindow Clan { get { return Live(ref _clan, ClanWindow.Instance); } }
    public ShowListWindow ShowList { get { return Live(ref _showList, ShowListWindow.Instance); } }
    public SeedInfoWindow SeedInfo { get { return Live(ref _seedInfo, SeedInfoWindow.Instance); } }
    public SellCropListWindow SellCrop { get { return Live(ref _sellCrop, SellCropListWindow.Instance); } }
    public SkillbarWindow Skillbar { get { return Live(ref _skillbar, SkillbarWindow.Instance); } }
    public SystemMessageWindow SystemMessageUi { get { return Live(ref _systemMessageUi, SystemMessageWindow.Instance); } }
    public BufferPanel Buffer { get { return Live(ref _buffer, BufferPanel.Instance); } }
    public InventoryWindow InventoryUi { get { return Live(ref _inventoryUi, InventoryWindow.Instance); } }
    public DeadWindow Dead { get { return Live(ref _dead, DeadWindow.Instance); } }
    public StatusWindow Status { get { return Live(ref _status, StatusWindow.Instance); } }
    public WorldPacketApply WorldApply { get; private set; }
    public MessagePacketApply MessageApply { get; private set; }

    public PlayerController Player
    {
        get { return _player != null ? _player : (_player = PlayerController.Instance); }
    }

    public PlayerActions Actions
    {
        get { return _actions != null ? _actions : (_actions = PlayerActions.Instance); }
    }

    public GameRuntime()
    {
        Events = EventProcessor.Instance;
        Game = GameClient.Instance;
        Login = LoginClient.Instance;
        Manager = GameManager.Instance;
    }

    public void BindApply(WorldPacketApply worldApply, MessagePacketApply messageApply)
    {
        if (worldApply != null)
            WorldApply = worldApply;
        if (messageApply != null)
            MessageApply = messageApply;
    }

    private static T Live<T>(ref T field, T instance) where T : class
    {
        if (field == null)
            field = instance;
        return field;
    }
}
