using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Golem Crasher only: attack stance stays on while the pawn slides.
/// Console filter: [CRASHER]
/// File: C:\unity\l2 client\decompile\log\crasher.txt
/// </summary>
public static class CrasherSlideLog
{
    const string LogPath = @"C:\unity\l2 client\decompile\log\crasher.txt";
    const float SyncSec = 0.25f;

    static readonly Dictionary<int, float> LastSyncAt = new Dictionary<int, float>(4);
    static readonly StringBuilder Line = new StringBuilder(512);

    public static bool Enabled { get; set; } = true;

    public static bool IsWatch(Entity entity)
    {
        if (!Enabled || entity == null || !(entity is MonsterEntity) || entity.Identity == null)
            return false;
        return ContainsCrasher(entity.Identity.Name)
            || ContainsCrasher(entity.Identity.Title)
            || ContainsCrasher(entity.Identity.NpcClass);
    }

    public static void Event(Entity entity, string tag, string detail = null)
    {
        if (!IsWatch(entity))
            return;
        Write(entity, tag, detail, dumpAnim: true);
    }

    public static void EventId(int objectId, string tag, string detail = null)
    {
        Event(EntityActionSkill.ResolveEntity(objectId), tag, detail);
    }

    /// <summary>While the pawn is translating. Throttled.</summary>
    public static void Sync(Entity entity)
    {
        if (!IsWatch(entity))
            return;
        int id = entity.Identity.Id;
        float now = Time.unscaledTime;
        float last;
        if (LastSyncAt.TryGetValue(id, out last) && now - last < SyncSec)
            return;
        LastSyncAt[id] = now;
        bool moving = MoveAllCharacters.Instance != null && MoveAllCharacters.Instance.IsMoving(id);
        Write(entity, "sync", "isMoving=" + moving, dumpAnim: true);
    }

    static bool ContainsCrasher(string value)
    {
        return !string.IsNullOrEmpty(value) &&
               value.IndexOf("crasher", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void Write(Entity entity, string tag, string detail, bool dumpAnim)
    {
        int id = entity.Identity.Id;
        Line.Length = 0;
        Line.Append("[CRASHER] t=").Append(Time.unscaledTime.ToString("F3"))
            .Append(" frame=").Append(Time.frameCount)
            .Append(" ").Append(tag)
            .Append(" name=").Append(entity.Identity.Name ?? "-")
            .Append(" class=").Append(entity.Identity.NpcClass ?? "-")
            .Append(" id=").Append(id)
            .Append(" action=").Append(entity.ActionSlot != null ? entity.ActionSlot.Action.ToString() : "?")
            .Append(" inCombat=").Append(entity.InCombat)
            .Append(" running=").Append(entity.Running)
            .Append(" dead=").Append(entity.IsDead());
        if (!string.IsNullOrEmpty(detail))
            Line.Append(" | ").Append(detail);
        if (dumpAnim)
            AppendAnim(entity);
        string text = Line.ToString();
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
            Debug.LogWarning("CrasherSlideLog file: " + ex.Message);
        }
    }

    static void AppendAnim(Entity entity)
    {
        NetworkAnimationController nac = entity.GetAnimatorController();
        Animator anm = nac != null ? nac.GetAnimator() : null;
        if (anm == null)
        {
            Line.Append(" | anim=null");
            return;
        }

        AnimatorClipInfo[] clips = anm.GetCurrentAnimatorClipInfo(0);
        string clip = clips != null && clips.Length > 0 && clips[0].clip != null
            ? clips[0].clip.name
            : "<none>";
        AnimatorStateInfo st = anm.GetCurrentAnimatorStateInfo(0);
        bool inTrans = anm.IsInTransition(0);
        string next = "-";
        if (inTrans)
        {
            AnimatorStateInfo nxt = anm.GetNextAnimatorStateInfo(0);
            next = Named(nxt);
        }

        BaseAnimationController bac = nac as BaseAnimationController;
        Line.Append(" | state=").Append(Named(st))
            .Append(" next=").Append(next)
            .Append(" clip=").Append(clip)
            .Append(" normT=").Append(st.normalizedTime.ToString("F3"))
            .Append(" speed=").Append(anm.speed.ToString("F2"))
            .Append(" inTrans=").Append(inTrans)
            .Append(" cull=").Append(anm.cullingMode)
            .Append(" hasRun=").Append(anm.HasState(0, Animator.StringToHash("run")))
            .Append(" hasWalk=").Append(anm.HasState(0, Animator.StringToHash("walk")))
            .Append(" hasAtkWait=").Append(anm.HasState(0, Animator.StringToHash("atkwait")))
            .Append(" castClip=").Append(bac != null && bac.IsCastClipActive);
    }

    static string Named(AnimatorStateInfo state)
    {
        if (state.IsName("run")) return "run";
        if (state.IsName("walk")) return "walk";
        if (state.IsName("wait")) return "wait";
        if (state.IsName("atkwait")) return "atkwait";
        if (state.IsName("atk01")) return "atk01";
        if (state.IsName("spatk01")) return "spatk01";
        if (state.IsName("death")) return "death";
        return "other";
    }
}
