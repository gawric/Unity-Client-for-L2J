#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Wire LineageMonsters prefabs like keltir_m00 / fox_m00:
/// Animator + Monster_Basic override, clip paths bound to the mesh armature, component refs.
/// </summary>
public static class L2MonsterPrefabBuilder
{
    static readonly string[] MonsterFolders =
    {
        "Assets/Resources/Data/Animations/LineageMonsters",
        "Assets/Resources/Data/Animations/LineageMonsters2",
    };
    const string OverrideFolder = "Assets/Resources/Data/Animations/Animator/Monster";
    const string BasicController = "Assets/Resources/Data/Animations/Animator/_Template/Monster_Basic.controller";
    static readonly (string slot, string[] needles)[] ClipNeedles =
    {
        ("wait", new[] { "ao_wait", "ao_Wait" }),
        ("walk", new[] { "ao_walk", "ao_Walk" }),
        ("run", new[] { "ao_run" }),
        ("atk01", new[] { "ao_atk01" }),
        ("atkwait", new[] { "ao_atkwait" }),
        ("deathwait", new[] { "ao_deathwait" }),
        ("death", new[] { "ao_death" }),
        ("spatk01", new[] { "ao_spatk01", "ao_SpAtk01" }),
        ("spwait", new[] { "ao_spwait", "ao_SpWait", "ao_spwait01", "ao_SpWait01" }),
        ("damageaction", new[] { "ao_takedamage", "ao_TakeDamage", "ao_damageaction", "ao_DamageAction" }),
        ("damageaction1", new[] { "ao_damageaction.001", "ao_DamageAction.001" }),
    };

    [MenuItem("L2/Strip NetworkTransformReceive")]
    public static void StripNetworkTransformReceiveMenu()
    {
        StripNetworkTransformReceive();
    }

    public static void StripNetworkTransformReceive()
    {
        string[] folders =
        {
            "Assets/Resources/Data/Animations",
            "Assets/Resources/Prefab",
        };
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", folders);
        int stripped = 0;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!prefabPath.EndsWith(".prefab"))
                continue;
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                NetworkTransformReceive ntr = root.GetComponent<NetworkTransformReceive>();
                if (ntr == null)
                    continue;
                Object.DestroyImmediate(ntr);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                stripped++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[L2Monster] Stripped NetworkTransformReceive from " + stripped + " prefabs.");
    }

    [MenuItem("L2/Monsters/Wire Prefabs Like Keltir")]
    public static void WireLikeKeltirMenu()
    {
        WireLikeKeltir();
    }

    public static void WireLikeKeltir()
    {
        var log = new StringBuilder();
        AnimatorController basic = AssetDatabase.LoadAssetAtPath<AnimatorController>(BasicController);
        if (basic == null)
        {
            Debug.LogError("[L2Monster] Missing Monster_Basic.controller");
            return;
        }

        Dictionary<string, AnimationClip> slotOriginals = CollectSlotOriginals(basic);
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", MonsterFolders);
        int ok = 0;
        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            if (!prefabPath.EndsWith(".prefab"))
                continue;
            try
            {
                string line = WireOne(prefabPath, basic, slotOriginals);
                log.AppendLine(line);
                ok++;
            }
            catch (System.Exception e)
            {
                log.AppendLine("FAIL " + prefabPath + " " + e.Message);
                Debug.LogException(e);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[L2Monster] Wired " + ok + "/" + prefabGuids.Length + " prefabs.\n" + log);
    }

    static string WireOne(
        string prefabPath,
        AnimatorController basic,
        Dictionary<string, AnimationClip> slotOriginals)
    {
        string folder = Path.GetDirectoryName(prefabPath).Replace('\\', '/');
        string stem = Path.GetFileNameWithoutExtension(prefabPath);
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            EnsureGameplayComponents(root);
            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();

            Dictionary<string, AnimationClip> locals = CollectLocalClips(folder);
            AnimatorOverrideController ov = animator.runtimeAnimatorController as AnimatorOverrideController;
            if (locals.Count > 0)
            {
                ov = LoadOrCreateOverride(stem, basic);
                ApplySlotOverrides(ov, slotOriginals, locals);
            }
            else if (ov == null)
            {
                ov = LoadOrCreateOverride(stem, basic);
            }

            if (ov != null)
                animator.runtimeAnimatorController = ov;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Avatar avatar = FindGenericAvatar(folder);
            if (avatar != null)
                animator.avatar = avatar;

            IEnumerable<AnimationClip> remapSet = locals.Count > 0
                ? (IEnumerable<AnimationClip>)new List<AnimationClip>(locals.Values)
                : new List<AnimationClip>();
            string remap = RemapClipsToHierarchy(animator, remapSet);
            AssignAnimatorRefs(root, animator);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return stem + " clips=" + locals.Count + " avatar=" + (avatar != null) + " " + remap;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void EnsureGameplayComponents(GameObject root)
    {
        DestroyImmediateIfPresent<NpcEntity>(root);
        DestroyImmediateIfPresent<NpcStateMachine>(root);
        DestroyImmediateIfPresent<MonsterStateMachine>(root);
        DestroyImmediateIfPresent<CharacterAnimationAudioHandler>(root);

        Gear gear = root.GetComponent<Gear>();
        if (gear != null && !(gear is MonsterGear))
            Object.DestroyImmediate(gear);

        if (root.GetComponent<NetworkAnimationController>() == null)
            root.AddComponent<NetworkAnimationController>();
        if (root.GetComponent<Animator>() == null)
            root.AddComponent<Animator>();
        if (root.GetComponent<CharacterController>() == null)
            root.AddComponent<CharacterController>();
        if (root.GetComponent<MonsterGear>() == null)
            root.AddComponent<MonsterGear>();
        if (root.GetComponent<MonsterEntity>() == null)
            root.AddComponent<MonsterEntity>();
        if (root.GetComponent<MonsterAnimationAudioHandler>() == null)
            root.AddComponent<MonsterAnimationAudioHandler>();
        DestroyImmediateIfPresent<NetworkTransformReceive>(root);
        DeadMonster dead = root.GetComponent<DeadMonster>();
        if (dead == null)
        {
            dead = root.AddComponent<DeadMonster>();
            dead.enabled = false;
        }
        GravityMonster gravity = root.GetComponent<GravityMonster>();
        if (gravity == null)
        {
            gravity = root.AddComponent<GravityMonster>();
            gravity.enabled = false;
        }

        if (root.GetComponent<NetworkAnimationController>() is NetworkAnimationController nac)
        {
            SerializedObject so = new SerializedObject(nac);
            so.FindProperty("_resetStateOnReceive").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Transform clickArea = root.transform.Find("click_area");
        if (clickArea != null)
            clickArea.gameObject.tag = "Npc";
    }

    static void DestroyImmediateIfPresent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component != null)
            Object.DestroyImmediate(component);
    }

    static AnimatorOverrideController LoadOrCreateOverride(string stem, AnimatorController basic)
    {
        string name = stem.EndsWith("_m00") ? stem.Substring(0, stem.Length - 4) : stem;
        string path = OverrideFolder + "/" + name + ".overrideController";
        AnimatorOverrideController ov = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (ov == null)
        {
            ov = new AnimatorOverrideController { runtimeAnimatorController = basic };
            Directory.CreateDirectory(OverrideFolder);
            AssetDatabase.CreateAsset(ov, path);
        }
        else if (ov.runtimeAnimatorController == null)
        {
            ov.runtimeAnimatorController = basic;
            EditorUtility.SetDirty(ov);
        }

        return ov;
    }

    static Dictionary<string, AnimationClip> CollectSlotOriginals(AnimatorController basic)
    {
        var map = new Dictionary<string, AnimationClip>();
        AnimatorControllerLayer[] layers = basic.layers;
        for (int i = 0; i < layers.Length; i++)
            CollectStates(layers[i].stateMachine, map);
        return map;
    }

    static void CollectStates(AnimatorStateMachine machine, Dictionary<string, AnimationClip> map)
    {
        if (machine == null)
            return;
        ChildAnimatorState[] states = machine.states;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            AnimationClip clip = state != null ? state.motion as AnimationClip : null;
            if (state != null && clip != null && !map.ContainsKey(state.name))
                map[state.name] = clip;
        }

        ChildAnimatorStateMachine[] children = machine.stateMachines;
        for (int i = 0; i < children.Length; i++)
            CollectStates(children[i].stateMachine, map);
    }

    static Dictionary<string, AnimationClip> CollectLocalClips(string folder)
    {
        var map = new Dictionary<string, AnimationClip>();
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { folder });
        var files = new List<AnimationClip>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null)
                files.Add(clip);
        }

        if (files.Count == 0)
        {
            string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            for (int i = 0; i < fbxGuids.Length; i++)
            {
                string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuids[i]);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
                for (int a = 0; a < assets.Length; a++)
                {
                    AnimationClip clip = assets[a] as AnimationClip;
                    if (clip != null && !clip.name.StartsWith("__preview__"))
                        files.Add(clip);
                }
            }
        }

        for (int s = 0; s < ClipNeedles.Length; s++)
        {
            AnimationClip best = PickClip(files, ClipNeedles[s].needles, ClipNeedles[s].slot == "death");
            if (best != null)
                map[ClipNeedles[s].slot] = best;
        }

        return map;
    }

    static AnimationClip PickClip(List<AnimationClip> files, string[] needles, bool deathNotWait)
    {
        AnimationClip fallback = null;
        int bestScore = int.MaxValue;
        for (int i = 0; i < files.Count; i++)
        {
            string n = files[i].name;
            if (deathNotWait && n.IndexOf("deathwait", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (n.IndexOf("_hand", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("social", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("knock", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("air_hold", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            for (int k = 0; k < needles.Length; k++)
            {
                if (n.IndexOf(needles[k], System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                int score = n.Length;
                if (n.IndexOf(".00", System.StringComparison.Ordinal) >= 0)
                    score += 50;
                if (score < bestScore)
                {
                    bestScore = score;
                    fallback = files[i];
                }
            }
        }

        return fallback;
    }

    static void ApplySlotOverrides(
        AnimatorOverrideController ov,
        Dictionary<string, AnimationClip> slotOriginals,
        Dictionary<string, AnimationClip> locals)
    {
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (KeyValuePair<string, AnimationClip> kv in slotOriginals)
        {
            AnimationClip local;
            if (!locals.TryGetValue(kv.Key, out local) || local == null)
            {
                if (kv.Key == "spatk01")
                    locals.TryGetValue("spwait", out local);
                if (kv.Key == "damageaction1")
                    locals.TryGetValue("damageaction", out local);
                if (kv.Key == "atk01")
                    locals.TryGetValue("atkwait", out local);
            }

            if (local != null)
                pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(kv.Value, local));
        }

        ov.ApplyOverrides(pairs);
        EditorUtility.SetDirty(ov);
    }

    static Avatar FindGenericAvatar(string folder)
    {
        string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        for (int i = 0; i < fbxGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(fbxGuids[i]);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int a = 0; a < assets.Length; a++)
            {
                Avatar avatar = assets[a] as Avatar;
                if (avatar != null)
                    return avatar;
            }
        }

        return null;
    }

    static string RemapClipsToHierarchy(Animator animator, IEnumerable<AnimationClip> clips)
    {
        Transform armature = FindArmature(animator.transform);
        if (armature == null)
            return "armature=missing";

        bool animatorIsArmature = armature == animator.transform;
        string wantPrefix = animatorIsArmature ? "" : RelativePath(animator.transform, armature);
        int remapped = 0;
        int bound = 0;
        int missing = 0;
        foreach (AnimationClip clip in clips)
        {
            if (clip == null)
                continue;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0)
                continue;
            string sample = bindings[0].path;
            if (PathExists(animator.transform, sample))
            {
                bound++;
                continue;
            }

            string haveRoot = FirstSegment(sample);
            if (RemapClipPrefix(clip, haveRoot, wantPrefix))
                remapped++;
            else
                missing++;
        }

        return "ao=" + armature.name + " prefix='" + wantPrefix + "' remapped=" + remapped +
               " alreadyBound=" + bound + " missing=" + missing;
    }

    static bool RemapClipPrefix(AnimationClip clip, string oldRoot, string newPrefix)
    {
        if (string.IsNullOrEmpty(oldRoot))
            return false;
        bool changed = false;
        EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
        for (int i = 0; i < bindings.Length; i++)
        {
            EditorCurveBinding b = bindings[i];
            string next = RewritePath(b.path, oldRoot, newPrefix);
            if (next == b.path)
                continue;
            AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, b);
            AnimationUtility.SetEditorCurve(clip, b, null);
            b.path = next;
            AnimationUtility.SetEditorCurve(clip, b, curve);
            changed = true;
        }

        EditorCurveBinding[] refs = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        for (int i = 0; i < refs.Length; i++)
        {
            EditorCurveBinding b = refs[i];
            string next = RewritePath(b.path, oldRoot, newPrefix);
            if (next == b.path)
                continue;
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, b);
            AnimationUtility.SetObjectReferenceCurve(clip, b, null);
            b.path = next;
            AnimationUtility.SetObjectReferenceCurve(clip, b, keys);
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(clip);
        return changed;
    }

    static string RewritePath(string path, string oldRoot, string newPrefix)
    {
        if (path == oldRoot)
            return newPrefix;
        string head = oldRoot + "/";
        if (path.StartsWith(head))
        {
            string tail = path.Substring(head.Length);
            return string.IsNullOrEmpty(newPrefix) ? tail : newPrefix + "/" + tail;
        }

        return path;
    }

    static Transform FindArmature(Transform root)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name.EndsWith(".ao"))
                return all[i];
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "Bip01" || all[i].name == "bip01")
                return all[i].parent != null ? all[i].parent : all[i];
        }

        return root;
    }

    static bool PathExists(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;
        return root.Find(path) != null;
    }

    static string FirstSegment(string path)
    {
        int slash = path.IndexOf('/');
        return slash < 0 ? path : path.Substring(0, slash);
    }

    static string RelativePath(Transform root, Transform target)
    {
        if (root == target)
            return "";
        var parts = new List<string>();
        Transform t = target;
        while (t != null && t != root)
        {
            parts.Add(t.name);
            t = t.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    static void AssignAnimatorRefs(GameObject root, Animator animator)
    {
        MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
                continue;
            SerializedObject so = new SerializedObject(behaviours[i]);
            SerializedProperty prop = so.FindProperty("_animator");
            if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                prop.objectReferenceValue = animator;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
