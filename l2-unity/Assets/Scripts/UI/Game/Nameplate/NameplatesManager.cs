using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

public class NameplatesManager : MonoBehaviour
{
    private VisualElement _rootElement;
    private VisualTreeAsset _nameplateTemplate;
    private readonly Dictionary<int, Nameplate> _nameplates = new Dictionary<int, Nameplate>();
    private Transform _playerTransform;

    [SerializeField] private float _nameplateViewDistance = 50f;
    [SerializeField] private LayerMask _entityMask;
    [SerializeField] public RaycastHit[] _entitiesInRange;

    private static NameplatesManager _instance;
    //private bool _isRemoveNamePlate = false;
    // -1 (not 0) so this never accidentally matches a real object id. Remove() sets this to
    // suppress CreateNameplateForEntities from immediately recreating a nameplate for an entity
    // removed the same frame (e.g. a monster whose collider is still briefly hit by the SphereCast
    // right after it died) - it must be cleared every frame, otherwise it permanently blocks any
    // future entity that happens to reuse that same object id (found via [NameplateDebug]: another
    // player's nameplate never appeared because their id had previously been used by a removed entity).
    private int _removeObjId = -1;
    public static NameplatesManager Instance { get { return _instance; } }

    private void Awake() {
        if(_instance == null) {
            _instance = this;
        } else {
            Destroy(this);
        }
    }

    private void OnDestroy() {
        _nameplates.Clear();
        _instance = null;
    }

    void Start() {
        if(_nameplateTemplate == null) {
            _nameplateTemplate = Resources.Load<VisualTreeAsset>("Data/UI/_Elements/Game/Nameplate");
        }
        if(_nameplateTemplate == null) {
            Debug.LogError("Could not load chat window template.");
        }
    }

    public void SetMask(LayerMask mask) {
        _entityMask = mask;
    }

    private const int kUpdatesPerSecond = 200;
    private const float kUpdateInterval = 1.0f / kUpdatesPerSecond; // how many seconds pass before an update should happen
    private float _accumulation = 0.0f; // stores time elapsed
    private void Update() {
        // add to the accumulator
        _accumulation += Time.deltaTime;

        // while enough time has passed for an update, call our code we want executed 200 times per second.
        while(_accumulation >= kUpdateInterval) {
            UpdateNameplates();
            _accumulation -= kUpdateInterval;
        }

        
    }

    private void FixedUpdate() {
        if(_playerTransform == null) {
            if(PlayerEntity.Instance != null && PlayerEntity.Instance.transform != null) {
                _playerTransform = PlayerEntity.Instance.transform;
            } else {
                return;
            }
        }

        if(!L2GameUI.Instance.UILoaded) {
            return;
        }

        if(_rootElement == null) {
            _rootElement = L2GameUI.Instance.RootElement.Q<VisualElement>("NameplatesContainer");
            return;
        }

        _entitiesInRange = Physics.SphereCastAll(_playerTransform.position, _nameplateViewDistance, transform.forward, 0, _entityMask);
        CreateNameplateForEntities();
        _removeObjId = -1;
        CheckNameplateVisibility();
        CheckMouseOver();
        CheckTarget();
    }

    private void CheckMouseOver() {
        ObjectData hoverObjectData = ClickManager.Instance.HoverObjectData;
        if(hoverObjectData != null) {
            if(_entityMask == (_entityMask | (1 << hoverObjectData.ObjectLayer))) {
                if(hoverObjectData.ObjectTransform != null)
                {
                    Entity e = hoverObjectData.ObjectTransform.GetComponent<Entity>();
                    if (e != null)
                    {
                        if (!_nameplates.ContainsKey(e.IdentityInterlude.Id))
                        {
                            CreateNameplate(e);
                        }
                    }
                }
            }
        }
    }

    private void CheckTarget() {
        if(!TargetManager.Instance.HasTarget()) {
            return;
        }

        Entity e = TargetManager.Instance.Target.Data.ObjectTransform.GetComponent<Entity>();
        if(e != null) {
            if(!_nameplates.ContainsKey(e.IdentityInterlude.Id)) {
                CreateNameplate(e);
            }
        }
    }

    private void CreateNameplateForEntities() {
        if(_entitiesInRange != null)
        {
            foreach (RaycastHit hit in _entitiesInRange)
            {
                Entity objectEntity = hit.transform.GetComponent<Entity>();

                if (objectEntity is UserEntity)
                {
                    Debug.Log($"[NameplateDebug] SphereCast hit '{hit.transform.name}' (layer={LayerMask.LayerToName(hit.transform.gameObject.layer)}) -> UserEntity found, id={objectEntity.IdentityInterlude.Id}");
                }

                if (objectEntity != null)
                {

                    int objectId = objectEntity.IdentityInterlude.Id;
                    if(objectId != _removeObjId)
                    {
                        if (!_nameplates.ContainsKey(objectId))
                        {
                            CreateNameplate(objectEntity);
                        }
                    }

                }
            }
        }

    }

    private void CreateNameplate(Entity entity) {
        if (entity == null) return;
        if(!IsNameplateVisible(entity.transform)) {
            if (entity is UserEntity)
            {
                Debug.Log($"[NameplateDebug] UserEntity id={entity.IdentityInterlude.Id} failed IsNameplateVisible");
            }
            return;
        }

        float height = GetHeight(entity);
        if (entity is UserEntity)
        {
            Debug.Log($"[NameplateDebug] UserEntity id={entity.IdentityInterlude.Id} passed visibility, creating nameplate. Name={entity.IdentityInterlude.Name}, CollisionHeight={entity.Appearance.CollisionHeight}, offsetHeight={height}, entityPos={entity.transform.position}, titleColor='{entity.IdentityInterlude.TitleColor}'");
        }

        VisualElement visualElement = _nameplateTemplate.Instantiate()[0];

        Nameplate nameplate = new Nameplate(
            visualElement,
            visualElement.Q<Label>("EntityName"),
            visualElement.Q<Label>("EntityTitle"),
            entity.transform,
            entity.IdentityInterlude.Title,
            entity.IdentityInterlude.TitleColor,
            height,
            entity.IdentityInterlude.Name,
            entity.IdentityInterlude.Id,
            true
            );

        if (!_nameplates.ContainsKey(entity.IdentityInterlude.Id))
        {
            _nameplates.Add(entity.IdentityInterlude.Id, nameplate);
            _rootElement.Add(visualElement);
        }

    }

    private float GetHeight(Entity entity)
    {
        if(entity is PlayerEntity playerEntity)
        {
            return CharacterHeight.GetHeight(playerEntity.RaceId);
        }

        // Other players use the exact same rig/model as the local player (per race), so the same
        // fixed race-based head height belongs here too, rather than the CollisionHeight-derived
        // formula below - that's tuned for NPCs/monsters, whose Appearance.CollisionHeight is
        // pre-converted to Unity meters by NpcgrpTable, unlike CharInfo (raw L2 UU).
        if(entity is UserEntity)
        {
            return CharacterHeight.GetHeight(entity.RaceId);
        }

        return entity.Appearance.CollisionHeight * 2.1f;
    }

    private void CheckNameplateVisibility() {
        foreach(var nameplateId in _nameplates.Keys) {
            var nameplate = _nameplates[nameplateId];
            if(!IsNameplateVisible(nameplate.Target)) {
                nameplate.Visible = false;
            } else {
                nameplate.Visible = true;
            }
        }
    }

    private void UpdateNameplates() {
        var keysToRemove = new List<int>();
        foreach(var nameplateId in _nameplates.Keys) {
            var nameplate = _nameplates[nameplateId];

            if(!nameplate.Visible) {
                keysToRemove.Add(nameplateId);
            } else {
                UpdateNameplatePosition(nameplate);
                UpdateNameplateStyle(nameplate);
            }
        }
        foreach(var key in keysToRemove) {
            _rootElement.Remove(_nameplates[key].NameplateEle);
            _nameplates.Remove(key);
        }
    }

    public void Remove(int id)
    {
        if (_nameplates.ContainsKey(id))
        {
           // _isRemoveNamePlate = true;
            Nameplate nameplate = _nameplates[id];
            nameplate.Visible = false;
            _removeObjId = id;
            _rootElement.Remove(_nameplates[id].NameplateEle);
            _nameplates.Remove(id);
        }  
    }
    private void UpdateNameplateStyle(Nameplate nameplate) {
        if(TargetManager.Instance.HasTarget() && TargetManager.Instance.Target.Data.ObjectTransform == nameplate.Target) {
            if (TargetManager.Instance.AttackTarget == TargetManager.Instance.Target) {
                nameplate.SetStyle("target-bubble-attack");
            } else {
                nameplate.SetStyle("target-bubble-target");
                nameplate.RemoveStyle("target-bubble-attack");
            }
            return;
        } else {
            nameplate.RemoveStyle("target-bubble-attack");
            nameplate.RemoveStyle("target-bubble-target");
        }
        
        if(ClickManager.Instance.HoverObjectData != null && ClickManager.Instance.HoverObjectData.ObjectTransform == nameplate.Target) {
            nameplate.SetStyle("target-bubble-hover");
        } else {
            nameplate.RemoveStyle("target-bubble-hover");
        }
    }

    private void UpdateNameplatePosition(Nameplate nameplate) {
        try {
            Vector2 nameplatePos = Camera.main.WorldToScreenPoint(nameplate.Target.position + Vector3.up * nameplate.NameplateOffsetHeight);
            nameplate.NameplateEle.style.left = nameplatePos.x - nameplate.NameplateEle.resolvedStyle.width / 2f;
            nameplate.NameplateEle.style.top = Screen.height - nameplatePos.y - nameplate.NameplateEle.resolvedStyle.height;
            if (nameplate.Target != null && nameplate.Target.GetComponent<UserEntity>() != null) {
                Debug.Log($"[NameplateDebug] UserEntity '{nameplate.Name}' position update: screenPos={nameplatePos}, left={nameplate.NameplateEle.style.left}, top={nameplate.NameplateEle.style.top}, resolvedWidth={nameplate.NameplateEle.resolvedStyle.width}, resolvedHeight={nameplate.NameplateEle.resolvedStyle.height}, display={nameplate.NameplateEle.resolvedStyle.display}, opacity={nameplate.NameplateEle.resolvedStyle.opacity}");
            }
        }
        catch (NullReferenceException) { } 
        catch (MissingReferenceException) { }
    }

    private bool IsNameplateVisible(Transform target) {
        bool isDebugTarget = target != null && target.GetComponent<UserEntity>() != null;

        if(target == null) {
            return false;
        }

        bool isHover = ClickManager.Instance.HoverObjectData != null && ClickManager.Instance.HoverObjectData.ObjectTransform == target;
        if(isHover) {
            return true;
        }

        bool isTarget = TargetManager.Instance.HasTarget() && TargetManager.Instance.Target.Data.ObjectTransform == target;
        bool isTooFar = Vector3.Distance(_playerTransform.position, target.position) > _nameplateViewDistance;
        if(isTooFar && !isTarget) {
            if (isDebugTarget) Debug.Log($"[NameplateDebug] too far: distance={Vector3.Distance(_playerTransform.position, target.position)} max={_nameplateViewDistance}");
            return false;
        }

        bool isCamera = CameraController.Instance.IsObjectVisible(target);
        if (isDebugTarget) Debug.Log($"[NameplateDebug] IsObjectVisible={isCamera}");
        return isCamera;
    }
}
