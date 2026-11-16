using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LocalMapImportChecks
{
    public static void RunExpansionBatch()
    {
        string[] candidates = { "20_18", "20_20", "21_20" };
        // These are new trial scenes. Keep each out of the build until its current checks pass.
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene =>
            !candidates.Any(tile => scene.path == "Assets/Resources/Scenes/" + tile + ".unity")).ToArray();
        string previous = Environment.GetEnvironmentVariable("L2_MAP_PROBE_TILE");
        var failures = new List<string>();
        try
        {
            foreach (string tile in candidates)
            {
                Environment.SetEnvironmentVariable("L2_MAP_PROBE_TILE", tile);
                try { Run(); }
                catch (Exception error) { failures.Add(tile + ": " + error.Message); }
            }
        }
        finally { Environment.SetEnvironmentVariable("L2_MAP_PROBE_TILE", previous); }
        if (failures.Count > 0) throw new InvalidOperationException(string.Join("; ", failures));
    }

    public static void InspectSeam()
    {
        EditorSceneManager.OpenScene("Assets/Resources/Scenes/20_19.unity", OpenSceneMode.Single);
        EditorSceneManager.OpenScene("Assets/Resources/Scenes/21_19.unity", OpenSceneMode.Additive);
        Physics.SyncTransforms();
        var rows = new List<string>();
        foreach (int x in new[] { 32540, 32620, 32639, 32640, 32641, 32660, 32720, 32768, 32840 })
        {
            Vector3 point = VectorUtils.ConvertPosToUnity(new Vector3(x, 49008, -3700));
            var hits = Physics.RaycastAll(point + Vector3.up * 20, Vector3.down, 40, ~0, QueryTriggerInteraction.Ignore)
                .OrderByDescending(hit => hit.point.y)
                .Select(hit => hit.collider.name + ":" + VectorUtils.ConvertPosUnityToL2j(hit.point).z.ToString("F2"));
            rows.Add("x=" + x + " " + string.Join(",", hits));
        }
        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../runtime/evidence/map-seam-20_19-21_19.txt"));
        File.WriteAllLines(output, rows);
        Debug.Log("[LocalMapImportChecks] seam " + string.Join(";", rows));
    }

    [Serializable]
    public sealed class Report
    {
        public string tile;
        public int roots;
        public int renderers;
        public int meshColliders;
        public List<string> errors = new List<string>();
        public List<string> groundHits = new List<string>();
        public List<string> terrainBounds = new List<string>();
        public bool sceneAddedToBuild;
    }

    public static void Run()
    {
        string tile = Environment.GetEnvironmentVariable("L2_MAP_PROBE_TILE") ?? "21_19";
        if (!Regex.IsMatch(tile, @"^\d{2}_\d{2}$"))
            throw new ArgumentException("Invalid map tile");
        string scenePath = "Assets/Resources/Scenes/" + tile + ".unity";
        var report = new Report { tile = tile };
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        report.roots = roots.Length;
        foreach (GameObject root in roots)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                    report.errors.Add("Missing script: " + child.name);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                report.renderers++;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        report.errors.Add("Missing material: " + renderer.name);
                    else if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader" || !material.shader.isSupported)
                        report.errors.Add("Invalid shader: " + material.name);
                }
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh == null)
                    report.errors.Add("Missing mesh: " + filter.name);
            foreach (MeshCollider collider in root.GetComponentsInChildren<MeshCollider>(true))
            {
                report.meshColliders++;
                if (collider.name == tile)
                    report.terrainBounds.Add("min=" + VectorUtils.ConvertPosUnityToL2j(collider.bounds.min) + " max=" + VectorUtils.ConvertPosUnityToL2j(collider.bounds.max));
                if (collider.sharedMesh == null)
                    report.errors.Add("Missing collision mesh: " + collider.name);
            }
        }
        if (report.renderers == 0 || report.meshColliders == 0)
            report.errors.Add("Scene has no rendered or collision geometry");
        Physics.SyncTransforms();
        Vector3[] samples = SamplePositions(tile);
        foreach (Vector3 l2 in samples)
        {
            Vector3 point = VectorUtils.ConvertPosToUnity(l2);
            RaycastHit[] hits = Physics.RaycastAll(point + Vector3.up * 300, Vector3.down, 600, ~0, QueryTriggerInteraction.Ignore);
            var ground = hits.Where(hit => Mathf.Abs(hit.point.y - point.y) <= 3f).OrderBy(hit => Mathf.Abs(hit.point.y - point.y)).ToArray();
            if (GroundSnapHelper.TrySnapToGround(point, out Vector3 snapped) && Mathf.Abs(snapped.y - point.y) <= 3f)
                report.groundHits.Add("L2=" + l2 + " ground=" + VectorUtils.ConvertPosUnityToL2j(snapped) + " local cast");
            else if (ground.Length == 0)
                report.errors.Add("No ground near L2 position " + l2);
            else
                report.groundHits.Add("L2=" + l2 + " ground=" + VectorUtils.ConvertPosUnityToL2j(ground[0].point) + " collider=" + ground[0].collider.name);
        }
        if (report.errors.Count == 0)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(value => value.path == scenePath && value.enabled))
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            report.sceneAddedToBuild = true;
        }
        string evidence = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../runtime/evidence/map-import-" + tile + ".json"));
        Directory.CreateDirectory(Path.GetDirectoryName(evidence));
        File.WriteAllText(evidence, JsonUtility.ToJson(report, true));
        Debug.Log("[LocalMapImportChecks] " + JsonUtility.ToJson(report));
        if (report.errors.Count > 0)
            throw new InvalidOperationException("Map import validation failed; see " + evidence);
    }

    static Vector3[] SamplePositions(string tile)
    {
        switch (tile)
        {
            case "20_18":
                return new[] { new Vector3(10800, 15800, -4552), new Vector3(10900, 15800, -4552), new Vector3(10800, 15900, -4552) };
            case "20_19":
                return new[] { new Vector3(32520, 49008, -3060), new Vector3(32620, 49008, -3060), new Vector3(32520, 49108, -3060) };
            case "21_19":
                return new[] { new Vector3(45848, 49008, -3060), new Vector3(45948, 49008, -3060), new Vector3(45848, 49108, -3060) };
            case "20_20":
                return new[] { new Vector3(12283, 77057, -4176), new Vector3(12383, 77057, -4176), new Vector3(12283, 77157, -4176) };
            case "21_20":
                return new[] { new Vector3(40494, 68888, -3548), new Vector3(40594, 68888, -3548), new Vector3(40494, 68988, -3548) };
            default:
                throw new ArgumentException("Define verified server sample positions before validating tile " + tile);
        }
    }
}
