using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class LocalNpcImportChecks
{
    public const string Folder = "Assets/Resources/Data/Animations/LineageNPCs/a_trader_MElf_m00";
    const string Model = Folder + "/Model/a_trader_MElf.fbx";
    const string Reference = "Assets/Resources/Data/Animations/LineageNPCs/a_traderA_MHuman_m00/a_traderA_MHuman_m00.prefab";

    public static void Build()
    {
        string output = Folder + "/a_trader_MElf_m00.prefab";
        if (File.Exists(output)) throw new InvalidOperationException("Refusing to overwrite an existing NPC prefab");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)UnityEditor.AssetImporter.GetAtPath(Model);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.optimizeGameObjects = false;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.isReadable = true;
        var definitions = importer.defaultClipAnimations;
        foreach (var definition in definitions)
        {
            string name = definition.name.ToLowerInvariant();
            definition.loopTime = name.Contains("wait") || name.Contains("walk") || name.Contains("run");
        }
        importer.clipAnimations = definitions;
        importer.SaveAndReimport();
        var assets = AssetDatabase.LoadAllAssetsAtPath(Model);
        var clips = assets.OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
        if (clips.Length != 14) throw new InvalidOperationException("Expected 14 exported animation clips");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        GameObject root = UnityEngine.Object.Instantiate(model);
        root.name = "a_trader_MElf_m00";
        try
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 6;
            root.tag = "Npc";
            var animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            Avatar avatar = assets.OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid) throw new InvalidOperationException("Generic avatar is missing or invalid");
            animator.avatar = avatar;
            var basic = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/Data/Animations/Animator/_Template/Npc_Basic.controller");
            if (basic == null) throw new InvalidOperationException("NPC animation template is missing");
            var overrides = new AnimatorOverrideController(basic) { name = "a_trader_MElf" };
            var motions = new Dictionary<AnimationClip, AnimationClip>();
            foreach (var layer in basic.layers) MapStates(layer.stateMachine, clips, motions);
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.GetOverrides(pairs);
            for (int i = 0; i < pairs.Count; i++)
            {
                if (!motions.TryGetValue(pairs[i].Key, out var clip))
                    throw new InvalidOperationException("Unmapped NPC animation slot " + pairs[i].Key.name);
                pairs[i] = new KeyValuePair<AnimationClip, AnimationClip>(pairs[i].Key, clip);
            }
            overrides.ApplyOverrides(pairs);
            AssetDatabase.CreateAsset(overrides, Folder + "/a_trader_MElf.overrideController");
            animator.runtimeAnimatorController = overrides;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader unavailable");
            Directory.CreateDirectory(Folder + "/Materials");
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    string name = materials[i].name;
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + name + ".png");
                    if (texture == null) throw new InvalidOperationException("No matching texture for " + name);
                    var material = new Material(shader) { name = name, mainTexture = texture };
                    material.SetTexture("_BaseMap", texture);
                    material.SetFloat("_Smoothness", 0);
                    if (name.EndsWith("_h"))
                    {
                        material.SetFloat("_AlphaClip", 1);
                        material.SetFloat("_Cutoff", 0.5f);
                        material.SetFloat("_Cull", 0);
                        material.EnableKeyword("_ALPHATEST_ON");
                    }
                    AssetDatabase.CreateAsset(material, Folder + "/Materials/" + name + ".mat");
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
                renderer.updateWhenOffscreen = true;
            }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y < 0.6f || bounds.size.y > 1.3f) throw new InvalidOperationException("NPC scale outside reference range");
            root.AddComponent<NpcEntity>();
            root.AddComponent<NpcStateMachine>();
            var gear = root.AddComponent<Gear>();
            var collider = root.AddComponent<CharacterController>();
            collider.height = 0.75f;
            collider.radius = 0.1f;
            collider.center = new Vector3(0, 0.375f, 0);
            collider.stepOffset = 0.2f;
            var click = new GameObject("click_area") { layer = 13, tag = "Npc" };
            click.transform.SetParent(root.transform, false);
            var box = click.AddComponent<BoxCollider>();
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(Mathf.Max(0.25f, bounds.size.x), bounds.size.y, Mathf.Max(0.25f, bounds.size.z));
            foreach (var component in new Component[] { root.GetComponent<NetworkAnimationController>(), root.GetComponent<CharacterAnimationAudioHandler>() })
            {
                var serialized = new SerializedObject(component);
                serialized.FindProperty("_animator").objectReferenceValue = animator;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var gearData = new SerializedObject(gear);
            Transform[] bones = root.GetComponentsInChildren<Transform>();
            gearData.FindProperty("_rightHandBone").objectReferenceValue = bones.FirstOrDefault(bone => bone.name == "Weapon_R_Bone");
            gearData.FindProperty("_leftHandBone").objectReferenceValue = bones.FirstOrDefault(bone => bone.name == "Weapon_L_Bone");
            gearData.FindProperty("_shieldBone").objectReferenceValue = bones.FirstOrDefault(bone => bone.name == "Weapon_L_Bone");
            gearData.ApplyModifiedPropertiesWithoutUndo();
            if (renderers.Any(renderer => renderer.bones.Any(bone => bone == null) || renderer.sharedMesh == null || renderer.sharedMaterials.Any(material => material == null)))
                throw new InvalidOperationException("Missing mesh, bone or material binding");
            PrefabUtility.SaveAsPrefabAsset(root, output);
            AssetDatabase.SaveAssets();
            Debug.Log("[LocalNpcImport] Created " + output + " clips=" + clips.Length + " bones=" + renderers.Sum(renderer => renderer.bones.Length) + " height=" + bounds.size.y);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void MapStates(AnimatorStateMachine machine, AnimationClip[] clips, Dictionary<AnimationClip, AnimationClip> motions)
    {
        var suffixes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { "wait", "ao_Wait_Hand" }, { "walk", "ao_walk_hand" }, { "run", "ao_Run_Hand" },
            { "atk01", "ao_atk01_Hand" }, { "atkwait", "ao_AtkWait_Hand" }, { "death", "ao_Death_Hand" },
            { "deathwait", "ao_deathwait_Hand" }, { "spwait", "ao_SpWait01" }, { "spwait01", "ao_SpWait01" }, { "spwait02", "ao_SpWait01" }, { "social01", "ao_Social01" }
        };
        foreach (var child in machine.states)
        {
            if (!(child.state.motion is AnimationClip original)) continue;
            if (!suffixes.TryGetValue(child.state.name, out string suffix))
                throw new InvalidOperationException("NPC template state requires an explicit mapping: " + child.state.name);
            AnimationClip clip = clips.SingleOrDefault(item => item.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (clip == null) throw new InvalidOperationException("Missing animation " + suffix);
            motions[original] = clip;
        }
        foreach (var child in machine.stateMachines) MapStates(child.stateMachine, clips, motions);
    }

    public static void Inspect()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string path in new[] { Reference, Model })
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) throw new InvalidOperationException("No model at " + path);
            GameObject instance = UnityEngine.Object.Instantiate(asset);
            try
            {
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var data = new {
                    path, size = new[] { bounds.size.x, bounds.size.y, bounds.size.z },
                    rootScale = new[] { instance.transform.localScale.x, instance.transform.localScale.y, instance.transform.localScale.z },
                    components = instance.GetComponents<Component>().Select(c => c.GetType().Name).ToArray(),
                    children = instance.transform.Cast<Transform>().Select(t => t.name).ToArray(),
                    bones = renderers.Sum(renderer => renderer.bones.Length),
                    materials = renderers.SelectMany(renderer => renderer.sharedMaterials).Select(material => material != null ? material.name : "NULL").ToArray(),
                    clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Select(clip => clip.name).ToArray()
                };
                Debug.Log("[LocalNpcInspect] " + Newtonsoft.Json.JsonConvert.SerializeObject(data));
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
    }
}
