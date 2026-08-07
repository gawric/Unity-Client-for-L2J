using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UIElements.Experimental;



public class World : MonoBehaviour {
    [SerializeField] private GameObject _player;
    [SerializeField] private GameObject _playerPlaceholder;
    [SerializeField] private GameObject _userPlaceholder;
    [SerializeField] private GameObject _npcPlaceHolder;
    [SerializeField] private GameObject _monsterPlaceholder;

    [SerializeField] private GameObject _monstersContainer;
    [SerializeField] private GameObject _npcsContainer;
    [SerializeField] private GameObject _usersContainer;

    private EventProcessor _eventProcessor;

    private Dictionary<int, Entity> _players = new Dictionary<int, Entity>();
    private Dictionary<int, Entity> _npcs = new Dictionary<int, Entity>();
    private Dictionary<int, Entity> _objects = new Dictionary<int, Entity>();
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
        }

        _eventProcessor = EventProcessor.Instance;
        _playerPlaceholder = Resources.Load<GameObject>("Prefab/Player_FDarkElf");
        _userPlaceholder = Resources.Load<GameObject>("Prefab/User_FDarkElf");
        _npcPlaceHolder = Resources.Load<GameObject>("Prefab/Npc");
        _monsterPlaceholder = Resources.Load<GameObject>("Data/Animations/LineageMonsters/gremlin/gremlin_prefab");
        _npcsContainer = GameObject.Find("Npcs");
        _monstersContainer = GameObject.Find("Monsters");
        _usersContainer = GameObject.Find("Users");
    }

    void OnDestroy() {
        _instance = null;
    }

    void Start() {
        UpdateMasks();
    }

    public void UpdateMasks() {
        NameplatesManager.Instance.SetMask(_entityMask);
        Geodata.Instance.ObstacleMask = _obstacleMask;
        ClickManager.Instance.SetMasks(_entityClickAreaMask, _clickThroughMask);
        CameraController.Instance.SetMask(_obstacleMask);
    }

    public void ClearEntities() {
        _objects.Clear();
        _players.Clear();
        _npcs.Clear();
    }

    public void RemoveObject(int id) {
        Entity entity;
        if (!_objects.TryGetValue(id, out entity))
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
        _msObjects.Remove(id);

        if (GravityNpc.Instance != null)
        {
            GravityNpc.Instance.DeleteGravity(id);
        }

        if (AnimationManager.Instance != null)
        {
            AnimationManager.Instance.UnregisterController(id);
        }

        if (go == null)
        {
            Debug.LogWarning($"[DeleteObject] RemoveObject id={id} entity.gameObject is null");
            return;
        }

        // City NPC + field monsters → pool. If pool fails or leaves object visible → Destroy.
        if (ObjectPoolManager.Instance != null &&
            (entity is NpcEntity || entity is MonsterEntity))
        {
            ObjectType poolType = entity is MonsterEntity ? ObjectType.Monster : ObjectType.Npc;
            bool returned = ObjectPoolManager.Instance.ReturnToPool(poolType, go);
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


    bool isSinglSpawn = false;

    public void SpawnNpcInterlude(NetworkIdentityInterlude identity, NpcStatusInterlude status, Stats stats)
    {



        if (_npcs.ContainsKey(identity.Id)) return;

        MonsterStateMachine msm = null;
        Npcgrp npcgrp = NpcgrpTable.Instance.GetNpcgrp(identity.NpcId);
        NpcName npcName = NpcNameTable.Instance.GetNpcName(identity.NpcId);


        if (npcName == null || npcgrp == null)
        {
            Debug.LogError($"Npc {identity.NpcId} could not be loaded correctly.");
            return;
        }

        if (identity.NpcId == 20481)
        {
            Debug.Log(" object NpcInfo 5 " + identity.Id);
        }

        GameObject go = ModelTable.Instance.GetNpc(npcgrp.Mesh);


        if (go != null)
        {

            Debug.Log("Name NPC " + npcName.Name);
            //Debug пїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ 1 пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅ пїЅпїЅпїЅ !!!!
           //if (isSinglSpawn | !npcName.Name.Equals("Elder Keltir")) return;

           // if (!isSinglSpawn)
            //{
              //  isSinglSpawn = true;
                
               // if (identity.EntityType == EntityType.NPC)
               // {
                //    return;
               //}
            //}
            
            identity.SetPosY(GetGroundHeight(identity.Position));

            identity.EntityType = EntityTypeParser.ParseEntityType(npcgrp.ClassName);
            ChangeEntityType(identity);

            GameObject npcGo = AcquireNpcGameObject(go, identity);

            NpcData npcData = new NpcData(npcName, npcgrp);

            Entity npc;

            if (identity.EntityType == EntityType.NPC)
            {
                npcGo.transform.SetParent(_npcsContainer.transform);
                npc = npcGo.GetComponent<NpcEntity>();
                ((NpcEntity)npc).NpcData = npcData;
            }
            else
            {
                npcGo.transform.SetParent(_monstersContainer.transform);
                npc = npcGo.GetComponent<MonsterEntity>();
                npc.Running = npc.IdentityInterlude.IsRunning;
                ((MonsterEntity)npc).NpcData = npcData;
            }

            // Pooled reuse may still have death latch from a previous corpse fade.
            npc.SetDead(false);



            Appearance appearance = new Appearance();
            appearance.RHand = npcgrp.Rhand;
            appearance.LHand = npcgrp.Lhand;
            appearance.CollisionRadius = npcgrp.CollisionRadius;
            appearance.CollisionHeight = npcgrp.CollisionHeight;

            

            npc.Status = status;

            npc.Stats = stats;

            npc.IdentityInterlude = identity;
            npc.IdentityInterlude.NpcClass = npcgrp.ClassName;
            npc.IdentityInterlude.Name = npcName.Name;
            npc.IdentityInterlude.Title = npcName.Title;

            if (npc.IdentityInterlude.Title == null || npc.IdentityInterlude.Title.Length == 0)
            {
                if (identity.EntityType == EntityType.Monster)
                {
                    npc.IdentityInterlude.Title = " Lvl: " + npc.Stats.Level;
                }
            }
            npc.IdentityInterlude.TitleColor = npcName.TitleColor;

            npc.Appearance = appearance;

           

            // Keep prefab name prefix so pool name-fallback still works; show NPC name in suffix.
            npcGo.transform.name = go != null
                ? $"{go.name}_{identity.Name}"
                : identity.Name;
            SanitizeCharacterControllerStepOffset(npcGo);
            npcGo.SetActive(true);


            if (npc.GetType() == typeof(MonsterEntity))
            {
                msm = InitMonster(npc, npcGo);
            }
            else
            {
                InitNpc(npc, npcGo);
            }



            RespawnPositionElseLoadingGame(identity, npcGo);


            if (msm != null) _msObjects.Add(npc.IdentityInterlude.Id, msm);
            _npcs.Add(identity.Id, npc);
            _objects.Add(identity.Id, npc);
            Debug.Log("NPC NEW SPAWN !!!!!!!!!! " + identity.Id);
        }
        else
        {
            Debug.LogWarning("NPC Not Found Nps!!!!! Need add server ID " + identity.Id  + " Npc Id " + identity.NpcId);
        }
    }

    /// <summary>
    /// City NPC / field monster → ObjectPool. Falls back to Instantiate if pool unavailable.
    /// </summary>
    private GameObject AcquireNpcGameObject(GameObject prefab, NetworkIdentityInterlude identity)
    {
        if (prefab == null)
        {
            return null;
        }

        ObjectType? poolType = null;
        if (identity.EntityType == EntityType.NPC)
        {
            poolType = ObjectType.Npc;
        }
        else if (identity.EntityType == EntityType.Monster)
        {
            poolType = ObjectType.Monster;
        }

        if (poolType.HasValue && ObjectPoolManager.Instance != null)
        {
            ObjectType tag = poolType.Value;
            ObjectPoolManager.Instance.AddPrefabToPool(tag, prefab, 1);
            GameObject pooled = ObjectPoolManager.Instance.SpawnFromPool(tag, prefab);
            if (pooled != null)
            {
                pooled.transform.SetPositionAndRotation(identity.Position, identity.Heading);
                Debug.Log(
                    $"[{tag}Pool] Acquire mesh={prefab.name} name={identity.Name} id={identity.Id}");
                return pooled;
            }
        }

        bool prefabWasActive = prefab.activeSelf;
        if (prefabWasActive)
        {
            prefab.SetActive(false);
        }

        GameObject npcGo = Instantiate(prefab, identity.Position, identity.Heading);

        if (prefabWasActive)
        {
            prefab.SetActive(true);
        }

        return npcGo;
    }

    private void ChangeEntityType(NetworkIdentityInterlude identity)
    {
        //Cat Npc
        if (identity.NpcId == 31760)
        {
            Debug.Log("SpawnNpcInterlude>>> Spawn 31760 p5");
            identity.EntityType = EntityType.NPC;
        }
    }

    /// <summary>
    /// Call after parenting. Broken L2 NPC prefabs often have stepOffset &gt; scaled capsule —
    /// Unity asserts on SetActive. World NPCs are mostly placeholders → stepOffset = 0.
    /// </summary>
    private static void SanitizeCharacterControllerStepOffset(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        CharacterController[] controllers = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterController cc = controllers[i];
            if (cc == null)
            {
                continue;
            }

            cc.stepOffset = 0f;
        }
    }

    //The only npcs that move in the game
    //Leandro
    //Remy
    private void RespawnPositionElseLoadingGame(NetworkIdentityInterlude identity , GameObject npcGo)
    {
        if (identity.Name.Equals("Leandro") | identity.Name.Equals("Remy"))
        {
            CharMoveToLocation lastLocation = InitPacketsLoadWord.getInstance().GetMoveToLocation(identity.Id);

            if(lastLocation != null)
            {
                PositionValidationController.Instance.AddInitPosition(lastLocation);
            }
            
        }
    }


    public void UpdateNpcInfo(Entity entity , NpcInfo npcInfo)
    {
        if(entity.GetType() == typeof(MonsterEntity))
        {
            MonsterEntity m_entity = (MonsterEntity) entity;
            m_entity.UpdateNpcPAtkSpd((int)npcInfo.Stats.PAtkRealSpeed);
            m_entity.UpdateNpcRunningSpd(npcInfo.Stats.RunRealSpeed);
            m_entity.UpdateNpcWalkSpd(npcInfo.Stats.WalkRealSpeed);
            m_entity.Running = npcInfo.Identity.IsRunning;
        }


    }


    public void UpdateUserInfo(Entity entity, UserInfo userInfo)
    {
        if (entity.GetType() == typeof(PlayerEntity))
        {
            PlayerEntity p_entity = (PlayerEntity)entity;

            var statsIntr = userInfo.PlayerInfoInterlude.Stats;

            p_entity.UpdateRunSpeed(statsIntr.RunRealSpeed);
            p_entity.UpdateWalkSpeed(statsIntr.WalkRealSpeed);


            p_entity.UpdatePAtkSpeedPlayer((int)statsIntr.BasePAtkSpeed);
            p_entity.UpdateMAtkSpeed((int)statsIntr.MAtkSpd);
        }
    }


    private MonsterStateMachine InitMonster(Entity npc , GameObject npcGo)
    {
        var animationController = npc.GetComponent<NetworkAnimationController>();
        animationController.Initialize();
        npcGo.GetComponent<Gear>().Initialize(npc.IdentityInterlude.Id, npc.RaceId);
        npc.Initialize();
        var msm = npcGo.GetComponent<MonsterStateMachine>();

        if (msm != null)
        {
            AnimationManager.Instance.RegisterController(npc.IdentityInterlude.Id, animationController, npc);
            npc.UpdateNpcPAtkSpd((int)npc.Stats.PAtkRealSpeed);
            npc.UpdateNpcRunningSpd(npc.Stats.RunRealSpeed);
            npc.UpdateNpcWalkSpd(npc.Stats.WalkRealSpeed);
            npc.Running = npc.IdentityInterlude.IsRunning;
            msm.Initialize(npc.IdentityInterlude.Id, npc.IdentityInterlude.NpcId, npcGo, npc);
        }

        return msm;
    }

    private void InitNpc(Entity npc, GameObject npcGo)
    {
        var animationController = npc.GetComponent<NetworkAnimationController>();
        animationController.Initialize();
        MoveNpc moveNpc = npcGo.GetComponent<MoveNpc>();


        npcGo.GetComponent<Gear>().Initialize(npc.IdentityInterlude.Id, npc.RaceId);
        npc.Initialize();
        var nsm = npcGo.GetComponent<NpcStateMachine>();
        if (nsm != null)
        {
            AnimationManager.Instance.RegisterController(npc.IdentityInterlude.Id, animationController, npc);
            npc.UpdateNpcPAtkSpd((int)npc.Stats.PAtkSpd);
            npc.UpdateNpcRunningSpd(npc.Stats.RunRealSpeed);
            npc.UpdateNpcWalkSpd(npc.Stats.WalkRealSpeed);
            npc.Running = npc.IdentityInterlude.IsRunning;
            nsm.Initialize(npc.IdentityInterlude.Id, npc.IdentityInterlude.NpcId, npcGo, moveNpc, npc);
        }
    }

    public async Task DeleteObject(int objectId)
    {
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
                DeadManager.Instance.AddDeadAndRemove(objectId , new DeadData(true, entity));
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
            RemoveObject(objectId);
            Debug.Log($"[DeleteObject] HANDLER id={objectId} NpcEntity → RemoveObject done");
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
        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 1.0f, Vector3.down, out hit, 2.5f, _groundMask)) {
            return hit.point.y;
        }

        return pos.y;
    }

    public string getEntityName(int id)
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
                // Match original client after TeleportToLocation / L2_Teleport:
                // ValidatePosition then Appearing → server onTeleported() + UserInfo / knownlist.
                SendValidatePosition(grounded);
                SendAppearing();
            }
        });
    }

    private void SendValidatePosition(Vector3 position)
    {
        ValidatePosition sendPaket = CreatorPacketsUser.CreateValidatePosition(position.x, position.y, position.z);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
    }

    private void SendAppearing()
    {
        Appearing packet = CreatorPacketsUser.CreateAppearing();
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(packet, enable, enable);
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
                WorldCombat.Instance.InflictAttack(targetEntity.transform, damage, criticalHit);
            }
        });
    }

    public Task UpdateObjectMoveDirection(int id, int speed, Vector3 direction) {
        return ExecuteWithEntityAsync(id, e => {
            if (speed != e.Stats.Speed) {
                e.UpdateSpeed(speed);
            }
            // Movement direction applied via MoveAllCharacters / CharMoveToLocation.
        });
    }

    public Task UpdateEntityTarget(int id, int targetId) {
        return ExecuteWithEntitiesAsync(id, targetId, (targeter, targeted) => {
            targeter.TargetId = targetId;
            targeter.Target = targeted.transform;
        });
    }


    public Task StatusUpdate(int id, List<StatusUpdatePacket.Attribute> attributes) {
        return ExecuteWithEntityAsync(id, e => {
            if(WorldCombat.Instance != null)
            {
                WorldCombat.Instance.StatusUpdate(e, attributes, id);
                if (e.GetType() == typeof(PlayerEntity))
                {
                    if(CharacterInfoWindow.Instance != null)
                    {
                        CharacterInfoWindow.Instance.UpdateValues();
                    }
                   
                }
            }

        });
    }

    public Task UserInfoUpdateCharacter(UserInfo user)
    {
        return ExecuteWithEntityAsync(user.PlayerInfoInterlude.Identity.Id, e => {
            WorldCombat.Instance.StatusUpdate(e, user.PlayerInfoInterlude.Stats, user.PlayerInfoInterlude.Status , user.PlayerInfoInterlude.Identity.Id);
            if (e == PlayerEntity.Instance)
            {
                PlayerEntity.Instance.Running = user.PlayerInfoInterlude.Appearance.Running;
                CharacterInfoWindow.Instance.UpdateValues();
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
