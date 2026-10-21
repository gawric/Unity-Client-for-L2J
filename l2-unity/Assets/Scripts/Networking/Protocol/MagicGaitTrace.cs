using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Deep anim/gait trace for Human Magic CharInfo only (MMagic / FMagic).
/// Covers MoveToPawn AND post-MagicShot return (cast playable → FinishRemoteCast → idle/gait).
/// Filter console: [MAGIC_GAIT]
/// File: C:\unity\l2 client\decompile\log\magic-gait.txt
/// </summary>
public static class MagicGaitTrace
{
    const string LogPath = @"C:\unity\l2 client\decompile\log\magic-gait.txt";

    // Per-id throttle only for high-frequency sync ticks.
    static readonly Dictionary<int, float> LastSyncAt = new Dictionary<int, float>(32);
    static readonly Dictionary<int, float> PrevNorm = new Dictionary<int, float>(32);
    // After MagicShot finish — denser sync for a few seconds to catch freeze.
    static readonly Dictionary<int, float> WatchUntil = new Dictionary<int, float>(32);
    static readonly StringBuilder Sb = new StringBuilder(1024);

    public static bool Enabled { get; set; } = false;

    public static bool IsMagic(Entity entity)
    {
        if (entity == null)
            return false;
        CharacterRaceAnimation race = entity.RaceId;
        return race == CharacterRaceAnimation.MMagic || race == CharacterRaceAnimation.FMagic;
    }

    public static Entity Resolve(int objectId)
    {
        return EntityActionSkill.ResolveEntity(objectId);
    }

    public static void Event(Entity entity, string tag, string detail = null)
    {
        if (!Enabled || !IsMagic(entity) || entity.Identity == null)
            return;
        Write(entity, tag, detail, dumpAnim: true, stack: true);
    }

    public static void EventId(int objectId, string tag, string detail = null)
    {
        Entity entity = Resolve(objectId);
        Event(entity, tag, detail);
    }

    /// <summary>High-frequency path (SyncUserGait every frame). Throttled + no stack.</summary>
    public static void SyncTick(Entity entity, string detail)
    {
        if (!Enabled || !IsMagic(entity) || entity.Identity == null)
            return;
        int id = entity.Identity.Id;
        float now = Time.unscaledTime;
        float interval = 0.1f;
        float watch;
        if (WatchUntil.TryGetValue(id, out watch) && now < watch)
            interval = 0.05f;
        float last;
        if (LastSyncAt.TryGetValue(id, out last) && now - last < interval)
            return;
        LastSyncAt[id] = now;
        Write(entity, "sync", detail, dumpAnim: true, stack: false);
    }

    public static void Decision(Entity entity, string tag, string detail)
    {
        if (!Enabled || !IsMagic(entity) || entity.Identity == null)
            return;
        Write(entity, tag, detail, dumpAnim: true, stack: false);
    }

    /// <summary>Call when MagicShot / cast ends so sync samples denser for ~4s.</summary>
    public static void BeginPostShotWatch(Entity entity)
    {
        if (!Enabled || !IsMagic(entity) || entity.Identity == null)
            return;
        WatchUntil[entity.Identity.Id] = Time.unscaledTime + 4f;
        Event(entity, "post-shot-WATCH", "dense-sync 4s");
    }

    static void Write(Entity entity, string tag, string detail, bool dumpAnim, bool stack)
    {
        int id = entity.Identity.Id;
        string nick = entity.Identity.Name ?? ("#" + id);
        UserEntity user = entity as UserEntity;

        Sb.Length = 0;
        Sb.Append("[MAGIC_GAIT] t=").Append(Time.unscaledTime.ToString("F3"))
            .Append(" frame=").Append(Time.frameCount)
            .Append(" ").Append(tag)
            .Append(" nick=").Append(nick)
            .Append(" id=").Append(id)
            .Append(" race=").Append(entity.RaceId)
            .Append(" weapon=").Append(user != null ? user.WeaponAnim : "?")
            .Append(" running=").Append(entity.Running)
            .Append(" inCombat=").Append(entity.InCombat)
            .Append(" action=").Append(entity.ActionSlot != null ? entity.ActionSlot.Action.ToString() : "?")
            .Append(" attackVis=").Append(user != null && user.IsAttackVisualPlaying())
            .Append(" visLeft=").Append(user != null ? user.AttackVisualLeftSec().ToString("F3") : "-")
            .Append(" isMoving=").Append(
                MoveAllCharacters.Instance != null && MoveAllCharacters.Instance.IsMoving(id));

        MagicCastData cast = entity.GetMagicCastData();
        if (cast != null)
        {
            Sb.Append(" speedShot=").Append(cast.SpeedShot.ToString("F3"))
                .Append(" speedMid=").Append(cast.SpeedMid.ToString("F3"))
                .Append(" speedEnd=").Append(cast.SpeedEnd.ToString("F3"));
        }

        if (!string.IsNullOrEmpty(detail))
            Sb.Append(" | ").Append(detail);

        if (dumpAnim)
            AppendAnimatorDump(Sb, user, id);

        if (stack)
        {
            string st = new StackTrace(2, false).ToString();
            Sb.Append(" | stack=");
            AppendShortStack(Sb, st);
        }

        string text = Sb.ToString();
        Debug.Log(text);

        try
        {
            string dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(LogPath, text + "\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("MagicGaitTrace file: " + ex.Message);
        }
    }

    static void AppendAnimatorDump(StringBuilder sb, UserEntity user, int id)
    {
        if (user == null)
            return;
        NetworkAnimationController nac = user.GetAnimatorController();
        Animator anm = nac != null ? nac.GetAnimator() : null;
        if (anm == null)
        {
            sb.Append(" | anim=null");
            return;
        }

        string want = "run_" + user.WeaponAnim;
        AnimatorStateInfo cur = anm.GetCurrentAnimatorStateInfo(0);
        AnimatorClipInfo[] clips = anm.GetCurrentAnimatorClipInfo(0);
        string clip = clips != null && clips.Length > 0 && clips[0].clip != null
            ? clips[0].clip.name
            : "<none>";
        float norm = cur.normalizedTime;
        float normD = 0f;
        float prev;
        if (PrevNorm.TryGetValue(id, out prev))
            normD = norm - prev;
        PrevNorm[id] = norm;

        bool inTrans = anm.IsInTransition(0);
        string nextClip = "-";
        if (inTrans)
        {
            AnimatorClipInfo[] next = anm.GetNextAnimatorClipInfo(0);
            if (next != null && next.Length > 0 && next[0].clip != null)
                nextClip = next[0].clip.name;
        }

        bool already = PlayerLocomotionCrossFade.IsAlreadyPlaying(anm, want);
        bool hasState = anm.HasState(0, Animator.StringToHash(want));
        var ctrl = anm.runtimeAnimatorController;
        string ctrlName = ctrl != null ? ctrl.name : "null";

        bool castActive = false;
        BaseAnimationController bac = nac as BaseAnimationController;
        if (bac != null)
            castActive = bac.IsCastClipActive;

        sb.Append(" | want=").Append(want)
            .Append(" hasState=").Append(hasState)
            .Append(" already=").Append(already)
            .Append(" inTrans=").Append(inTrans)
            .Append(" clip=").Append(clip)
            .Append(" nextClip=").Append(nextClip)
            .Append(" normT=").Append(norm.ToString("F3"))
            .Append(" normD=").Append(normD.ToString("F3"))
            .Append(" animSpeed=").Append(anm.speed.ToString("F2"))
            .Append(" runSpeed=").Append(anm.GetFloat("run_speed").ToString("F3"))
            .Append(" walkSpeed=").Append(anm.GetFloat("walk_speed").ToString("F3"))
            .Append(" castClip=").Append(castActive)
            .Append(" cull=").Append(anm.cullingMode)
            .Append(" enabled=").Append(anm.enabled)
            .Append(" ctrl=").Append(ctrlName);
    }

    static void AppendShortStack(StringBuilder sb, string full)
    {
        if (string.IsNullOrEmpty(full))
            return;
        int kept = 0;
        int start = 0;
        while (kept < 5 && start < full.Length)
        {
            int nl = full.IndexOf('\n', start);
            if (nl < 0)
                nl = full.Length;
            string line = full.Substring(start, nl - start).Trim();
            start = nl + 1;
            if (line.Length == 0)
                continue;
            if (line.IndexOf("MagicGaitTrace", StringComparison.Ordinal) >= 0)
                continue;
            if (line.IndexOf("Assets", StringComparison.Ordinal) < 0 &&
                line.IndexOf("EntityAction", StringComparison.Ordinal) < 0 &&
                line.IndexOf("MovementData", StringComparison.Ordinal) < 0 &&
                line.IndexOf("MoveAll", StringComparison.Ordinal) < 0 &&
                line.IndexOf("WorldApply", StringComparison.Ordinal) < 0 &&
                line.IndexOf("CastClip", StringComparison.Ordinal) < 0 &&
                line.IndexOf("AnimationManager", StringComparison.Ordinal) < 0 &&
                line.IndexOf("Skill", StringComparison.Ordinal) < 0 &&
                line.IndexOf("PlayerOverridden", StringComparison.Ordinal) < 0)
                continue;
            if (kept > 0)
                sb.Append(" < ");
            int at = line.IndexOf("at ", StringComparison.Ordinal);
            sb.Append(at >= 0 ? line.Substring(at + 3) : line);
            kept++;
        }
    }

    public static void Reset()
    {
        LastSyncAt.Clear();
        PrevNorm.Clear();
        WatchUntil.Clear();
    }
}
