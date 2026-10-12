using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Writes session.json in the Lineage 2 replay shape:
/// version, durationMs, localActorId, actors, events.
/// tMs is milliseconds since the first event Unity applied in this session.
/// </summary>
public static class UnityReplaySession
{
    public const string Root =
        @"C:\Users\hh-soft\source\repos\AutoLoginInterlude\ChainLogs\unity\Sessions";

    const float IdleSec = 15f;
    const float FlushSec = 1f;
    const float PollSec = 0.033f;

    sealed class Actor
    {
        public int Id;
        public bool Npc;
        public int NpcId;
        public string Name;
        public int ClassId;
        public int Sex = -1;
        public int Face = -1;
        public int HairStyle = -1;
        public int HairColor = -1;
        public int AttackItem;
        public int AttackEnchant;
        public int DefenseItem;
        public int ShieldItem;
        public bool CharInfoSet;
        public int HairAll;
        public int Head;
        public int RHand;
        public int LHand;
        public int Gloves;
        public int Chest;
        public int Legs;
        public int Feet;
        public int Cloak;
        public int HairItem;
        public int FaceItem;
        public float X;
        public float Y;
        public float Z;
        public int Heading;
    }

    sealed class AnimSlot
    {
        public string Anim = "";
        public float At;
        public float Norm;
    }

    static bool _active;
    static float _start;
    static float _lastEvent;
    static float _lastFlush;
    static float _lastPoll;
    static int _seq;
    static string _folder;
    static readonly List<Actor> _actors = new List<Actor>();
    static readonly Dictionary<int, Actor> _byId = new Dictionary<int, Actor>();
    static readonly List<string> _events = new List<string>();
    static readonly Dictionary<int, AnimSlot> _anims = new Dictionary<int, AnimSlot>();

    static UnityReplaySession()
    {
        Application.quitting += delegate { Stop("quit"); };
    }

    public static void Tick()
    {
        if (!_active)
            return;

        float now = Time.realtimeSinceStartup;
        if (now - _lastEvent >= IdleSec)
        {
            Stop("idle");
            return;
        }

        if (now - _lastPoll >= PollSec)
        {
            _lastPoll = now;
            int before = _events.Count;
            PollAnims();
            if (_events.Count != before)
                Flush();
        }
        else if (now - _lastFlush >= FlushSec)
            Flush();
    }

    public static void OnMoveTo(Entity entity, Vector3 destinationUnity)
    {
        if (!Begin(entity))
            return;
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(destinationUnity);
        AddMove("MOVE_TO_LOCATION", entity, null, 0, l2);
    }

    public static void OnMoveToPawn(Entity entity, MoveToPawnDto dto)
    {
        if (dto == null || !Begin(entity))
            return;
        Actor target = RememberId(dto.TarObjid);
        AddMove("MOVE_TO_PAWN", entity, target, dto.DistanceUu, Vector3.zero);
    }

    public static void OnStopMove(Entity entity, StopMoveDto dto)
    {
        if (dto == null || !Begin(entity))
            return;
        Add("STOP_MOVE", entity.Identity.Id, 0, delegate (StringBuilder sb)
        {
            sb.Append(",\"x\":").Append(dto.X);
            sb.Append(",\"y\":").Append(dto.Y);
            sb.Append(",\"z\":").Append(dto.Z);
        });
    }

    public static void OnAttack(Entity entity, AttackDto dto)
    {
        if (dto == null || !Begin(entity))
            return;
        RememberId(dto.TargetId);
        Add("ATTACK_HIT", entity.Identity.Id, dto.TargetId, delegate (StringBuilder sb)
        {
            sb.Append(",\"damage\":").Append(dto.Damage);
            sb.Append(",\"flags\":").Append(dto.Flags);
        });
    }

    public static void OnAttackStart(Entity entity)
    {
        if (!Begin(entity))
            return;
        Add("ATTACK_START", entity.Identity.Id, 0, null);
    }

    public static void OnAttackStop(Entity entity)
    {
        if (!Begin(entity))
            return;
        Add("ATTACK_STOP", entity.Identity.Id, 0, null);
    }

    public static void OnDie(Entity entity)
    {
        if (!Begin(entity))
            return;
        Add("DIE_OBSERVED", entity.Identity.Id, 0, null);
    }

    public static void OnStatus(Entity entity, List<StatusUpdate.Attribute> attributes)
    {
        if (entity == null || attributes == null)
            return;
        for (int i = 0; i < attributes.Count; i++)
        {
            StatusUpdate.Attribute attr = attributes[i];
            if ((StatusUpdate.AttributeType)attr.id != StatusUpdate.AttributeType.CUR_HP)
                continue;
            if (!Begin(entity))
                return;
            int hp = attr.value;
            Add("HP_SET", entity.Identity.Id, 0, delegate (StringBuilder sb)
            {
                sb.Append(",\"hp\":").Append(hp);
            });
        }
    }

    public static void OnMagicSkillUse(Entity entity, MagicSkillUseDto dto)
    {
        if (dto == null || !Begin(entity))
            return;
        RememberId(dto.TargetId);
        Add("MAGIC_SKILL_USE", entity.Identity.Id, dto.TargetId, delegate (StringBuilder sb)
        {
            sb.Append(",\"skillId\":").Append(dto.SkillId);
            sb.Append(",\"skillLvl\":").Append(dto.SkillLvl);
            sb.Append(",\"hitTime\":").Append(dto.HitTime);
            sb.Append(",\"reuseDelay\":").Append(dto.Reusedelay);
        });
    }

    public static void OnMagicLaunched(Entity entity, MagicSkillLaunchedDto dto)
    {
        if (dto == null || !Begin(entity))
            return;
        int targetId = 0;
        if (dto.TargetArray != null && dto.TargetArray.Length > 0)
        {
            targetId = dto.TargetArray[0];
            RememberId(targetId);
        }
        Add("MAGIC_LAUNCHED", entity.Identity.Id, targetId, delegate (StringBuilder sb)
        {
            sb.Append(",\"skillId\":").Append(dto.SkillId);
            sb.Append(",\"skillLvl\":").Append(dto.SkillLvl);
        });
    }

    public static void OnBowShot(Entity attacker, Entity target, Vector3 releaseUnity)
    {
        if (!Begin(attacker))
            return;
        int targetId = 0;
        if (target != null && target.Identity != null)
        {
            Remember(target);
            targetId = target.Identity.Id;
        }
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(releaseUnity);
        float unityFps = ClipFps(attacker, null);
        Add("BOW_SHOT", attacker.Identity.Id, targetId, delegate (StringBuilder sb)
        {
            AppendBowFormula(sb, attacker, unityFps);
            sb.Append(",\"x\":").Append(Num(l2.x));
            sb.Append(",\"y\":").Append(Num(l2.y));
            sb.Append(",\"z\":").Append(Num(l2.z));
        });
    }

    public static void OnMagicShot(Entity caster, Entity target, Vector3 positionL2)
    {
        if (!Begin(caster))
            return;
        int targetId = 0;
        if (target != null && target.Identity != null)
        {
            Remember(target);
            targetId = target.Identity.Id;
        }
        Add("MAGIC_SHOT", caster.Identity.Id, targetId, delegate (StringBuilder sb)
        {
            sb.Append(",\"x\":").Append(Num(positionL2.x));
            sb.Append(",\"y\":").Append(Num(positionL2.y));
            sb.Append(",\"z\":").Append(Num(positionL2.z));
        });
    }

    public static void OnMagicHit(Entity caster, Entity target, Vector3 positionL2)
    {
        if (!Begin(caster))
            return;
        int targetId = 0;
        if (target != null && target.Identity != null)
        {
            Remember(target);
            targetId = target.Identity.Id;
        }
        Add("MAGIC_HIT", caster.Identity.Id, targetId, delegate (StringBuilder sb)
        {
            sb.Append(",\"x\":").Append(Num(positionL2.x));
            sb.Append(",\"y\":").Append(Num(positionL2.y));
            sb.Append(",\"z\":").Append(Num(positionL2.z));
        });
    }

    static void PollAnims()
    {
        for (int i = 0; i < _actors.Count; i++)
        {
            Entity entity = Find(_actors[i].Id);
            if (entity == null)
                continue;
            string clip;
            float norm;
            if (!CurrentClip(entity, out clip, out norm))
                continue;
            string state = Classify(clip);
            if (state == null)
                continue;
            if (!AcceptAnim(entity.Identity.Id, state, clip, norm))
                continue;
            string anim = ShortAnim(clip);
            string logged = state;
            bool bowAttack = logged == "ATTACK" && clip.IndexOf("bow", StringComparison.OrdinalIgnoreCase) >= 0;
            float unityFps = bowAttack ? ClipFps(entity, clip) : 0f;
            Add("STATE", entity.Identity.Id, 0, delegate (StringBuilder sb)
            {
                sb.Append(",\"state\":").Append(Quote(logged));
                sb.Append(",\"anim\":").Append(Quote(anim));
                if (bowAttack)
                    AppendBowFormula(sb, entity, unityFps);
            });
        }
    }

    static bool AcceptAnim(int actorId, string state, string anim, float norm)
    {
        AnimSlot slot;
        if (!_anims.TryGetValue(actorId, out slot))
        {
            _anims[actorId] = new AnimSlot { Anim = anim, At = Time.realtimeSinceStartup, Norm = norm };
            return true;
        }
        bool same = slot.Anim == anim;
        bool replay = state == "ATTACK" || state == "ATTACK_WAIT" ||
            state == "CAST" || state == "CAST_WAIT" || state == "DEATH";
        bool restarted = replay && norm + 0.25f < slot.Norm;
        if (same && !restarted)
        {
            slot.Norm = norm;
            return false;
        }
        slot.Anim = anim;
        slot.At = Time.realtimeSinceStartup;
        slot.Norm = norm;
        return true;
    }

    static bool CurrentClip(Entity entity, out string name, out float norm)
    {
        name = null;
        norm = 0f;
        BaseAnimationController controller = entity.GetComponentInChildren<BaseAnimationController>(true);
        Animator animator = controller != null
            ? controller.GetComponentInChildren<Animator>(true)
            : entity.GetComponentInChildren<Animator>(true);
        if (animator == null)
            return false;

        string fallback = null;
        float fallbackNorm = 0f;
        int layers = animator.layerCount < 3 ? animator.layerCount : 3;
        for (int layer = 0; layer < layers; layer++)
        {
            bool next;
            string clipName = ClipOnLayer(animator, layer, out next);
            string state = Classify(clipName);
            if (state == null)
                continue;
            float layerNorm = next
                ? animator.GetNextAnimatorStateInfo(layer).normalizedTime
                : animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            if (state == "ATTACK" || state == "ATTACK_WAIT" ||
                state == "CAST" || state == "CAST_WAIT" || state == "DEATH")
            {
                name = clipName;
                norm = layerNorm;
                return true;
            }
            if (fallback == null)
            {
                fallback = clipName;
                fallbackNorm = layerNorm;
            }
        }
        if (fallback == null)
            return false;
        name = fallback;
        norm = fallbackNorm;
        return true;
    }

    static string ClipOnLayer(Animator animator, int layer, out bool next)
    {
        next = false;
        if (animator.IsInTransition(layer))
        {
            AnimatorClipInfo[] nextClips = animator.GetNextAnimatorClipInfo(layer);
            if (nextClips != null && nextClips.Length > 0 && nextClips[0].clip != null)
            {
                string nextState = Classify(nextClips[0].clip.name);
                if (nextState == "ATTACK" || nextState == "ATTACK_WAIT" ||
                    nextState == "CAST" || nextState == "CAST_WAIT" || nextState == "DEATH")
                {
                    next = true;
                    return nextClips[0].clip.name;
                }
            }
        }

        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(layer);
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
        {
            clips = animator.GetNextAnimatorClipInfo(layer);
            next = true;
        }
        if (clips == null || clips.Length == 0 || clips[0].clip == null)
            return null;
        return clips[0].clip.name;
    }

    static string Classify(string anim)
    {
        if (string.IsNullOrEmpty(anim))
            return null;
        string lower = anim.ToLowerInvariant();
        if (lower.EndsWith("_mmagic") || lower.EndsWith("_fmagic"))
            lower = lower.Substring(0, lower.Length - 7);
        if (lower.Contains("death") || lower.Contains("die"))
            return "DEATH";
        if (lower.Contains("atkwait") || lower.Contains("attackwait"))
            return "ATTACK_WAIT";
        if (lower.Contains("wait_spell") || lower.Contains("spelltohand") ||
            lower.Contains("castwait") || lower.Contains("magicwait"))
            return "CAST_WAIT";
        if (lower.Contains("cast") || lower.Contains("magic"))
            return "CAST";
        if (lower.Contains("spatk") || lower.Contains("shieldatk") || lower.Contains("attack") || lower.Contains("atk"))
            return "ATTACK";
        if (lower.Contains("run"))
            return "RUN";
        if (lower.Contains("walk"))
            return "WALK";
        if (lower.Contains("wait") || lower.Contains("stand") || lower.Contains("idle") || lower.Contains("sit"))
            return "STOP";
        return null;
    }

    static bool Begin(Entity entity)
    {
        if (entity == null || entity.Identity == null || entity.Identity.Id == 0)
            return false;
        if (!_active)
            Start();
        Remember(entity);
        _lastEvent = Time.realtimeSinceStartup;
        return true;
    }

    static void Start()
    {
        _active = true;
        _start = Time.realtimeSinceStartup;
        _lastEvent = _start;
        _lastFlush = 0f;
        _lastPoll = 0f;
        _seq = 0;
        _actors.Clear();
        _byId.Clear();
        _events.Clear();
        _anims.Clear();
        _folder = Path.Combine(Root, DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"));
        Directory.CreateDirectory(_folder);
        Debug.Log("[UnityReplay] START " + _folder);
    }

    static void Stop(string reason)
    {
        if (!_active)
            return;
        _active = false;
        Flush();
        Debug.Log("[UnityReplay] STOP " + reason + " events=" + _events.Count + " " + _folder);
    }

    static Actor Remember(Entity entity)
    {
        Actor actor;
        if (_byId.TryGetValue(entity.Identity.Id, out actor))
        {
            FillPlayer(actor, entity);
            return actor;
        }

        EntityType type = entity.Identity.EntityType;
        bool npc = type != EntityType.Player && type != EntityType.User;
        Vector3 l2 = VectorUtils.ConvertPosUnityToL2j(entity.transform.position);
        actor = new Actor
        {
            Id = entity.Identity.Id,
            Npc = npc,
            NpcId = npc ? entity.Identity.NpcId : 0,
            Name = entity.Identity.Name ?? "",
            X = l2.x,
            Y = l2.y,
            Z = l2.z,
            Heading = HeadingOf(entity.transform)
        };
        FillPlayer(actor, entity);
        _byId[actor.Id] = actor;
        _actors.Add(actor);
        return actor;
    }

    static void FillPlayer(Actor actor, Entity entity)
    {
        if (actor == null || actor.Npc || entity == null || entity.Identity == null)
            return;
        PlayerAppearance appearance = entity.Appearance as PlayerAppearance;
        actor.ClassId = entity.Identity.PlayerClass;
        if (appearance == null)
            return;
        actor.Sex = appearance.Sex;
        if (!appearance.PaperdollReady)
            return;
        actor.Face = appearance.Face;
        actor.HairStyle = appearance.HairStyle;
        actor.HairColor = appearance.HairColor;
        actor.AttackEnchant = appearance.WeaponEnchant;
        PaperDollSelection paper = appearance.PaperDoll;
        actor.CharInfoSet = true;
        actor.HairAll = paper.Item_Under;
        actor.Head = paper.Item_Head;
        actor.RHand = paper.Item_RHand;
        actor.LHand = paper.Item_LHand;
        actor.Gloves = paper.Item_Gloves;
        actor.Chest = paper.Item_Chest;
        actor.Legs = paper.Item_Legs;
        actor.Feet = paper.Item_Feet;
        actor.Cloak = paper.Item_Cloak;
        actor.HairItem = paper.Item_Hair;
        actor.FaceItem = paper.Item_Face;
        actor.AttackItem = actor.RHand;
        actor.DefenseItem = actor.Chest;
        actor.ShieldItem = actor.LHand;
    }

    static Actor RememberId(int objectId)
    {
        if (objectId == 0)
            return null;
        Actor existing;
        if (_byId.TryGetValue(objectId, out existing))
            return existing;
        Entity entity = Find(objectId);
        return entity != null ? Remember(entity) : null;
    }

    static void AppendBowFormula(StringBuilder sb, Entity entity, float unityFps)
    {
        AttackTimingHelper.ResolveClientBowAnimation(
            entity,
            unityFps,
            out float atkMul,
            out float animRate,
            out float playRate,
            out float tween,
            out float motionSpeed);
        sb.Append(",\"animRate\":").Append(Num(animRate));
        sb.Append(",\"atkMul\":").Append(Num(atkMul));
        sb.Append(",\"playRate\":").Append(Num(playRate));
        sb.Append(",\"tween\":").Append(Num(tween));
        sb.Append(",\"stackRate\":").Append(Num(playRate));
        sb.Append(",\"stackTween\":").Append(Num(tween));
        sb.Append(",\"motionSpeed\":").Append(Num(motionSpeed));
        sb.Append(",\"sourceFps\":").Append(Num(30f));
        sb.Append(",\"unityFps\":").Append(Num(unityFps > 0.0001f ? unityFps : 24f));
    }

    static float ClipFps(Entity entity, string clipName)
    {
        if (entity == null)
            return 24f;
        Animator animator = entity.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
            return 24f;
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null)
            return 24f;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
                continue;
            if (clipName == null)
            {
                if (clip.name.IndexOf("bow", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    clip.name.IndexOf("atk", StringComparison.OrdinalIgnoreCase) >= 0)
                    return clip.frameRate;
                continue;
            }
            if (clip.name == clipName)
                return clip.frameRate;
        }
        return 24f;
    }

    static void AddMove(string type, Entity entity, Actor target, int distance, Vector3 destinationL2)
    {
        float run = Speed(entity, false);
        float walk = Speed(entity, true);
        bool running = entity.Running;
        int targetId = target != null ? target.Id : 0;
        Add(type, entity.Identity.Id, targetId, delegate (StringBuilder sb)
        {
            if (type == "MOVE_TO_LOCATION")
            {
                sb.Append(",\"x\":").Append(Num(destinationL2.x));
                sb.Append(",\"y\":").Append(Num(destinationL2.y));
                sb.Append(",\"z\":").Append(Num(destinationL2.z));
            }
            if (type == "MOVE_TO_PAWN")
                sb.Append(",\"distance\":").Append(distance);
            sb.Append(",\"runSpeed\":").Append(Num(run));
            sb.Append(",\"walkSpeed\":").Append(Num(walk));
            sb.Append(",\"running\":").Append(running ? "true" : "false");
        });
    }

    static void Add(string type, int actorId, int targetId, Action<StringBuilder> extra)
    {
        _seq++;
        int tMs = NowMs();
        StringBuilder sb = new StringBuilder(160);
        sb.Append("{\"seq\":").Append(_seq);
        sb.Append(",\"tMs\":").Append(tMs);
        sb.Append(",\"type\":").Append(Quote(type));
        sb.Append(",\"actor\":").Append(Quote(actorId.ToString()));
        if (targetId != 0)
            sb.Append(",\"target\":").Append(Quote(targetId.ToString()));
        if (extra != null)
            extra(sb);
        sb.Append("}");
        _events.Add(sb.ToString());
        Flush();
    }

    static float Speed(Entity entity, bool walk)
    {
        if (entity.Stats == null)
            return walk ? 80f : 115f;
        float real = walk ? entity.Stats.WalkRealSpeed : entity.Stats.RunRealSpeed;
        if (real > 1f)
            return real;
        int bas = walk ? entity.Stats.BaseWalkingSpeed : entity.Stats.BaseRunSpeed;
        return bas > 0 ? bas : (walk ? 80f : 115f);
    }

    static int HeadingOf(Transform transform)
    {
        int heading = Mathf.RoundToInt(transform.eulerAngles.y / 360f * 65536f) % 65536;
        if (heading < 0)
            heading += 65536;
        return heading;
    }

    static int NowMs()
    {
        return Mathf.Max(0, Mathf.RoundToInt((Time.realtimeSinceStartup - _start) * 1000f));
    }

    static Entity Find(int objectId)
    {
        if (objectId <= 0)
            return null;
        if (PlayerEntity.Instance != null && PlayerEntity.Instance.Identity != null &&
            PlayerEntity.Instance.Identity.Id == objectId)
            return PlayerEntity.Instance;
        World world = IncomingPacketActions.GameWorld;
        return world != null ? world.GetEntityNoLockSync(objectId) : null;
    }

    static void Flush()
    {
        _lastFlush = Time.realtimeSinceStartup;
        if (string.IsNullOrEmpty(_folder) || _events.Count == 0)
            return;

        StringBuilder sb = new StringBuilder(_events.Count * 128);
        sb.Append("{\n  \"version\": 2,\n  \"durationMs\": ");
        sb.Append(LastEventMs().ToString(CultureInfo.InvariantCulture));
        int localId = LocalActorId();
        if (localId != 0)
        {
            sb.Append(",\n  \"localActorId\": ");
            sb.Append(Quote(localId.ToString()));
        }
        sb.Append(",\n  \"actors\": [\n");
        for (int i = 0; i < _actors.Count; i++)
        {
            Actor actor = _actors[i];
            sb.Append("    {\"id\":").Append(Quote(actor.Id.ToString()));
            sb.Append(",\"kind\":").Append(Quote(actor.Npc ? "NPC" : "PLAYER"));
            sb.Append(",\"name\":").Append(Quote(actor.Name));
            if (actor.Npc)
                sb.Append(",\"npcId\":").Append(actor.NpcId);
            else
                AppendPlayer(sb, actor);
            sb.Append(",\"initial\":{\"x\":").Append(Num(actor.X));
            sb.Append(",\"y\":").Append(Num(actor.Y));
            sb.Append(",\"z\":").Append(Num(actor.Z));
            sb.Append(",\"heading\":").Append(actor.Heading).Append("}}");
            if (i + 1 < _actors.Count)
                sb.Append(",");
            sb.Append("\n");
        }
        sb.Append("  ],\n  \"events\": [\n");
        for (int i = 0; i < _events.Count; i++)
        {
            sb.Append("    ").Append(_events[i]);
            if (i + 1 < _events.Count)
                sb.Append(",");
            sb.Append("\n");
        }
        sb.Append("  ]\n}\n");

        try
        {
            Directory.CreateDirectory(_folder);
            File.WriteAllText(Path.Combine(_folder, "session.json"), sb.ToString(), new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[UnityReplay] write failed: " + e.Message);
        }
    }

    static int LocalActorId()
    {
        PlayerEntity local = PlayerEntity.Instance;
        if (local == null || local.Identity == null || local.Identity.Id == 0)
            return 0;
        return _byId.ContainsKey(local.Identity.Id) ? local.Identity.Id : 0;
    }

    static void AppendPlayer(StringBuilder sb, Actor actor)
    {
        sb.Append(",\"classId\":").Append(actor.ClassId);
        if (actor.Sex >= 0)
            sb.Append(",\"sex\":").Append(actor.Sex);
        if (actor.Face >= 0)
            sb.Append(",\"face\":").Append(actor.Face);
        if (actor.HairStyle >= 0)
            sb.Append(",\"hairStyle\":").Append(actor.HairStyle);
        if (actor.HairColor >= 0)
            sb.Append(",\"hairColor\":").Append(actor.HairColor);
        if (actor.AttackItem > 0)
        {
            sb.Append(",\"attackItem\":").Append(actor.AttackItem);
            if (actor.AttackEnchant > 0)
                sb.Append(",\"attackEnchant\":").Append(actor.AttackEnchant);
        }
        if (actor.DefenseItem > 0)
            sb.Append(",\"defenseItem\":").Append(actor.DefenseItem);
        if (actor.ShieldItem > 0)
            sb.Append(",\"shieldItem\":").Append(actor.ShieldItem);
        if (!actor.CharInfoSet)
            return;
        sb.Append(",\"charInfo\":{\"hairall\":").Append(actor.HairAll);
        sb.Append(",\"head\":").Append(actor.Head);
        sb.Append(",\"rhand\":").Append(actor.RHand);
        sb.Append(",\"lhand\":").Append(actor.LHand);
        sb.Append(",\"gloves\":").Append(actor.Gloves);
        sb.Append(",\"chest\":").Append(actor.Chest);
        sb.Append(",\"legs\":").Append(actor.Legs);
        sb.Append(",\"feet\":").Append(actor.Feet);
        sb.Append(",\"cloak\":").Append(actor.Cloak);
        sb.Append(",\"hairItem\":").Append(actor.HairItem);
        sb.Append(",\"faceItem\":").Append(actor.FaceItem);
        sb.Append("}");
    }

    static string ShortAnim(string clip)
    {
        if (string.IsNullOrEmpty(clip))
            return clip;
        int mark = clip.LastIndexOf(".ao_", StringComparison.OrdinalIgnoreCase);
        string name = mark >= 0 ? clip.Substring(mark + 4) : clip;
        if (name.IndexOf('_') < 0 || name.Length == 0 || !char.IsLetter(name[0]))
            return name;
        if (char.IsUpper(name[0]))
            return name;
        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    static int LastEventMs()
    {
        for (int i = _events.Count - 1; i >= 0; i--)
        {
            int mark = _events[i].IndexOf("\"tMs\":", StringComparison.Ordinal);
            if (mark < 0)
                continue;
            int from = mark + 6;
            int to = from;
            while (to < _events[i].Length && char.IsDigit(_events[i][to]))
                to++;
            int value;
            if (int.TryParse(_events[i].Substring(from, to - from), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;
        }
        return 0;
    }

    static string Num(float value)
    {
        return value.ToString("G9", CultureInfo.InvariantCulture);
    }

    static string Quote(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
