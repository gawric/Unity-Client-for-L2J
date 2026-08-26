using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop / pickup presentation: dropper animation, throw arc, land FX, timed ground glow.
/// Sound is omitted. Timed work runs in <see cref="Tick"/> — no coroutines.
/// </summary>
public sealed class ItemDropPresentationService
{
    sealed class ThrowArcState
    {
        public int ItemObjectId;
        public ItemEntity Item;
        public Vector3 From;
        public Vector3 To;
        public float Elapsed;
        public float Duration;
        public ItemDropVisualKind Kind;
        public Vector3 TumbleAxis;
        public float TumbleDegPerSec;
        public Vector3 LandEuler;
        public bool StickOnLand;
        public float ArcHeight;
    }

    sealed class GroundGlowState
    {
        public GameObject GlowAnchor;
        public float EndTime;
    }

    sealed class CoinMeshRevealState
    {
        public ItemEntity Item;
        public float RevealTime;
    }

    readonly List<ThrowArcState> _throws = new List<ThrowArcState>(8);
    readonly Dictionary<int, GroundGlowState> _groundGlowByItemId = new Dictionary<int, GroundGlowState>();
    readonly Dictionary<int, CoinMeshRevealState> _coinRevealByItemId = new Dictionary<int, CoinMeshRevealState>();
    readonly Dictionary<int, GameObject> _landBurstByItemId = new Dictionary<int, GameObject>();
    readonly List<int> _expiredGlowIds = new List<int>(4);
    readonly List<int> _expiredCoinRevealIds = new List<int>(4);
    readonly ItemDropClickAreaService _clickArea;
    readonly ItemDropWeaponAligner _weapons;
    readonly ItemDropLayerService _layers;
    readonly ItemDropGrpCatalog _grp;

    public ItemDropPresentationService(
        ItemDropClickAreaService clickArea,
        ItemDropWeaponAligner weapons,
        ItemDropLayerService layers,
        ItemDropGrpCatalog grp)
    {
        _clickArea = clickArea;
        _weapons = weapons;
        _layers = layers;
        _grp = grp;
    }

    public void Tick(float deltaTime)
    {
        TickThrows(deltaTime);
        TickGlows();
        TickCoinMeshReveals();
    }

    public void PlayDrop(ItemEntity item, Vector3 landPos, int dropperCharObjId)
    {
        if (item == null)
            return;

        int itemObjectId = item.Identity != null ? item.Identity.Id : 0;
        ItemDropVisualKind kind = ResolveVisualKind(item);
        Entity dropper = ResolveEntity(dropperCharObjId);
        landPos = GroundSnapHelper.SnapToGroundOrKeep(landPos);

        if (dropper != null)
            PlayActorAnimation(dropperCharObjId, ItemDropPresentationIds.DropAnimationTrigger);

        int fallEffectId = ResolveFallEffectId(kind);
        if (fallEffectId > 0 && fallEffectId != ItemDropPresentationIds.LandBurstEffectId)
        {
            PlayEffect(
                fallEffectId,
                dropper != null ? dropper.transform : item.transform,
                "fall-trail");
        }

        // Adena: FX toss, no mesh throw. Everything else flies from the hand
        // (or from a short hover if the packet has no dropper).
        bool throwMesh = kind != ItemDropVisualKind.Adena;
        if (throwMesh)
        {
            Vector3 from = ResolveThrowStart(dropper, landPos);
            StopThrow(itemObjectId);
            BeginThrow(item, from, landPos, itemObjectId, kind);
        }
        else
        {
            LandItem(item, landPos, itemObjectId, kind);
        }

    }

    public void StopItemPresentation(int itemObjectId)
    {
        StopThrow(itemObjectId);
        StopGroundGlow(itemObjectId);
        StopLandBurst(itemObjectId);
        CancelCoinMeshReveal(itemObjectId);
    }

    public void PlayPickup(int itemObjectId, int pickerCharObjId, Vector3 unityPos)
    {
        StopItemPresentation(itemObjectId);
        PlayActorCrossFade(pickerCharObjId, ItemDropPresentationIds.PickupAnimationState);
        PlayEffect(ItemDropPresentationIds.PickupBurstEffectId, unityPos, "pickup-burst");
    }

    void TickThrows(float deltaTime)
    {
        for (int i = _throws.Count - 1; i >= 0; i--)
        {
            ThrowArcState state = _throws[i];
            if (state.Item == null)
            {
                _throws.RemoveAt(i);
                continue;
            }

            state.Elapsed += deltaTime;
            float u = Mathf.Clamp01(state.Elapsed / state.Duration);
            Vector3 pos = Vector3.Lerp(state.From, state.To, u);
            pos.y += Mathf.Sin(u * Mathf.PI) * state.ArcHeight;
            state.Item.transform.position = pos;

            if (state.TumbleDegPerSec != 0f && state.TumbleAxis.sqrMagnitude > 0.0001f)
                state.Item.transform.Rotate(state.TumbleAxis, state.TumbleDegPerSec * deltaTime, Space.World);

            if (state.Elapsed < state.Duration)
                continue;

            _throws.RemoveAt(i);
            LandItem(state.Item, state.To, state.ItemObjectId, state.Kind, state);
        }
    }

    void TickGlows()
    {
        if (_groundGlowByItemId.Count == 0)
            return;

        float now = Time.time;
        _expiredGlowIds.Clear();
        foreach (KeyValuePair<int, GroundGlowState> pair in _groundGlowByItemId)
        {
            if (pair.Value.EndTime <= now)
                _expiredGlowIds.Add(pair.Key);
        }

        for (int i = 0; i < _expiredGlowIds.Count; i++)
            StopGroundGlow(_expiredGlowIds[i]);
    }

    void BeginThrow(ItemEntity item, Vector3 from, Vector3 to, int itemObjectId, ItemDropVisualKind kind)
    {
        SetSpinEnabled(item, false);
        item.transform.position = from;

        ItemDropPose.TryBuildThrowPose(item, _grp.ResolveGrp(item.ItemId), _grp.IsStickWeapon(item.ItemId), from, to, out ItemDropPose.ThrowPose pose);
        Vector3 throwDir = to - from;
        if (pose.TumbleInFlight || pose.StickOnLand)
        {
            Vector3 flat = new Vector3(throwDir.x, 0f, throwDir.z);
            Vector3 fly = flat.sqrMagnitude > 0.0001f
                ? (flat.normalized + Vector3.up * 0.4f).normalized
                : Vector3.forward;
            if (!_weapons.AlignBladeAlong(item.transform, fly))
                item.transform.rotation = Quaternion.LookRotation(fly, Vector3.up);
        }

        float duration = pose.StickOnLand
            ? ItemDropPresentationIds.WeaponThrowArcSeconds
            : ItemDropPresentationIds.DropThrowArcSeconds;

        _throws.Add(new ThrowArcState
        {
            ItemObjectId = itemObjectId,
            Item = item,
            From = from,
            To = to,
            Elapsed = 0f,
            Duration = Mathf.Max(0.05f, duration),
            Kind = kind,
            TumbleAxis = pose.TumbleAxis,
            TumbleDegPerSec = pose.TumbleDegPerSec,
            LandEuler = pose.LandEuler,
            StickOnLand = pose.StickOnLand,
            ArcHeight = pose.ArcHeightMeters
        });
    }

    void LandItem(ItemEntity item, Vector3 landPos, int itemObjectId, ItemDropVisualKind kind)
    {
        LandItem(item, landPos, itemObjectId, kind, null);
    }

    void LandItem(
        ItemEntity item,
        Vector3 landPos,
        int itemObjectId,
        ItemDropVisualKind kind,
        ThrowArcState throwState)
    {
        if (item == null)
            return;

        landPos = GroundSnapHelper.SnapToGroundOrKeep(landPos);
        item.transform.position = landPos;

        if (kind == ItemDropVisualKind.Adena)
        {
            HideCoinDropMeshUntilPile(item, itemObjectId);
            PlayLandBurst(itemObjectId, item.transform);
            StartGroundGlow(itemObjectId, item.transform, kind);
            _clickArea.Refresh(item);
            return;
        }

        bool stick = throwState != null && throwState.StickOnLand;
        if (stick)
        {
            Vector3 throwDir = throwState.To - throwState.From;
            if (!_weapons.AlignBladeTipDown(item.transform, throwDir))
                item.transform.rotation = Quaternion.Euler(throwState.LandEuler);
            _weapons.PlantStuckInGround(item, landPos);
        }
        else
        {
            item.transform.rotation = Quaternion.identity;
            _clickArea.SitOnGround(item);
        }

        _clickArea.Refresh(item);
        StartGroundGlow(itemObjectId, item.transform, kind);
    }

    void StartGroundGlow(int itemObjectId, Transform parent, ItemDropVisualKind kind)
    {
        StopGroundGlow(itemObjectId);

        int glowId = ResolveGroundGlowEffectId(kind);
        float duration = ItemDropPresentationIds.GroundGlowDurationSeconds;
        if (glowId <= 0 && duration <= 0f)
            return;

        var state = new GroundGlowState();
        if (glowId > 0 && parent != null)
        {
            state.GlowAnchor = new GameObject("DropGroundGlow_" + itemObjectId);
            state.GlowAnchor.transform.SetParent(parent, false);
            state.GlowAnchor.transform.localPosition = Vector3.up * ItemDropPresentationIds.GroundGlowLocalY;
            PlayEffect(glowId, state.GlowAnchor.transform, "ground-glow");
            _layers.ApplyIgnoreRaycastLayer(state.GlowAnchor);
        }

        state.EndTime = duration > 0f ? Time.time + duration : float.PositiveInfinity;
        _groundGlowByItemId[itemObjectId] = state;
    }

    void StopGroundGlow(int itemObjectId)
    {
        if (!_groundGlowByItemId.TryGetValue(itemObjectId, out GroundGlowState state))
            return;

        if (state.GlowAnchor != null)
            Object.Destroy(state.GlowAnchor);
        _groundGlowByItemId.Remove(itemObjectId);
    }

    void PlayLandBurst(int itemObjectId, Transform itemTransform)
    {
        StopLandBurst(itemObjectId);
        if (itemTransform == null || ItemDropPresentationIds.LandBurstEffectId <= 0)
            return;

        EffectManager effects = IncomingPacketActions.Effects;
        if (effects == null)
            return;

        var anchor = new GameObject("HitPointProxy");
        Vector3 grounded = GroundSnapHelper.SnapToGroundOrKeep(itemTransform.position);
        if (ItemDropPresentationIds.LandBurstLocalY != 0f)
            grounded += Vector3.up * ItemDropPresentationIds.LandBurstLocalY;
        anchor.transform.SetParent(itemTransform, true);
        anchor.transform.position = grounded;
        _landBurstByItemId[itemObjectId] = anchor;
        effects.PlayEffect(ItemDropPresentationIds.LandBurstEffectId, anchor.transform);
        _layers.ApplyIgnoreRaycastLayer(anchor);
    }

    void StopLandBurst(int itemObjectId)
    {
        if (!_landBurstByItemId.TryGetValue(itemObjectId, out GameObject anchor))
            return;

        _landBurstByItemId.Remove(itemObjectId);
        if (anchor != null)
            Object.Destroy(anchor);
    }

    void StopThrow(int itemObjectId)
    {
        for (int i = _throws.Count - 1; i >= 0; i--)
        {
            if (_throws[i].ItemObjectId != itemObjectId)
                continue;

            SetSpinEnabled(_throws[i].Item, false);
            _throws.RemoveAt(i);
        }
    }

    static void SetSpinEnabled(ItemEntity item, bool enabled)
    {
        ItemPickupMotion motion = item != null ? item.GetComponent<ItemPickupMotion>() : null;
        if (motion != null)
            motion.enabled = enabled;
    }

    static Vector3 ResolveThrowStart(Entity dropper, Vector3 landPos)
    {
        if (dropper != null)
        {
            Transform hand = dropper.GetWeaponTransform();
            if (hand == null && dropper.Gear != null)
                hand = dropper.Gear.GetTransformRightHandBone();
            if (hand != null)
                return hand.position;

            float ch = dropper.Appearance != null
                ? L2NameplateAnchor.CollisionHeightToUnityMeters(dropper.Appearance.CollisionHeight)
                : L2NameplateAnchor.DefaultCollisionHeightMeters;
            return dropper.transform.position + Vector3.up * (ch * 1.15f);
        }

        return landPos + Vector3.up * ItemDropPresentationIds.DropThrowStartHeightMeters;
    }

    static Entity ResolveEntity(int objectId)
    {
        if (objectId == 0 || IncomingPacketActions.GameWorld == null)
            return null;
        return IncomingPacketActions.GameWorld.GetEntityNoLockSync(objectId);
    }

    static void PlayActorAnimation(int objectId, string trigger)
    {
        if (objectId == 0 || IncomingPacketActions.Animations == null)
            return;
        IncomingPacketActions.Animations.PlayAnimationTrigger(objectId, trigger);
    }

    static void PlayActorCrossFade(int objectId, string stateName)
    {
        if (objectId == 0 || IncomingPacketActions.Animations == null)
            return;
        IncomingPacketActions.Animations.PlayExactAnimatorState(objectId, stateName);
    }

    static void PlayEffect(int effectId, Transform target, string label)
    {
        if (effectId <= 0 || target == null)
        {
            Debug.Log($"[ItemDropFx] effect stub ({label}) id={effectId} — assign in ItemDropPresentationIds");
            return;
        }

        EffectManager effects = IncomingPacketActions.Effects;
        if (effects == null)
            return;

        Transform playTarget = target;
        if (NeedsLandBurstHeight(effectId))
        {
            var anchor = new GameObject("HitPointProxy");
            Vector3 grounded = GroundSnapHelper.SnapToGroundOrKeep(target.position);
            if (ItemDropPresentationIds.LandBurstLocalY != 0f)
                grounded += Vector3.up * ItemDropPresentationIds.LandBurstLocalY;
            anchor.transform.SetParent(target, true);
            anchor.transform.position = grounded;
            playTarget = anchor.transform;
        }

        effects.PlayEffect(effectId, playTarget);
    }

    static void PlayEffect(int effectId, Vector3 point, string label)
    {
        if (effectId <= 0)
        {
            Debug.Log($"[ItemDropFx] effect stub ({label}) id={effectId} — assign in ItemDropPresentationIds");
            return;
        }

        EffectManager effects = IncomingPacketActions.Effects;
        if (effects == null)
            return;

        // Parent must outlive the FX. EffectManager.PlayEffect instantiates as a child
        // of target; BaseEffect.DestoryEffect also destroys a HitPointProxy parent.
        var anchor = new GameObject("HitPointProxy");
        Vector3 spawn = GroundSnapHelper.SnapToGroundOrKeep(point);
        if (NeedsLandBurstHeight(effectId) && ItemDropPresentationIds.LandBurstLocalY != 0f)
            spawn += Vector3.up * ItemDropPresentationIds.LandBurstLocalY;
        anchor.transform.SetPositionAndRotation(spawn, Quaternion.identity);
        effects.PlayEffect(effectId, anchor.transform);
    }

    static bool NeedsLandBurstHeight(int effectId)
    {
        return effectId == ItemDropPresentationIds.LandBurstEffectId
            || effectId == ItemDropPresentationIds.CoinSparkleEffectId;
    }

    void HideCoinDropMeshUntilPile(ItemEntity item, int itemObjectId)
    {
        SetDropMeshActive(item, false);
        CancelCoinMeshReveal(itemObjectId);
        _coinRevealByItemId[itemObjectId] = new CoinMeshRevealState
        {
            Item = item,
            RevealTime = Time.time + ItemDropPresentationIds.CoinDropMeshRevealDelaySeconds
        };
    }

    void TickCoinMeshReveals()
    {
        if (_coinRevealByItemId.Count == 0)
            return;

        float now = Time.time;
        _expiredCoinRevealIds.Clear();
        foreach (KeyValuePair<int, CoinMeshRevealState> pair in _coinRevealByItemId)
        {
            if (pair.Value.RevealTime <= now)
                _expiredCoinRevealIds.Add(pair.Key);
        }

        for (int i = 0; i < _expiredCoinRevealIds.Count; i++)
            RevealCoinDropMesh(_expiredCoinRevealIds[i]);
    }

    void RevealCoinDropMesh(int itemObjectId)
    {
        if (!_coinRevealByItemId.TryGetValue(itemObjectId, out CoinMeshRevealState state))
            return;

        _coinRevealByItemId.Remove(itemObjectId);
        if (state.Item == null)
            return;

        SetDropMeshActive(state.Item, true);
        _clickArea.Refresh(state.Item);
        ItemPickupMotion motion = state.Item.GetComponent<ItemPickupMotion>();
        if (motion != null)
            motion.BeginSpin(itemObjectId);
    }

    void CancelCoinMeshReveal(int itemObjectId)
    {
        _coinRevealByItemId.Remove(itemObjectId);
    }

    static void SetDropMeshActive(ItemEntity item, bool active)
    {
        if (item == null)
            return;
        Transform dropMesh = item.transform.Find("DropMesh");
        if (dropMesh != null)
            dropMesh.gameObject.SetActive(active);
    }

    static int ResolveFallEffectId(ItemDropVisualKind kind)
    {
        switch (kind)
        {
            case ItemDropVisualKind.Weapon:
                return ItemDropPresentationIds.WeaponFallTrailEffectId;
            case ItemDropVisualKind.EtcStackable:
                return ItemDropPresentationIds.CoinSparkleEffectId;
            default:
                return 0;
        }
    }

    static int ResolveGroundGlowEffectId(ItemDropVisualKind kind)
    {
        return ItemDropPresentationIds.GroundGlowEffectId;
    }

    ItemDropVisualKind ResolveVisualKind(ItemEntity item)
    {
        if (item == null)
            return ItemDropVisualKind.Generic;

        int itemId = item.ItemId;
        if (_grp.IsAdenaDropVisual(itemId))
            return ItemDropVisualKind.Adena;

        ItemTable table = ItemTable.Instance;
        if (table == null)
            return item.Stackable ? ItemDropVisualKind.EtcStackable : ItemDropVisualKind.Generic;
        if (table.GetWeapon(itemId) != null)
            return ItemDropVisualKind.Weapon;

        EtcItem etc = table.GetEtcItem(itemId);
        if (etc != null && etc.EtcItemgrp != null)
        {
            ConsumeCategory consume = etc.EtcItemgrp.ConsumeType;
            if (item.Stackable || consume == ConsumeCategory.Stackable)
                return ItemDropVisualKind.EtcStackable;
        }
        else if (item.Stackable)
        {
            return ItemDropVisualKind.EtcStackable;
        }

        if (table.GetArmor(itemId) != null)
            return ItemDropVisualKind.Armor;

        return ItemDropVisualKind.Generic;
    }
}
