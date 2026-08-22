using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements.Experimental;
using VContainer;



public class World : MonoBehaviour, IWorldSpawnContext {
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject _playerPlaceholder;
    [SerializeField] private GameObject _userPlaceholder;
    [SerializeField] private GameObject _npcPlaceHolder;
    [SerializeField] private GameObject _monsterPlaceholder;

    [SerializeField] private GameObject _monstersContainer;
    [SerializeField] private GameObject _npcsContainer;
    [SerializeField] private GameObject _usersContainer;
    [SerializeField] private GameObject _droppedItemsContainer;

    [Inject] private EventProcessor _eventProcessor;
    [Inject] private Geodata _geodata;
    [Inject] private WorldCombat _worldCombat;
    [Inject] private ObjectPoolManager _objectPool;
    [Inject] private GravityNpc _gravityNpc;
    [Inject] private DeadManager _deadManager;
    [Inject] private ClickManager _clicks;
    [Inject] private CameraController _camera;
    [Inject] private IAnimationManager _animations;
    [Inject] private GameClient _gameClient;
    [Inject] private PlayerSpawner _playerSpawner;
    [Inject] private UserSpawner _userSpawner;
    [Inject] private NpcSpawner _npcSpawner;
    [Inject] private MonsterSpawner _monsterSpawner;
    [Inject] private NpcgrpTable _npcGrps;
    [Inject] private NpcNameTable _npcNames;
    [Inject] private ModelTable _models;
    [Inject] private CharacterInfoWindow _characterInfo;

    private CharacterInfoWindow CharacterInfo
    {
        get { return _characterInfo != null ? _characterInfo : CharacterInfoWindow.Instance; }
    }

    public IAnimationManager Animations
    {
        get { return IncomingPacketActions.Animations != null ? IncomingPacketActions.Animations : _animations; }
    }

    public Transform UsersContainer
    {
        get { return _usersContainer != null ? _usersContainer.transform : null; }
    }

    public Transform NpcsContainer
    {
        get { return _npcsContainer != null ? _npcsContainer.transform : null; }
    }

    public Transform MonstersContainer
    {
        get { return _monstersContainer != null ? _monstersContainer.transform : null; }
    }

    private Dictionary<int, Entity> _players = new Dictionary<int, Entity>();
    private Dictionary<int, Entity> _npcs = new Dictionary<int, Entity>();
    private Dictionary<int, Entity> _objects = new Dictionary<int, Entity>();
    private Dictionary<int, GameObject> _droppedItems = new Dictionary<int, GameObject>();

    private Dictionary<int , MonsterStateMachine> _msObjects = new Dictionary<int, MonsterStateMachine>();


    [Header("Layer Masks")]
    [SerializeField] private LayerMask _entityMask;
    [SerializeField] private LayerMask _entityClickAreaMask;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _clickThroughMask;
    [SerializeField] private LayerMask _groundMask;

    [SerializeField] private bool _offlineMode = false;

    public bool OfflineMode { get { return _offlineMode; } }
    public LayerMask GroundMask { get { return _groundMask; } }

    private static World _instance;
    public static World Instance { get { return _instance; } }

    private void Awake() {
        if (_instance == null) {
            _instance = this;
        } else if (_instance != this) {
            Destroy(this);
            return;
        }

        DiBootstrap.EnsureGameScope();
        if (_eventProcessor == null)
            _eventProcessor = EventProcessor.Instance;
        _playerPlaceholder = Resources.Load<GameObject>("Prefab/Player_FDarkElf");
        _userPlaceholder = Resources.Load<GameObject>("Prefab/User_FDarkElf");
        _npcPlaceHolder = Resources.Load<GameObject>("Prefab/Npc");
        _monsterPlaceholder = Resources.Load<GameObject>("Data/Animations/LineageMonsters/gremlin/gremlin_prefab");
        _npcsContainer = GameObject.Find("Npcs");
        _monstersContainer = GameObject.Find("Monsters");
        _usersContainer = GameObject.Find("Users");
        _droppedItemsContainer = GameObject.Find("DroppedItems");
    }

    void OnDestroy() {
        if (_deadManager != null)
            _deadManager.OnReadyToRemove -= RemoveObject;
        _instance = null;
    }

    void Start() {
        if (_deadManager != null)
            _deadManager.OnReadyToRemove += RemoveObject;
        UpdateMasks();
    }

    public void UpdateMasks() {
        NameplatesManager.Instance.SetMask(_entityMask);
        if (_geodata != null)
            _geodata.ObstacleMask = _obstacleMask;
        if (_clicks != null)
            _clicks.SetMasks(_entityClickAreaMask, _clickThroughMask);
        if (_camera != null)
            _camera.SetMask(_obstacleMask);
    }

    public void ClearEntities() {
        _objects.Clear();
        _players.Clear();
        _npcs.Clear();
        _droppedItems.Clear();
    }

    public void RemoveObject(int id) {
        if (RemoveDroppedItem(id)) {
            return;
        }

        Entity transform;
        if (_objects.TryGetValue(id, out transform)) {
            _players.Remove(id);
            _npcs.Remove(id);
            _objects.Remove(id);

            Destroy(transform.gameObject);

        }
    }

    public void SpawnPlayerInterlude(NetworkIdentityInterlude identity, PlayerStatusInterlude status, PlayerInterludeStats stats, PlayerInterludeAppearance appearance)
    {
        
        identity.SetPosY(GetGroundHeight(identity.Position));
 
        identity.EntityType = EntityType.Player;
 

        CharacterRace race = (CharacterRace)appearance.Race;
   
        CharacterRaceAnimation raceId = CharacterRaceAnimationParser.ParseRaceInterlude(race, appearance.Sex, appearance.BaseClass);
   

        GameObject go = CharacterBuilder.Instance.BuildCharacterBaseInterlude(raceId, appearance, identity.EntityType);
     
        go.transform.SetParent(_usersContainer.transform);
      
        //go.transform.eulerAngles = new Vector3(transform.eulerAngles.x, identity.Heading, transform.eulerAngles.z);

        go.transform.position = identity.Position;
       
        go.transform.rotation = identity.Heading;
     

        // go.transform.name = "_Player";
        go.transform.name = identity.Name;
   
        PlayerEntity player = go.GetComponent<PlayerEntity>();
 

        player.Status = status;
        player.IdentityInterlude = identity;
        player.Stats = stats;
        player.Appearance = appearance;
        player.Race = race;
        player.RaceId = raceId;
        player.Running = appearance.Running;
  
        player.SetDead(false);
 
        go.GetComponent<NetworkTransformShare>().enabled = true;
   
        go.GetComponent<PlayerController>().enabled = true;
   
        go.GetComponent<PlayerController>().Initialize();
   

        go.SetActive(true);
      

        go.GetComponentInChildren<PlayerAnimationController>().Initialize();
     
        PlayerAnimationController controller = go.GetComponentInChildren<PlayerAnimationController>();

       
        AnimationManager.Instance.RegisterController(identity.Id, controller , player);
   
        go.GetComponent<Gear>().Initialize(player.IdentityInterlude.Id, player.RaceId);
     
        var statsIntr = (PlayerInterludeStats)player.Stats;
       
        player.Initialize();



        player.UpdateRunSpeed(statsIntr.RunRealSpeed);
      
        player.UpdateWalkSpeed(statsIntr.WalkRealSpeed);
      

        //416 - пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅ
        //554 - пїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ
        player.UpdatePAtkSpeedPlayer((int)statsIntr.BasePAtkSpeed);
      
        player.UpdateMAtkSpeed((int)statsIntr.MAtkSpd);
     
        //go.transform.SetParent(_usersContainer.transform);

        CameraController.Instance.enabled = true;
      
        CameraController.Instance.SetTarget(go);
      
        CameraController.Instance.SetHeading(identity.OrigHeading);
    

        CharacterInfoWindow.Instance.UpdateValues();
    
        PlayerStateMachine.Instance.Player = player;
    
        _players.Add(identity.Id, player);

        _objects.Add(identity.Id, player);
    }

    /// <summary>
    /// Spawns another player entering our visibility range (CharInfo). Unlike
    /// SpawnPlayerInterlude (our own avatar - camera, input, PlayerStateMachine), this is purely
    /// network-driven, same shape as an NPC: no local control, movement/animation come from
    /// packets via NetworkAnimationController/NetworkCharacterControllerReceive.
    /// </summary>
    public void SpawnUserInterlude(NetworkIdentityInterlude identity, PlayerStatusInterlude status, PlayerInterludeStats stats, PlayerInterludeAppearance appearance)
    {
        if (_objects.TryGetValue(identity.Id, out Entity existingEntity))
        {
            // Same packet is re-sent when this player's visible equipment changes - not just on
            // first entering view range - so an already-spawned user gets a gear refresh instead
            // of being silently ignored.
            if (existingEntity is UserEntity existingUser)
            {
                existingUser.RefreshEquipment(appearance);
            }

            return;
        }

        identity.SetPosY(GetGroundHeight(identity.Position));
        identity.EntityType = EntityType.User;

        CharacterRace race = (CharacterRace)appearance.Race;
        CharacterRaceAnimation raceId = CharacterRaceAnimationParser.ParseRaceInterlude(race, appearance.Sex, appearance.BaseClass);

        GameObject go = CharacterBuilder.Instance.BuildCharacterBaseInterlude(raceId, appearance, identity.EntityType);
        if (go == null)
        {
            Debug.LogWarning($"SpawnUserInterlude - could not build character model for {identity.Name} ({identity.Id})");
            return;
        }

        go.transform.SetParent(_usersContainer.transform);
        go.transform.position = identity.Position;
        go.transform.rotation = identity.Heading;
        go.transform.name = identity.Name;

        UserEntity user = go.GetComponent<UserEntity>();
        user.Status = status;
        user.IdentityInterlude = identity;
        user.Stats = stats;
        user.Appearance = appearance;
        user.Race = race;
        user.RaceId = raceId;

        user.SetDead(false);

        go.SetActive(true);

        var animationController = user.GetComponent<NetworkAnimationController>();
        animationController.Initialize();

        go.GetComponent<Gear>().Initialize(user.IdentityInterlude.Id, user.RaceId);
        user.Initialize();

        AnimationManager.Instance.RegisterController(identity.Id, animationController, user);
        user.UpdateNpcPAtkSpd((int)stats.PAtkSpd);
        user.UpdateRunSpeed(stats.RunRealSpeed);
        user.UpdateWalkSpeed(stats.WalkRealSpeed);
        user.Running = identity.IsRunning;

        // The local player gets kicked into "wait_<weapon>" on ENTER_WORLD (NewIdleState), which is
        // what puts its Animator into the state the UserState* StateMachineBehaviours are attached
        // to (Wait, then auto-switching to Run via IsMoving()). Nothing does that for other
        // players, so without this their Animator never leaves its default entry state.
        AnimationManager.Instance.PlayMonsterAnimation(identity.Id, AnimationNames.WAIT.ToString() + user.Gear.WeaponAnim);

        _players.Add(identity.Id, user);
        _objects.Add(identity.Id, user);
    }


    bool isSinglSpawn = false;

    /// <param name="itemObjectId">World id of the dropped item, not the item template id.</param>
    public void PickupItemFromTheGround(int itemObjectId, int playerId, Vector3 position)
    {
        RemoveDroppedItem(itemObjectId);
    }

    /// <summary>
    /// Closest dropped item to fromPosition within maxDistance, or null - used by the "Pick Up"
    /// action (/pickup) to grab whatever is lying around without having to click it directly.
    /// </summary>
    public GameObject GetNearestDroppedItem(Vector3 fromPosition, float maxDistance)
    {
        GameObject nearest = null;
        float nearestDistance = maxDistance;

        foreach (GameObject itemGo in _droppedItems.Values)
        {
            if (itemGo == null)
            {
                continue;
            }

            float distance = Vector3.Distance(fromPosition, itemGo.transform.position);
            if (distance <= nearestDistance)
            {
                nearest = itemGo;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    /// <param name="dropperId">Object id of the character that dropped the item.</param>
    /// <param name="itemObjectId">World id of the dropped item, unique per drop.</param>
    /// <param name="itemId">Item template id, the one the dat tables are keyed by.</param>
    public void DropItemOnTheGround(int dropperId, int itemObjectId, int itemId, Vector3 position, int count, bool stackable = false)
    {
        if (_droppedItems.ContainsKey(itemObjectId))
        {
            return;
        }

        try
        {
            position.y = GetDroppedItemGroundHeight(position);

            GameObject itemGo = DroppedItemFactory.Create(itemId, position, GetDroppedItemsContainer());
            itemGo.transform.name = DroppedItemFactory.GetItemName(itemId);

            DroppedItemEntity droppedItem = itemGo.AddComponent<DroppedItemEntity>();
            droppedItem.Initialize(itemObjectId, BuildDropTooltip(itemId, count, stackable), count, DroppedItemFactory.GetVisualHeight(itemGo));

            itemGo.SetActive(true);

            _droppedItems.Add(itemObjectId, itemGo);
        }
        catch (Exception e)
        {
            Debug.LogError($"DropItemOnTheGround - Can't spawn item {itemId} (object {itemObjectId}) - {e}");
        }
    }

    private string BuildDropTooltip(int itemId, int count, bool stackable)
    {
        StringBuilder sb = new StringBuilder(DroppedItemFactory.GetItemName(itemId));
        if (stackable && count > 1)
        {
            sb.Append(" (");
            sb.Append(count);
            sb.Append(")");
        }

        return sb.ToString();
    }

    private Transform GetDroppedItemsContainer()
    {
        if (_droppedItemsContainer == null)
        {
            _droppedItemsContainer = GameObject.Find("DroppedItems") ?? new GameObject("DroppedItems");
        }

        return _droppedItemsContainer.transform;
    }

    private bool RemoveDroppedItem(int itemObjectId)
    {
        if (!_droppedItems.TryGetValue(itemObjectId, out GameObject itemGo))
        {
            return false;
        }

        _droppedItems.Remove(itemObjectId);

        if (itemGo != null)
        {
            Destroy(itemGo);
        }

        return true;
    }

    public void SpawnNpcInterlude(NetworkIdentityInterlude identity, NpcStatusInterlude status, Stats stats)
    {



        if (_npcs.ContainsKey(identity.Id)) return;

        MonsterStateMachine msm = null;
        Npcgrp npcgrp = NpcgrpTable.Instance.GetNpcgrp(identity.NpcId);
        NpcName npcName = NpcNameTable.Instance.GetNpcName(identity.NpcId);


        if (npcName == null || npcgrp == null)
        {
            Debug.LogWarning($"[DeleteObject] RemoveObject MISS dict id={id} (already gone or never spawned)");
            return;
        }

        string goName = entity != null ? entity.name : "null";
        string typeName = entity != null ? entity.GetType().Name : "null";
        GameObject go = entity != null ? entity.gameObject : null;
        bool wasActive = go != null && go.activeSelf;
        string parentBefore = go != null && go.transform.parent != null
            ? go.transform.parent.name
            : "null";

        Debug.Log(
            $"[DeleteObject] RemoveObject START id={id} type={typeName} name={goName} " +
            $"activeSelf={wasActive} parent={parentBefore}");

        _players.Remove(id);
        _npcs.Remove(id);
        _objects.Remove(id);
        if (EntityActionMachine.Instance != null)
            EntityActionMachine.Instance.Remove(entity);

        if (_gravityNpc != null)
        {
            _gravityNpc.DeleteGravity(id);
        }

        if (Animations != null)
            Animations.UnregisterController(id);

        if (go == null)
        {
            Debug.LogWarning($"[DeleteObject] RemoveObject id={id} entity.gameObject is null");
            return;
        }

        // City NPC + field monsters → pool. If pool fails or leaves object visible → Destroy.
        if (_objectPool != null &&
            (entity is NpcEntity || entity is MonsterEntity))
        {
            ObjectType poolType = entity is MonsterEntity ? ObjectType.Monster : ObjectType.Npc;
            bool returned = _objectPool.ReturnToPool(poolType, go);
            bool stillVisible = go != null && go.activeInHierarchy;
            Debug.Log(
                $"[DeleteObject] RemoveObject {poolType} id={id} returned={returned} " +
                $"activeSelf={(go != null && go.activeSelf)} " +
                $"activeInHierarchy={stillVisible} " +
                $"parent={(go != null && go.transform.parent != null ? go.transform.parent.name : "null")}");

            if (returned && !stillVisible)
            {
                return;
            }

            Debug.LogWarning(
                $"[DeleteObject] RemoveObject {poolType} id={id} pool did not hide → Destroy " +
                $"(returned={returned} stillVisible={stillVisible})");
        }

        Destroy(go);
        Debug.Log($"[DeleteObject] RemoveObject DESTROY id={id} name={goName}");
    }

    public bool ContainsNpc(int id)
    {
        return _npcs.ContainsKey(id);
    }

    public void RegisterPlayer(PlayerEntity player)
    {
        if (player == null || player.Identity == null)
            return;

        int id = player.Identity.Id;
        _players[id] = player;
        _objects[id] = player;
    }

    public void RegisterUser(Entity user)
    {
        if (user == null || user.Identity == null)
            return;

        int id = user.Identity.Id;
        if (_objects.ContainsKey(id))
            return;

        _players[id] = user;
        _objects[id] = user;
    }

    public void SpawnUser(CharInfoDto info)
    {
        if (info == null || info.Identity == null || _userSpawner == null)
        {
            GearFlowLog.Warn("SpawnUser abort info/spawner null");
            return;
        }

        int id = info.Identity.Id;
        if (_objects.ContainsKey(id))
        {
            GearFlowLog.Info("SpawnUser SKIP already in world id=" + id +
                " type=" + _objects[id].GetType().Name);
            return;
        }

        PlayerEntity local = PlayerEntity.Instance;
        if (local != null && local.Identity != null && local.Identity.Id == id)
        {
            GearFlowLog.Info("SpawnUser SKIP local PlayerEntity id=" + id);
            return;
        }

        GearFlowLog.Info("SpawnUser CREATE UserEntity id=" + id +
            " nick=" + info.Identity.Name + " " + GearFlowLog.Paperdoll(info.Appearance));
        _userSpawner.Spawn(info, this);
    }

    public void UpdateUser(Entity entity, CharInfoDto info)
    {
        if (_userSpawner != null)
            _userSpawner.UpdateInfo(entity, info);
    }

    public void RegisterNpc(Entity npc)
    {
        if (npc == null || npc.Identity == null)
            return;

        int id = npc.Identity.Id;
        _npcs.Add(id, npc);
        _objects.Add(id, npc);
    }

    public void SpawnPlayer(EntityIdentity identity, PlayerStatus status, PlayerStats stats, PlayerAppearance appearance)
    {
        if (_playerSpawner != null)
            _playerSpawner.Spawn(identity, status, stats, appearance, this);
    }

    public void SpawnNpc(EntityIdentity identity, NpcStatusInterlude status, Stats stats)
    {
        if (identity == null || ContainsNpc(identity.Id))
            return;

        if (identity.NpcId == 20481)
            Debug.Log(" object NpcInfo 5 " + identity.Id);

        NpcSpawnRequest request;
        if (!EntitySpawnShared.TryResolveNpc(identity, _npcGrps, _npcNames, _models, out request))
            return;

        request.Status = status;
        request.Stats = stats;
        Debug.Log("Name NPC " + request.NpcName.Name);

        if (identity.EntityType == EntityType.NPC)
        {
            if (_npcSpawner != null)
                _npcSpawner.Spawn(request, this);
        }
        else if (_monsterSpawner != null)
        {
            _monsterSpawner.Spawn(request, this);
        }
    }

    public void UpdateNpc(Entity entity , NpcInfoDto npcInfo)
    {
        if (_monsterSpawner != null)
            _monsterSpawner.UpdateInfo(entity, npcInfo);
    }

    public void UpdateUserInfo(Entity entity, UserInfoDto userInfo)
    {
        if (_playerSpawner != null)
            _playerSpawner.UpdateInfo(entity, userInfo);
    }

    public async Task DeleteObject(int objectId)
    {
        // Items on the ground are not entities, the server despawns them with the same packet.
        if (RemoveDroppedItem(objectId))
        {
            return;
        }

        Entity entity = await GetEntityNoLock(objectId);
        if (entity == null)
        {
            Debug.LogWarning(
                $"[DeleteObject] HANDLER id={objectId} → entity NOT in World._objects " +
                "(packet arrived but client has no entity — GO may be orphaned in Hierarchy)");
            return;
        }

        string typeName = entity.GetType().Name;
        Debug.Log(
            $"[DeleteObject] HANDLER id={objectId} found type={typeName} name={entity.name} " +
            $"activeInHierarchy={entity.gameObject.activeInHierarchy}");

        if (entity.GetType() == typeof(MonsterEntity))
        {
            if (entity.IsDead())
            {
                if (_deadManager != null)
                    _deadManager.AddDeadAndRemove(objectId, new DeadData(entity));
                Debug.Log($"[DeleteObject] HANDLER id={objectId} Monster DEAD → DeadManager");
            }
            else
            {
                RemoveObject(objectId);
                Debug.Log($"[DeleteObject] HANDLER id={objectId} Monster → RemoveObject");
            }
        }
        else if (entity is NpcEntity)
        {
            if (_deadManager != null)
            {
                _deadManager.AddDeadAndRemove(objectId, new DeadData(entity));
                Debug.Log($"[DeleteObject] HANDLER id={objectId} NpcEntity → DeadManager");
            }
            else
            {
                RemoveObject(objectId);
                Debug.Log($"[DeleteObject] HANDLER id={objectId} NpcEntity → RemoveObject done");
            }
        }
        else
        {
            // Same as historical catch-all: anything else still must leave the world.
            Debug.LogWarning(
                $"[DeleteObject] HANDLER id={objectId} unexpected type={typeName} → RemoveObject");
            RemoveObject(objectId);
        }
    }

    public float GetGroundHeight(Vector3 pos) {
        return GroundSnapHelper.SnapToGroundOrKeep(pos, _groundMask).y;
    }

    public string GetEntityName(int id)
    {
        if (_npcs.ContainsKey(id))
        {
            return _npcs[id].name;
        }else if (_players.ContainsKey(id))
        {
            return _players[id].name;
        }
        return "";
    }

    public async Task UpdateObjectPosition(int id, Vector3 position) {
            Entity entity = await GetEntityNoLock(id);

            if(entity != null)
            {
                entity.transform.position = position;
            }
            else
            {

                if(entity != null)
                {
                    entity.transform.position = position;
                    Debug.Log("UpdateObjectPosition not found obj. Name " + entity.name);
                }

            }
            
        //});
    }



    public Task TeleportTo(int id, Vector3 position)
    {
        return ExecuteWithEntityAsync(id, entity => {
            if (entity.GetType() == typeof(PlayerEntity))
            {
                PlayerTeleport teleport = entity.GetComponent<PlayerTeleport>();
                teleport.TeleportTo(position);
                Vector3 grounded = teleport.LastTeleportPosition;
                // Match original client after TeleportToLocationDto / L2_Teleport:
                // ValidatePosition then Appearing → server onTeleported() + UserInfoDto / knownlist.
                SendValidatePosition(grounded);
                SendAppearing();
            }
        });
    }

    private void SendValidatePosition(Vector3 position)
    {
        GameClient game = _gameClient != null ? _gameClient : IncomingPacketActions.Game;
        if (game != null)
            game.Send(new ValidatePositionCommand(position.x, position.y, position.z));
    }

    private void SendAppearing()
    {
        GameClient game = _gameClient != null ? _gameClient : IncomingPacketActions.Game;
        if (game != null)
            game.Send(new AppearingCommand());
    }



    public Task UpdateObjectRotation(int id, float angle) {
        return ExecuteWithEntityAsync(id, e => {
            e.GetComponent<NetworkTransformReceive>().SetFinalRotation(angle);
        });
    }

    public Task UpdateObjectDestination(int id, Vector3 position, int speed, bool walking) {
        return ExecuteWithEntityAsync(id, e => {
            if (speed != e.Stats.Speed) {
                e.UpdateSpeed(speed);
            }

            NetworkTransformReceive ntr = e.GetComponent<NetworkTransformReceive>();
            if (ntr != null)
            {
                ntr.LookAt(position);
            }

            e.OnStartMoving(walking);
        });
    }

    public Task UpdateObjectAnimation(int id, int animId, float value) {
        return ExecuteWithEntityAsync(id, e => {
            e.GetComponent<NetworkAnimationController>().SetAnimationProperty(animId, value);
        });
    }

    public Task InflictDamageTo(int sender, int target, int damage, bool criticalHit) {
        return ExecuteWithEntitiesAsync(sender, target, (senderEntity, targetEntity) => {
            if (senderEntity != null) {
                //WorldCombat.Instance.InflictAttack(senderEntity.transform, targetEntity.transform, damage, criticalHit);
            } else {
                if (_worldCombat != null)
                    _worldCombat.InflictAttack(targetEntity.transform, damage, criticalHit);
            }
        });
    }

    public Task UpdateObjectMoveDirection(int id, int speed, Vector3 direction) {
        return ExecuteWithEntityAsync(id, e => {
            if (speed != e.Stats.Speed) {
                e.UpdateSpeed(speed);
            }
            // Movement direction applied via MoveAllCharacters / CharMoveToLocationDto.
        });
    }

    public Task UpdateEntityTarget(int id, int targetId) {
        return ExecuteWithEntitiesAsync(id, targetId, (targeter, targeted) => {
            targeter.TargetId = targetId;
            targeter.Target = targeted.transform;
        });
    }


    public Task StatusUpdate(int id, List<StatusUpdate.Attribute> attributes) {
        return ExecuteWithEntityAsync(id, e => {
            if(_worldCombat != null)
            {
                _worldCombat.StatusUpdate(e, attributes, id);
                if (e.GetType() == typeof(PlayerEntity))
                {
                    if (CharacterInfo != null)
                        CharacterInfo.UpdateValues();
                }
            }

        });
    }

    public Task UserInfoUpdateCharacter(UserInfoDto user)
    {
        return ExecuteWithEntityAsync(user.PlayerInfoInterlude.Identity.Id, e => {
            if (_worldCombat != null)
                _worldCombat.StatusUpdate(e, user.PlayerInfoInterlude.Stats, user.PlayerInfoInterlude.Status , user.PlayerInfoInterlude.Identity.Id);
            if (e == PlayerEntity.Instance)
            {
                PlayerEntity.Instance.Running = user.PlayerInfoInterlude.Appearance.Running;
                if (CharacterInfo != null)
                    CharacterInfo.UpdateValues();
            }
        });
    }



  

    public async Task Revive(int dieObj)
    {
        Entity entity = await GetEntityNoLock(dieObj);

        if (entity != null)
        {
            if (entity.GetType() == typeof(PlayerEntity))
            {

                PlayerStateMachine.Instance.ChangeState(PlayerState.REBIRTH);
                PlayerStateMachine.Instance.NotifyEvent(Event.REBIRTH);

                entity.SetDead(false);

            }
   
        }
    }

    public async Task<Entity> GetEntityNoLock(int id)
    {
        if (_objects.ContainsKey(id)){
            return _objects[id];
        }
        return null;
    }

    public Entity GetEntityNoLockSync(int id)
    {
        if (_objects.ContainsKey(id))
        {
            return _objects[id];
        }
        return null;
    }

    public void ForEachEntity(Action<Entity> action)
    {
        if (action == null)
            return;

        foreach (KeyValuePair<int, Entity> pair in _objects)
        {
            if (pair.Value != null)
                action(pair.Value);
        }
    }



    // Wait for entity to be fully loaded
    public async Task<Entity> GetEntityAsync(int id) {
        Entity entity;
        lock (_objects) {
            if (!_objects.TryGetValue(id, out entity)) {
                //Debug.LogWarning($"GetEntityAsync - Entity {id} not found, retrying...");
            }
        }

        if (entity == null) {
            await Task.Delay(150); // Wait for 150 ms retrying

            lock (_objects) {
                if (!_objects.TryGetValue(id, out entity)) {
                    Debug.LogWarning($"GetEntityAsync - Entity {id} not found after retry");
                    return null;
                } else {
                   // Debug.LogWarning($"GetEntityAsync - Entity {id} found after retry");
                }
            }
        }

        return entity;
    }

    // Execute action after entity is loaded
    private async Task ExecuteWithEntityAsync(int id, Action<Entity> action) {
        var entity = await GetEntityAsync(id);
        if (entity != null) {
            try {
                _eventProcessor.QueueEvent(() => action(entity));
            } catch (Exception ex) {
                Debug.LogWarning($"Operation failed - Target {id} - Error {ex.Message}");
            }
        }
    }

    // Execute action after 2 entities are loaded
    private async Task ExecuteWithEntitiesAsync(int id1, int id2, Action<Entity, Entity> action) {
        var entity1Task = GetEntityAsync(id1);
        var entity2Task = GetEntityAsync(id2);

        await Task.WhenAll(entity1Task, entity2Task);

        var entity1 = await entity1Task;
        var entity2 = await entity2Task;

        if (entity1 != null && entity2 != null) {
            try {
                _eventProcessor.QueueEvent(() => action(entity1, entity2));
            } catch (Exception ex) {
                Debug.LogWarning($"Operation failed - Target {id1} or {id2} - Error {ex.Message}");
            }
        }
    }
}
