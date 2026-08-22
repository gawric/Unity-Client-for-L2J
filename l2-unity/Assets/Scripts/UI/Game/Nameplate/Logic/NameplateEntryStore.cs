using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World nameplate entry dictionary: discover, upsert, visibility cull, paint list.
/// </summary>
public sealed class NameplateEntryStore
{
    private readonly Dictionary<int, NameplateEntry> _entries;
    private readonly List<int> _removeIds;
    private readonly List<int> _entryKeys;
    private readonly NameplateBubbleResolver _resolver;

    private int _removeObjId;

    public NameplateEntryStore(NameplateBubbleResolver resolver, int capacity = 64)
    {
        _resolver = resolver;
        _entries = new Dictionary<int, NameplateEntry>(capacity);
        _removeIds = new List<int>(32);
        _entryKeys = new List<int>(capacity);
    }

    public int Count => _entries.Count;

    public void Clear()
    {
        _entries.Clear();
    }

    public void Remove(int id, NameplatePixelSnap snap)
    {
        if (_entries.Remove(id))
        {
            _removeObjId = id;
            snap?.Clear(id);
        }
    }

    public void Discover(
        RaycastHit[] hits,
        Color defaultNameColor)
    {
        if (hits == null)
        {
            return;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitT = hits[i].transform;
            if (hitT == null)
            {
                continue;
            }

            Entity entity = hitT.GetComponent<Entity>();
            if (entity == null || entity.Identity == null)
            {
                continue;
            }

            if (entity.Identity.Id == _removeObjId)
            {
                continue;
            }

            UpsertEntry(entity, defaultNameColor);
        }
    }

    public void EnsureHoverAndTarget(LayerMask entityMask, Color defaultNameColor)
    {
        if (ClickManager.Instance != null && ClickManager.Instance.HoverObjectData != null)
        {
            ObjectData hover = ClickManager.Instance.HoverObjectData;
            if (hover.ObjectTransform != null &&
                entityMask == (entityMask | (1 << hover.ObjectLayer)))
            {
                Entity e = hover.ObjectTransform.GetComponent<Entity>();
                if (e != null)
                {
                    UpsertEntry(e, defaultNameColor);
                }
            }
        }

        if (TargetManager.Instance != null &&
            TargetManager.Instance.HasTarget() &&
            TargetManager.Instance.Target?.Data?.ObjectTransform != null)
        {
            Entity e = TargetManager.Instance.Target.Data.ObjectTransform.GetComponent<Entity>();
            if (e != null)
            {
                UpsertEntry(e, defaultNameColor);
            }
        }
    }

    public void UpsertEntry(Entity entity, Color defaultNameColor)
    {
        if (entity == null || entity.Identity == null || entity.transform == null)
        {
            return;
        }

        EntityIdentity idn = entity.Identity;
        if (string.IsNullOrEmpty(idn.Name))
        {
            return;
        }

        int id = idn.Id;
        Color titleColor = defaultNameColor;
        if (!string.IsNullOrEmpty(idn.TitleColor))
        {
            titleColor = StringUtils.HexToColor(idn.TitleColor);
        }

        CharacterController cc = null;
        CapsuleCollider capsule = null;
        if (_entries.TryGetValue(id, out NameplateEntry existing))
        {
            cc = existing.CC;
            capsule = existing.Capsule;
        }

        Transform target = entity.transform;
        if (cc == null)
        {
            cc = target.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = target.GetComponentInChildren<CharacterController>();
            }
        }

        if (capsule == null && cc == null)
        {
            capsule = target.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                capsule = target.GetComponentInChildren<CapsuleCollider>();
            }
        }

        _entries[id] = new NameplateEntry
        {
            Id = id,
            Target = target,
            CC = cc,
            Capsule = capsule,
            Entity = entity,
            Name = idn.Name,
            Title = idn.Title ?? string.Empty,
            NameColor = defaultNameColor,
            TitleColor = titleColor,
            Visible = true
        };
    }

    public void RefreshVisibility(
        Transform playerTransform,
        float nameplateViewDistance,
        NameplatePixelSnap snap)
    {
        _removeIds.Clear();
        _entryKeys.Clear();
        foreach (int id in _entries.Keys)
        {
            _entryKeys.Add(id);
        }

        for (int i = 0; i < _entryKeys.Count; i++)
        {
            int id = _entryKeys[i];
            if (!_entries.TryGetValue(id, out NameplateEntry e))
            {
                continue;
            }

            if (e.Target == null)
            {
                _removeIds.Add(id);
                continue;
            }

            bool visible = IsNameplateVisible(e.Target, playerTransform, nameplateViewDistance);
            e.Visible = visible;
            _entries[id] = e;

            bool isLocal = PlayerEntity.Instance != null && e.Entity == PlayerEntity.Instance;
            if (!visible && !isLocal && !_resolver.IsHoverOrTarget(e.Target))
            {
                _removeIds.Add(id);
            }
        }

        for (int i = 0; i < _removeIds.Count; i++)
        {
            int rid = _removeIds[i];
            _entries.Remove(rid);
            snap?.Clear(rid);
        }
    }

    public void BuildPaintList(
        List<NameplatePaintItem> paintList,
        bool drawTitles,
        float headHeightOffset)
    {
        paintList.Clear();

        foreach (KeyValuePair<int, NameplateEntry> kv in _entries)
        {
            NameplateEntry e = kv.Value;
            if (!e.Visible || e.Target == null || string.IsNullOrEmpty(e.Name))
            {
                continue;
            }

            if (e.Entity != null && e.Entity.Identity != null)
            {
                EntityIdentity idn = e.Entity.Identity;
                e.Name = idn.Name;
                e.Title = idn.Title ?? string.Empty;
                if (!string.IsNullOrEmpty(idn.TitleColor))
                {
                    e.TitleColor = StringUtils.HexToColor(idn.TitleColor);
                }
            }

            bool isLocal = PlayerEntity.Instance != null && e.Entity == PlayerEntity.Instance;
            L2TargetRenderType bubbleType = _resolver.ResolveForPaint(e.Target, isLocal);

            paintList.Add(new NameplatePaintItem
            {
                Id = e.Id,
                World = GetHeadWorldPos(e, headHeightOffset),
                Name = e.Name,
                Title = drawTitles ? e.Title : null,
                NameColor = e.NameColor,
                TitleColor = e.TitleColor,
                IsLocalPlayer = isLocal,
                BubbleType = bubbleType
            });
        }
    }

    private static Vector3 GetHeadWorldPos(NameplateEntry entry, float headHeightOffset)
    {
        float ch = L2NameplateAnchor.DefaultCollisionHeightMeters;
        Entity entity = entry.Entity;
        if (entity != null && entity.Appearance != null)
        {
            ch = entity.Appearance.CollisionHeight;
        }

        return L2NameplateAnchor.GetHeadWorldPos(
            entry.Target, entry.CC, entry.Capsule, ch, headHeightOffset);
    }

    private bool IsNameplateVisible(
        Transform target,
        Transform playerTransform,
        float nameplateViewDistance)
    {
        if (target == null || playerTransform == null)
        {
            return false;
        }

        if (PlayerEntity.Instance != null && target == PlayerEntity.Instance.transform)
        {
            return true;
        }

        if (_resolver.IsHoverOrTarget(target))
        {
            return true;
        }

        if (Vector3.Distance(playerTransform.position, target.position) > nameplateViewDistance)
        {
            return false;
        }

        if (CameraController.Instance != null)
        {
            return CameraController.Instance.IsObjectVisible(target);
        }

        return true;
    }
}
