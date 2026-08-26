using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// Body alpha for CharInfo users and city NPCs: CreateSkin fade-in, FNDisappearAction fade-out.
/// </summary>
public sealed class AppearFadeService : ITickable
{
    readonly L2ActorFade _fade;
    readonly Dictionary<int, Entry> _appear = new Dictionary<int, Entry>();
    readonly Dictionary<int, Entry> _disappear = new Dictionary<int, Entry>();
    readonly List<int> _remove = new List<int>();

    public AppearFadeService(L2ActorFade fade)
    {
        _fade = fade;
    }

    public void Begin(Entity entity)
    {
        if (_fade == null || entity == null || entity.Identity == null || entity.IsDead())
        {
            return;
        }

        int id = entity.Identity.Id;
        if (_appear.ContainsKey(id))
        {
            return;
        }

        Entry entry = new Entry(entity);
        if (!entry.Apply(_fade, L2ActorFade.AppearStartAlpha))
        {
            return;
        }

        _appear.Add(id, entry);
    }

    public void BeginDisappear(Entity entity, Action<GameObject> onFinished)
    {
        if (_fade == null || entity == null || entity.gameObject == null)
        {
            InvokeFinished(onFinished, entity);
            return;
        }

        int objectId = entity.Identity != null ? entity.Identity.Id : 0;
        Entry entry;
        if (objectId != 0 && _appear.TryGetValue(objectId, out entry) && entry.Entity == entity)
        {
            _appear.Remove(objectId);
            entry.OnFinished = onFinished;
            StartDisappearFromCurrent(entry);
            _disappear[entity.gameObject.GetInstanceID()] = entry;
            return;
        }

        int goId = entity.gameObject.GetInstanceID();
        if (_disappear.ContainsKey(goId))
        {
            return;
        }

        entry = new Entry(entity);
        entry.OnFinished = onFinished;
        if (!entry.Apply(_fade, 255))
        {
            InvokeFinished(onFinished, entity);
            return;
        }

        _disappear.Add(goId, entry);
    }

    public void Cancel(int id)
    {
        Entry entry;
        if (!_appear.TryGetValue(id, out entry))
        {
            return;
        }

        entry.Restore(_fade);
        _appear.Remove(id);
    }

    /// <summary>
    /// Gear refresh replaces renderer materials. Restore fade instances first, then
    /// re-apply the shader at the same elapsed alpha so CharInfo updates do not abort the ramp.
    /// </summary>
    public void AroundVisualRefresh(Entity entity, Action refresh)
    {
        if (refresh == null)
        {
            return;
        }

        Entry entry;
        bool fading = TryGetAppear(entity, out entry);
        if (fading)
        {
            entry.Restore(_fade);
        }

        refresh();

        if (!fading)
        {
            return;
        }

        if (!entry.IsValid() || !entry.Apply(_fade, _fade.AppearAlphaByte(entry.Elapsed)))
        {
            _appear.Remove(entry.Id);
        }
    }

    public void Tick()
    {
        TickDict(_appear, true);
        TickDict(_disappear, false);
    }

    void TickDict(Dictionary<int, Entry> dict, bool appear)
    {
        if (dict.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<int, Entry> kvp in dict)
        {
            Entry entry = kvp.Value;
            if (entry == null || !entry.IsValid())
            {
                _remove.Add(kvp.Key);
                continue;
            }

            entry.Elapsed += Time.deltaTime;
            if (entry.Elapsed >= L2ActorFade.DurationSeconds)
            {
                if (appear)
                {
                    entry.Restore(_fade);
                }
                else
                {
                    InvokeFinished(entry.OnFinished, entry.Entity);
                }

                _remove.Add(kvp.Key);
                continue;
            }

            byte alpha = appear
                ? _fade.AppearAlphaByte(entry.Elapsed)
                : _fade.AlphaByte(entry.Elapsed);
            entry.SetAlpha(_fade, alpha);
        }

        for (int i = 0; i < _remove.Count; i++)
        {
            dict.Remove(_remove[i]);
        }

        _remove.Clear();
    }

    void StartDisappearFromCurrent(Entry entry)
    {
        byte current = _fade.AppearAlphaByte(entry.Elapsed);
        entry.Elapsed = (255f - current) / L2ActorFade.AlphaPerSecond;
        entry.SetAlpha(_fade, current);
    }

    bool TryGetAppear(Entity entity, out Entry entry)
    {
        entry = null;
        if (entity == null || entity.Identity == null)
        {
            return false;
        }

        return _appear.TryGetValue(entity.Identity.Id, out entry);
    }

    static void InvokeFinished(Action<GameObject> onFinished, Entity entity)
    {
        if (onFinished == null)
        {
            return;
        }

        onFinished(entity != null ? entity.gameObject : null);
    }

    sealed class Entry
    {
        readonly Entity _entity;
        Renderer[] _renderers;
        Material[][] _instances;
        Material[][] _sharedBackup;
        bool _applied;

        public Entry(Entity entity)
        {
            _entity = entity;
        }

        public Entity Entity
        {
            get { return _entity; }
        }

        public Action<GameObject> OnFinished { get; set; }

        public int Id
        {
            get { return _entity != null && _entity.Identity != null ? _entity.Identity.Id : 0; }
        }

        public float Elapsed { get; set; }

        public bool IsValid()
        {
            return _entity != null && _entity.gameObject != null;
        }

        public bool Apply(L2ActorFade fade, byte startAlpha)
        {
            Restore(fade);
            if (!IsValid() || fade == null)
            {
                return false;
            }

            _applied = fade.TryBegin(_entity, startAlpha, out _renderers, out _instances, out _sharedBackup);
            return _applied;
        }

        public void SetAlpha(L2ActorFade fade, byte alphaByte)
        {
            if (fade != null)
            {
                fade.SetAlphaByte(_instances, alphaByte);
            }
        }

        public void Restore(L2ActorFade fade)
        {
            if (!_applied)
            {
                return;
            }

            if (fade != null)
            {
                fade.Restore(_renderers, _sharedBackup, _instances);
            }

            _applied = false;
            _renderers = null;
            _instances = null;
            _sharedBackup = null;
        }
    }
}
