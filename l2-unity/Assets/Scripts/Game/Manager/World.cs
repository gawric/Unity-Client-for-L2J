using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

using UnityEngine;
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
        AnimationManager.Instance.SetAnimationManager(controller , player);

        go.GetComponent<Gear>().Initialize(player.IdentityInterlude.Id, player.RaceId);
        var statsIntr = (PlayerInterludeStats)player.Stats;
        player.Initialize();
        //player.GetComponent<NetworkTransformReceive>().Initialize();
        player.UpdateRunSpeed(statsIntr.RunRealSpeed);
        player.UpdateWalkSpeed(statsIntr.WalkRealSpeed);

        // Debug.Log("PLAYER SPAWN RunSpeed " + statsIntr.RunSpeed);
        //Debug.Log("PLAYER SPAWN PAtkSpd " + statsIntr.PAtkSpd);
        // player.UpdatePAtkSpeedPlayer((int)statsIntr.BasePAtkSpeed);

        //416 - ���������� �����
        //554 - � ����������
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

        //Debug.Log("������ ��������� Spawn Npc Interlude ++++++++++++++++++ ");
        Npcgrp npcgrp = NpcgrpTable.Instance.GetNpcgrp(identity.NpcId);
        NpcName npcName = NpcNameTable.Instance.GetNpcName(identity.NpcId);


        if (npcName == null || npcgrp == null)
        {
            Debug.LogError($"Npc {identity.NpcId} could not be loaded correctly.");
            return;
        }


        GameObject go = ModelTable.Instance.GetNpc(npcgrp.Mesh);

        if(identity.NpcId == 35103)
        {
            Debug.Log(" object NpcInfo 5 " + identity.Id);
        }

        if (go != null)
        {

            Debug.Log("Name NPC " + npcName.Name);
            //Debug ����� ��������� ������ 1 �������� � ��� !!!!
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

            //GameObject npcGo = Instantiate(go, identity.Position, identity.Heading);
            GameObject npcGo = Instantiate(go, identity.Position, identity.Heading);


            NpcData npcData = new NpcData(npcName, npcgrp);

           

            identity.EntityType = EntityTypeParser.ParseEntityType(npcgrp.ClassName);
            Entity npc;


            ChangeEntityType(identity);

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

           

            npcGo.transform.name = identity.Name;
            npcGo.SetActive(true);

            if (identity.NpcId == 35103)
            {
                Debug.Log(" object NpcInfo 5 " + identity.Id);
            }

            if (npc.GetType() == typeof(MonsterEntity))
            {
                InitMonster(npc, npcGo);
            }
            else
            {
                InitNpc(npc, npcGo);
            }

            if (identity.NpcId == 35103)
            {
                Debug.Log(" object NpcInfo 5 " + identity.Id);
            }

            RespawnPositionElseLoadingGame(identity, npcGo);



            _npcs.Add(identity.Id, npc);
            _objects.Add(identity.Id, npc);
            Debug.Log("NPC NEW SPAWN !!!!!!!!!! " + identity.Id);
        }
        else
        {
            Debug.LogWarning("NPC Not Found Nps!!!!! Need add server ID " + identity.Id  + " Npc Id " + identity.NpcId);
        }
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


    private void InitMonster(Entity npc , GameObject npcGo)
    {
        npc.GetComponent<NetworkAnimationController>().Initialize();
        //MoveMonster mm = npcGo.GetComponent<MoveMonster>();
       // GravityMonster gm = npcGo.GetComponent<GravityMonster>();
        npcGo.GetComponent<Gear>().Initialize(npc.IdentityInterlude.Id, npc.RaceId);
        npc.Initialize();
        var msm = npcGo.GetComponent<MonsterStateMachine>();

        if (msm != null)
        {
            //Debug.Log("NPC IS RUNNING " + npc.IdentityInterlude.IsRunning);
            //Debug.Log("NPC IS RunSpeed " + npc.Stats.RunSpeed);
            //Debug.Log("NPC IS WalkSpeed " + npc.Stats.WalkSpeed);
            npc.UpdateNpcPAtkSpd((int)npc.Stats.PAtkRealSpeed);
            npc.UpdateNpcRunningSpd(npc.Stats.RunRealSpeed);
            npc.UpdateNpcWalkSpd(npc.Stats.WalkRealSpeed);
            npc.Running = npc.IdentityInterlude.IsRunning;
            msm.Initialize(npc.IdentityInterlude.Id, npc.IdentityInterlude.NpcId, npcGo, npc);
        }
    }

    private void InitNpc(Entity npc, GameObject npcGo)
    {
        npc.GetComponent<NetworkAnimationController>().Initialize();
        MoveNpc moveNpc = npcGo.GetComponent<MoveNpc>();


        npcGo.GetComponent<Gear>().Initialize(npc.IdentityInterlude.Id, npc.RaceId);
        npc.Initialize();
        var nsm = npcGo.GetComponent<NpcStateMachine>();
        if (nsm != null)
        {
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
        if(entity.GetType() == typeof(MonsterEntity))
        {
            if (entity.IsDead())
            {
                //DeadMonster deadEvent = entity.GetComponent<DeadMonster>();
                //deadEvent.OnDeadAntiGravity(objectId, true, this);
                DeadManager.Instance.AddDeadAndRemove(objectId , new DeadData(true, entity));
            }
            else
            {
                RemoveObject(objectId);
                Debug.Log("REMOVEEEEE OBJECT Name " + entity.name + " ID " + entity.Identity.Id);
            }

        }
        // RemoveObject(objectId);
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
                entity.GetComponent<PlayerTeleport>().TeleportTo(position);
                SendValidatePosition(position);
            }
        });
    }

    private void SendValidatePosition(Vector3 position)
    {
        ValidatePosition sendPaket = CreatorPacketsUser.CreateValidatePosition(position.x, position.y, position.z);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
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

            e.GetComponent<NetworkCharacterControllerReceive>().SetDestination(position);
            e.GetComponent<NetworkTransformReceive>().LookAt(position);
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

            e.GetComponent<NetworkCharacterControllerReceive>().UpdateMoveDirection(direction);
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
