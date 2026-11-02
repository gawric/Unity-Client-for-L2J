using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Keeps each pawn's cubic list in step with CharInfo/UserInfo and moves the
/// spawned emitter every frame. The math stays in <see cref="CubicCollection"/>,
/// <see cref="CubicMotion"/> and <see cref="CubicCast"/>.
///
/// Those methods treat Z as up (the native pawn axis) while this project is
/// Y-up, so positions and directions are swapped at this boundary and swapped
/// back before the visual is placed.
/// </summary>
public sealed class CubicService : ILateTickable
{
    const float FallbackRadiusUu = 7.5f;
    const float FallbackHeightUu = 22.8f;
    const float MoveEpsilon = 0.01f;

    readonly StriderRideService _ride;
    readonly Dictionary<int, OwnerCubics> _owners = new Dictionary<int, OwnerCubics>();
    readonly Dictionary<string, BaseEffect> _prefabs = new Dictionary<string, BaseEffect>();
    readonly HashSet<string> _missing = new HashSet<string>();
    readonly List<int> _gone = new List<int>();
    readonly List<Vector3> _locBefore = new List<Vector3>(3);
    readonly List<CubicStrikeFx> _strike = new List<CubicStrikeFx>(4);
    readonly HashSet<int> _loggedOtherSkills = new HashSet<int>();
    readonly EffectParticle _bodySettings;
    readonly EffectParticle _castSettings;

    public CubicService(StriderRideService ride)
    {
        _ride = ride;
        _bodySettings = ScriptableObject.CreateInstance<EffectParticle>();
        _bodySettings.isFollowCaster = false;
        _bodySettings.defaultLifeTime = 3600f;
        _bodySettings.hideTime = 0f;
        _castSettings = ScriptableObject.CreateInstance<EffectParticle>();
        _castSettings.isFollowCaster = false;
        _castSettings.defaultLifeTime = 3f;
        _castSettings.hideTime = 0.2f;
    }

    public void Sync(Entity owner)
    {
        if (owner == null || owner.Identity == null)
            return;

        int id = owner.Identity.Id;
        OwnerCubics slot;
        if (!_owners.TryGetValue(id, out slot))
        {
            slot = new OwnerCubics();
            slot.LastPosition = owner.transform.position;
            _owners[id] = slot;
        }

        slot.Owner = owner;
        ApplyList(slot);
    }

    public void OnMagicSkillUse(Entity owner, Entity target, MagicSkillUseDto dto)
    {
        int skillId = dto != null ? dto.SkillId : 0;
        int ownerId = owner != null && owner.Identity != null ? owner.Identity.Id : 0;
        int targetId = target != null && target.Identity != null ? target.Identity.Id : 0;
        int type = CubicConstants.CubicTypeForSkill(skillId);

        if (owner == null || owner.Identity == null)
        {
            Debug.Log("[Cubic] skill=" + skillId + " why=no-owner");
            return;
        }

        if (type == 0)
        {
            if (_loggedOtherSkills.Add(skillId))
            {
                Debug.Log("[Cubic] skill=" + skillId +
                          " atk=" + ownerId +
                          " tgt=" + targetId +
                          " why=not-cubic-skill");
            }
            return;
        }

        Sync(owner);
        OwnerCubics slot;
        if (!_owners.TryGetValue(ownerId, out slot))
        {
            Debug.Log("[Cubic] skill=" + skillId + " atk=" + ownerId + " why=no-slot");
            return;
        }

        Transform aim = CubicConstants.CastTargetsOwner(type)
            ? owner.transform
            : (target != null ? target.transform : null);
        string why;
        int armed = 0;
        if (aim == null)
        {
            why = "no-aim";
        }
        else
        {
            armed = CubicCast.ArmCastingCubics(slot.Collection, owner.transform, aim, skillId);
            if (armed > 0)
                StampCastPacket(slot, type, dto);
            why = armed > 0 ? "armed" : "type-missing";
        }

        slot.FlyWatch = 4f;
        slot.FlyLogIn = 0f;
        Debug.Log("[Cubic] skill=" + skillId +
                  " atk=" + ownerId +
                  " tgt=" + targetId +
                  " need=" + type +
                  " have=" + HeldTypes(slot) +
                  " armed=" + armed +
                  " why=" + why +
                  " state=" + StateOf(slot, type) +
                  " aim=" + (aim != null ? aim.name : "none"));
    }

    public void Release(int objectId)
    {
        OwnerCubics slot;
        if (!_owners.TryGetValue(objectId, out slot))
            return;

        slot.Collection.DeleteAll();
        _owners.Remove(objectId);
    }

    public void LateTick()
    {
        if (_owners.Count == 0 && PlayerEntity.Instance != null)
            Sync(PlayerEntity.Instance);

        if (_owners.Count == 0)
            return;

        _gone.Clear();
        float dt = Time.deltaTime;
        foreach (KeyValuePair<int, OwnerCubics> pair in _owners)
        {
            OwnerCubics slot = pair.Value;
            Entity owner = slot.Owner;
            if (owner == null || owner.Identity == null || owner.Identity.Id != pair.Key)
            {
                slot.Collection.DeleteAll();
                _gone.Add(pair.Key);
                continue;
            }

            ApplyList(slot);
            Step(slot, dt);
        }

        for (int i = 0; i < _gone.Count; i++)
            _owners.Remove(_gone[i]);
    }

    void ApplyList(OwnerCubics slot)
    {
        PlayerStats stats = slot.Owner.Stats as PlayerStats;
        int[] ids = stats != null ? stats.Cubics : null;
        slot.Collection.CheckCubicStatus(ids, slot.Owner.transform);
        EnsureVisuals(slot);
    }

    void Step(OwnerCubics slot, float deltaTime)
    {
        Entity owner = slot.Owner;
        Transform ownerTransform = owner.transform;
        Vector3 unityPos = ownerTransform.position;
        float moved = Vector3.Distance(unityPos, slot.LastPosition);
        bool moving = moved > MoveEpsilon;
        slot.LastPosition = unityPos;
        // Native ORBIT speed is |Pawn.Velocity| (ASM GetVelocity: FVector::Size at +0x1D4),
        // ~116 UU/s, the same rate the pawn itself moves. PlayerEntity stores that raw
        // UU value in UnitySpeedRun, so passing it as m/s clamps the step to the whole
        // gap and the cubic sticks to the ring. Displacement / dt is that velocity in meters.
        float ownerSpeed = deltaTime > 0.0001f ? moved / deltaTime : 0f;

        IReadOnlyList<CubicInstance> cubics = slot.Collection.Cubics;
        bool anyCast = false;
        Transform castTarget = null;
        for (int i = 0; i < cubics.Count; i++)
        {
            CubicInstance cubic = cubics[i];
            if (cubic.MovementState == CubicConstants.StateCast)
            {
                anyCast = true;
                if (castTarget == null)
                    castTarget = cubic.Target;
                continue;
            }

            cubic.MovementState = moving
                ? CubicConstants.StateSpawn
                : CubicConstants.StateFollow;
        }

        Vector3 origin = SwapYz(unityPos);
        Vector3 headUnity = HeadPosition(ownerTransform, owner);
        Vector3 bone = SwapYz(headUnity);
        Vector3 forward = SwapYz(ownerTransform.forward);
        bool dead = owner.IsDead();
        byte waitType = dead ? (byte)2 : (byte)0;
        bool riding = _ride != null && _ride.IsRiding(owner);
        float radius = Meters(owner.Appearance != null ? owner.Appearance.CollisionRadius : 0f, FallbackRadiusUu);
        float height = Meters(owner.Appearance != null ? owner.Appearance.CollisionHeight : 0f, FallbackHeightUu);
        float groundUu = owner.Stats != null ? owner.Stats.RunRealSpeed : 0f;
        if (groundUu < 1f)
            groundUu = 116f;

        _locBefore.Clear();
        for (int i = 0; i < cubics.Count; i++)
            _locBefore.Add(cubics[i].Location);

        CubicMotion.UpdateOrbit(slot.Collection, origin, bone, forward, radius, height,
                                 waitType, dead, riding, ownerSpeed, deltaTime);
        CubicMotion.UpdateFollow(slot.Collection, origin, bone, forward, radius, height,
                                 groundUu, waitType, dead, riding, deltaTime);
        LogChase(slot, cubics, deltaTime, moved, ownerSpeed, moving, height);

        if (anyCast)
        {
            // ASM GetVelocity case 2 writes Dest = e_bone. Imported skeletons
            // have no e_bone. XZ stays on the soulshot capsule border. Y is the
            // nameplate crown (capsule center + half height) plus 2 cm.
            Vector3 targetStop = bone;
            if (castTarget != null)
            {
                Entity aimBody = EntityOn(castTarget);
                Vector3 stopUnity = aimBody != null
                    ? StopBesideCrown(aimBody, unityPos)
                    : HeadPosition(castTarget, null);
                targetStop = SwapYz(stopUnity);
            }
            Vector3 targetBone = targetStop;
            // GroundSpeed is UU/s and the divisor 22 is UU, so the coefficient
            // stays groundUu/22 in meter space.
            float targetGroundUu = groundUu;
            Entity aimEntity = EntityOn(castTarget);
            if (aimEntity != null && aimEntity.Stats != null && aimEntity.Stats.RunRealSpeed > 1f)
                targetGroundUu = aimEntity.Stats.RunRealSpeed;
            CubicMotion.UpdateCast(slot.Collection, origin, bone, forward, targetBone,
                                   targetGroundUu, height, waitType, dead, riding, deltaTime);
            LogFly(slot, cubics, true, castTarget, targetGroundUu, deltaTime);
        }
        else if (slot.FlyWatch > 0f)
        {
            LogFly(slot, cubics, false, null, 0f, deltaTime);
        }

        // Location is the motion space (Y/Z swapped). The shot reads the visual.
        PlaceVisuals(owner, cubics, unityPos, headUnity);
        if (anyCast)
            StepCastEffects(ownerTransform, cubics, deltaTime);
    }

    static void PlaceVisuals(Entity owner, IReadOnlyList<CubicInstance> cubics, Vector3 unityPos, Vector3 headUnity)
    {
        for (int i = 0; i < cubics.Count; i++)
        {
            CubicInstance cubic = cubics[i];
            if (cubic.Visual == null)
                continue;

            Vector3 placed = SwapYz(cubic.Location);
            cubic.Visual.transform.position = placed;
            if (!cubic.AppearanceLogged)
            {
                cubic.AppearanceLogged = true;
                LogAppeared(owner, cubic, unityPos, headUnity, placed);
            }
        }
    }

    void StepCastEffects(Transform owner, IReadOnlyList<CubicInstance> cubics, float deltaTime)
    {
        for (int i = 0; i < cubics.Count; i++)
        {
            CubicInstance cubic = cubics[i];
            if (cubic.MovementState != CubicConstants.StateCast)
                continue;

            _strike.Clear();
            CubicCast.NCubicSkillProcess(cubic, deltaTime, _strike);
            for (int s = 0; s < _strike.Count; s++)
                SpawnStrike(owner, cubic, _strike[s]);
        }
    }

    void SpawnStrike(Transform owner, CubicInstance cubic, CubicStrikeFx fx)
    {
        Vector3 atCubic = cubic.Visual != null
            ? cubic.Visual.transform.position
            : SwapYz(cubic.Location);
        Entity targetBody = EntityOn(cubic.Target);
        Vector3 atTarget = cubic.Target != null
            ? (targetBody != null
                ? StopBesideCrown(targetBody, atCubic)
                : HeadPosition(cubic.Target, null))
            : atCubic;
        BaseEffect prefab = LoadPrefab(ClassName(fx.Name), true);
        CompositeEffectV2 prefabComposite = prefab as CompositeEffectV2;
        // _fl / _ra / _pr, ShotProjectile, or NSkillProjectile. Heal and the
        // vampiric burst have neither, so they stay on the pawn they already hit.
        bool outbound = !fx.OnCubic &&
                        cubic.Target != null &&
                        prefabComposite != null &&
                        prefabComposite.HasCubicOutboundFlight();

        Vector3 pos = outbound || fx.OnCubic ? atCubic : atTarget;
        Transform follow = outbound
            ? null
            : (fx.OnCubic
                ? (cubic.Visual != null ? cubic.Visual.transform : null)
                : (fx.AttachToTarget ? cubic.Target : null));
        // AttachToBone effects are stationary at CasterRoot, so the context
        // caster is the target. An outbound orb is aimed from the cubic.
        Transform contextOwner = outbound
            ? owner
            : (fx.OnCubic
                ? follow
                : (fx.AttachToTarget && cubic.Target != null ? cubic.Target : owner));

        if (!fx.OnCubic && !outbound && cubic.CharInfoShotPlayed)
            return;

        if (!fx.OnCubic && !outbound && !cubic.CharInfoShotChecked)
        {
            cubic.CharInfoShotChecked = true;
            Entity ownerEntity = EntityOn(owner);
            Transform cubicCaster = cubic.Visual != null ? cubic.Visual.transform : contextOwner;
            if (ownerEntity != null && cubic.CastPacket != null &&
                EntityActionSkill.PlayImmediateSkillShot(ownerEntity, cubic.CastPacket, cubicCaster))
            {
                cubic.CharInfoShotPlayed = true;
                Debug.Log("[Cubic] strike type=" + cubic.CubicType +
                          " skill=" + cubic.SkillId +
                          " fx=" + fx.Name +
                          " via=charinfo");
                return;
            }
        }

        if (prefab == null)
            return;

        BaseEffect effect = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
        effect.gameObject.name = fx.Name;
        if (follow != null)
            effect.transform.SetParent(follow, true);

        CompositeEffectV2 composite = effect as CompositeEffectV2;
        if (composite != null && fx.SizeScale > 0f && fx.SizeScale != 1f)
            composite.MultiplyPartScale(fx.SizeScale);

        MagicCastData cast = null;
        if (!fx.OnCubic)
        {
            Entity ownerEntity = EntityOn(owner);
            Transform cubicCaster = cubic.Visual != null ? cubic.Visual.transform : contextOwner;
            cast = ownerEntity != null && cubic.CastPacket != null
                ? EntityActionSkill.BuildImmediateCast(ownerEntity, cubic.CastPacket, cubicCaster)
                : ImmediateCast(ObjectId(cubic.Target));
        }

        effect.Setup(_castSettings, cast, contextOwner != null ? contextOwner : owner);
        if (outbound && composite != null)
        {
            Transform muzzle = cubic.Visual != null ? cubic.Visual.transform : effect.transform;
            composite.ArmCubicOutbound(muzzle, cubic.Target);
            string impactName = CubicConstants.ImpactEffectName(cubic.SkillId);
            if (!string.IsNullOrEmpty(impactName))
                composite.ArmCubicImpact(LoadPrefab(impactName, true));
        }

        effect.Play();
        if (composite != null && cast != null)
            composite.ReleaseShot();

        Debug.Log("[Cubic] strike type=" + cubic.CubicType +
                  " skill=" + cubic.SkillId +
                  " fx=" + fx.Name +
                  " scale=" + fx.SizeScale.ToString("0.0") +
                  " attach=" + (fx.AttachToTarget ? 1 : 0) +
                  " via=" + (outbound ? "orb" : "charinfo-cast"));
    }

    static void StampCastPacket(OwnerCubics slot, int type, MagicSkillUseDto dto)
    {
        IReadOnlyList<CubicInstance> cubics = slot.Collection.Cubics;
        for (int i = 0; i < cubics.Count; i++)
        {
            if (cubics[i].CubicType != type)
                continue;
            cubics[i].CastPacket = dto;
            cubics[i].CharInfoShotChecked = false;
            cubics[i].CharInfoShotPlayed = false;
        }
    }

    static MagicCastData ImmediateCast(int targetObjectId)
    {
        MagicCastData cast = new MagicCastData();
        cast.StartTime = Time.time;
        cast.SpawnShotVisualsImmediately = true;
        cast.TargetObjectId = targetObjectId;
        return cast;
    }

    static Entity EntityOn(Transform transform)
    {
        if (transform == null)
            return null;
        Entity entity = transform.GetComponent<Entity>();
        if (entity == null)
            entity = transform.GetComponentInParent<Entity>();
        return entity;
    }

    static int ObjectId(Transform transform)
    {
        Entity entity = EntityOn(transform);
        if (entity == null || entity.Identity == null)
            return 0;
        return entity.Identity.Id;
    }

    void EnsureVisuals(OwnerCubics slot)
    {
        IReadOnlyList<CubicInstance> cubics = slot.Collection.Cubics;
        for (int i = 0; i < cubics.Count; i++)
        {
            CubicInstance cubic = cubics[i];
            if (cubic.Visual != null)
                continue;

            BaseEffect prefab = LoadPrefab(ClassName(cubic.EmitterName), false);
            if (prefab == null)
                continue;

            Vector3 head = HeadPosition(slot.Owner.transform, slot.Owner);
            Vector3 spawn = head + Vector3.up * VectorUtils.ConvertL2UuToMeters(CubicConstants.SpawnZOffset);
            BaseEffect effect = UnityEngine.Object.Instantiate(prefab, spawn, Quaternion.identity);
            effect.gameObject.name = cubic.EmitterName;
            effect.BindLifetimeToHost();
            effect.Setup(_bodySettings, null, slot.Owner.transform);
            effect.Play();
            cubic.Visual = effect.gameObject;
            cubic.Location = SwapYz(spawn);
            cubic.DestLocation = Vector3.zero;
        }
    }

    void SpawnBurst(string lineageName, Vector3 worldPos)
    {
        BaseEffect prefab = LoadPrefab(ClassName(lineageName), true);
        if (prefab == null)
            return;

        BaseEffect effect = UnityEngine.Object.Instantiate(prefab, worldPos, Quaternion.identity);
        effect.gameObject.name = lineageName;
        effect.Setup(_castSettings, null, null);
        effect.Play();
    }

    BaseEffect LoadPrefab(string className, bool preferComposite)
    {
        if (string.IsNullOrEmpty(className))
            return null;

        string key = (preferComposite ? "c:" : "e:") + className;
        BaseEffect cached;
        if (_prefabs.TryGetValue(key, out cached))
            return cached;

        string emitter = "Data/Effects/" + className + "/" + className + "/" + className;
        string composite = "Data/Effects/" + className + "/" + className + "_composite";
        BaseEffect prefab = preferComposite
            ? Resources.Load<BaseEffect>(composite)
            : Resources.Load<BaseEffect>(emitter);
        if (prefab == null)
            prefab = Resources.Load<BaseEffect>(preferComposite ? emitter : composite);

        _prefabs[key] = prefab;
        if (prefab == null && _missing.Add(className))
            Debug.LogWarning("[Cubic] missing effect prefab " + className);
        return prefab;
    }

    static string ClassName(string lineageName)
    {
        if (string.IsNullOrEmpty(lineageName))
            return null;
        const string prefix = "LineageEffect.";
        if (lineageName.StartsWith(prefix))
            return lineageName.Substring(prefix.Length);
        return lineageName;
    }

    const float CrownClearanceMeters = 0.02f;

    static Vector3 StopBesideCrown(Entity target, Vector3 fromWorld)
    {
        Vector3 beside = HitManager.BorderFacing(target, fromWorld);
        float collisionHeight = target.Appearance != null ? target.Appearance.CollisionHeight : 0f;
        Vector3 crown = L2NameplateAnchor.GetHeadWorldPos(
            target.transform, collisionHeight, CrownClearanceMeters);
        beside.y = crown.y;
        return beside;
    }

    static Vector3 HeadPosition(Transform owner, Entity entity)
    {
        float collisionHeight = entity != null && entity.Appearance != null
            ? entity.Appearance.CollisionHeight
            : 0f;
        return L2NameplateAnchor.GetHeadWorldPos(
            owner, collisionHeight, L2NameplateAnchor.DefaultHeadHeightOffsetMeters);
    }

    static void LogAppeared(Entity owner, CubicInstance cubic, Vector3 feet, Vector3 head, Vector3 visual)
    {
        CharacterController cc = owner.GetComponent<CharacterController>();
        if (cc == null)
            cc = owner.GetComponentInChildren<CharacterController>();

        string name = owner.Identity != null ? owner.Identity.Name : owner.name;
        int id = owner.Identity != null ? owner.Identity.Id : 0;
        float ccHeight = cc != null ? cc.height : 0f;
        Debug.Log("[Cubic] appeared owner=" + name +
                  " id=" + id +
                  " type=" + cubic.CubicType +
                  " emitter=" + cubic.EmitterName +
                  " feetY=" + feet.y.ToString("F2") +
                  " headY=" + head.y.ToString("F2") +
                  " ccHeight=" + ccHeight.ToString("F2") +
                  " visualY=" + visual.y.ToString("F2") +
                  " aboveFeet=" + (visual.y - feet.y).ToString("F2"));
    }

    void LogChase(OwnerCubics slot, IReadOnlyList<CubicInstance> cubics, float deltaTime,
                  float moved, float ownerSpeed, bool moving, float height)
    {
        if (cubics.Count == 0)
        {
            slot.WasMoving = false;
            return;
        }

        bool started = moving && !slot.WasMoving;
        bool stopped = !moving && slot.WasMoving;
        if (moving)
        {
            slot.StopFrames = 0;
            slot.MoveLogIn -= deltaTime;
        }
        else if (stopped)
        {
            slot.StopFrames = 90;
        }

        bool sampleRun = moving && (started || slot.MoveLogIn <= 0f);
        bool sampleStop = !moving && slot.StopFrames > 0;
        if (sampleRun)
            slot.MoveLogIn = 0.5f;

        if (sampleRun || sampleStop || stopped)
        {
            int ownerId = slot.Owner.Identity != null ? slot.Owner.Identity.Id : 0;
            float reachUu = height / 5f / VectorUtils.L2UuToUnity;
            string phase = stopped ? "STOP" : (sampleStop ? "catch" : "run");
            for (int i = 0; i < cubics.Count && i < _locBefore.Count; i++)
            {
                CubicInstance cubic = cubics[i];
                if (cubic.MovementState == CubicConstants.StateCast)
                    continue;

            }
        }

        if (sampleStop)
            slot.StopFrames--;
        slot.WasMoving = moving;
    }

    void LogFly(OwnerCubics slot, IReadOnlyList<CubicInstance> cubics, bool anyCast,
                Transform castTarget, float targetGroundUu, float deltaTime)
    {
        slot.FlyWatch -= deltaTime;
        slot.FlyLogIn -= deltaTime;
        if (slot.FlyLogIn > 0f)
            return;
        slot.FlyLogIn = 0.25f;

        if (!anyCast)
        {
            Debug.Log("[Cubic] fly why=not-in-cast states=" + States(cubics));
            return;
        }

        float coef = targetGroundUu / 22f;
        for (int i = 0; i < cubics.Count; i++)
        {
            CubicInstance cubic = cubics[i];
            if (cubic.MovementState != CubicConstants.StateCast)
                continue;

            string why;
            if (cubic.CubicType == 1 || cubic.CubicType == 3 || cubic.CubicType == 8)
                why = "anchor-type";
            else if (cubic.Target == null)
                why = "no-target";
            else if (castTarget == null)
                why = "no-target-bone";
            else
                why = "chase";

            float gapUu = Uu(Vector3.Distance(cubic.DestLocation, cubic.Location));
            float stepUu = i < _locBefore.Count
                ? Uu(Vector3.Distance(cubic.Location, _locBefore[i]))
                : 0f;
            Debug.Log("[Cubic] fly type=" + cubic.CubicType +
                      " why=" + why +
                      " gapUu=" + gapUu.ToString("F1") +
                      " stepUu=" + stepUu.ToString("F2") +
                      " coef=" + coef.ToString("F2") +
                      " t=" + cubic.SkillActiveTime.ToString("F2") +
                      " aim=" + (cubic.Target != null ? cubic.Target.name : "none"));
        }
    }

    static string HeldTypes(OwnerCubics slot)
    {
        IReadOnlyList<CubicInstance> cubics = slot.Collection.Cubics;
        if (cubics.Count == 0)
            return "none";
        string text = "";
        for (int i = 0; i < cubics.Count; i++)
        {
            if (i > 0)
                text += ",";
            text += cubics[i].CubicType;
        }
        return text;
    }

    static string StateOf(OwnerCubics slot, int type)
    {
        IReadOnlyList<CubicInstance> cubics = slot.Collection.Cubics;
        for (int i = 0; i < cubics.Count; i++)
        {
            if (cubics[i].CubicType == type)
                return StateName(cubics[i].MovementState);
        }
        return "absent";
    }

    static string States(IReadOnlyList<CubicInstance> cubics)
    {
        if (cubics.Count == 0)
            return "none";
        string text = "";
        for (int i = 0; i < cubics.Count; i++)
        {
            if (i > 0)
                text += ",";
            text += cubics[i].CubicType + ":" + StateName(cubics[i].MovementState);
        }
        return text;
    }

    static string StateName(byte state)
    {
        if (state == CubicConstants.StateSpawn) return "orbit";
        if (state == CubicConstants.StateFollow) return "follow";
        if (state == CubicConstants.StateCast) return "cast";
        return state.ToString();
    }

    static float Uu(float meters)
    {
        return meters / VectorUtils.L2UuToUnity;
    }

    static float Meters(float rawUu, float fallbackUu)
    {
        if (rawUu < 1f)
            rawUu = fallbackUu;
        return VectorUtils.ConvertL2UuToMeters(rawUu);
    }

    static Vector3 SwapYz(Vector3 value)
    {
        return new Vector3(value.x, value.z, value.y);
    }

    sealed class OwnerCubics
    {
        public Entity Owner;
        public Vector3 LastPosition;
        public bool WasMoving;
        public float MoveLogIn;
        public int StopFrames;
        public float FlyWatch;
        public float FlyLogIn;
        public readonly CubicCollection Collection = new CubicCollection();
    }
}
