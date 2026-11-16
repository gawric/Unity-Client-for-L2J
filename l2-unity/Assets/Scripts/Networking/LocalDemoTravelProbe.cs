using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LocalDemoTravelProbe : MonoBehaviour
{
    public string ExpectedTile;
    public int OffsetX;
    public int OffsetY;
    public int Delay = 12;

    [Serializable]
    public sealed class TravelResult
    {
        public string tile;
        public bool sceneLoaded;
        public int activeRenderers;
        public bool movementRequested;
        public bool success;
        public Vector3 start;
        public Vector3 target;
        public Vector3 finish;
        public string finishTile;
        public int finishActiveRenderers;
        public string error;
    }

    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(1, Delay));
        var result = new TravelResult();
        PlayerEntity player = PlayerEntity.Instance;
        if (player == null)
        {
            Save(result, "No player");
            yield break;
        }
        result.start = VectorUtils.ConvertPosUnityToL2j(player.transform.position);
        result.tile = VectorUtils.GetSwMapName(result.start);
        Scene scene = SceneManager.GetSceneByName(result.tile);
        result.sceneLoaded = scene.IsValid() && scene.isLoaded;
        if (result.sceneLoaded)
            result.activeRenderers = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Renderer>(false).Count(renderer => renderer.enabled));
        if (!result.sceneLoaded || result.activeRenderers == 0 || (!string.IsNullOrEmpty(ExpectedTile) && result.tile != ExpectedTile))
        {
            Save(result, "Expected rendered map scene is not active");
            yield break;
        }
        result.target = result.start + new Vector3(OffsetX, OffsetY, 0);
        result.movementRequested = OffsetX != 0 || OffsetY != 0;
        if (result.movementRequested)
        {
            Vector3 point = VectorUtils.ConvertPosToUnity(result.target);
            RaycastHit[] ground = Physics.RaycastAll(point + Vector3.up * 10, Vector3.down, 30, ~0, QueryTriggerInteraction.Ignore)
                .Where(hit => hit.collider.GetComponentInParent<Entity>() == null && hit.point.y <= point.y + 2f)
                .OrderByDescending(hit => hit.point.y).ToArray();
            if (ground.Length == 0 || ClickManager.Instance == null)
            {
                Save(result, "No clickable ground at destination");
                yield break;
            }
            result.target = VectorUtils.ConvertPosUnityToL2j(ground[0].point);
            ClickManager.Instance.OnClickToMove(ground[0]);
            float deadline = Time.realtimeSinceStartup + 18f;
            do
            {
                yield return new WaitForSecondsRealtime(0.5f);
                result.finish = VectorUtils.ConvertPosUnityToL2j(player.transform.position);
            }
            while (Distance(result.finish, result.target) > 20 && Time.realtimeSinceStartup < deadline);
            if (Distance(result.finish, result.target) > 20)
            {
                Save(result, "Player did not reach destination");
                yield break;
            }
        }
        result.finish = VectorUtils.ConvertPosUnityToL2j(player.transform.position);
        result.finishTile = VectorUtils.GetSwMapName(result.finish);
        float sceneDeadline = Time.realtimeSinceStartup + 10f;
        Scene finishScene;
        do
        {
            finishScene = SceneManager.GetSceneByName(result.finishTile);
            if (finishScene.IsValid() && finishScene.isLoaded)
                break;
            yield return new WaitForSecondsRealtime(0.5f);
        }
        while (Time.realtimeSinceStartup < sceneDeadline);
        if (finishScene.IsValid() && finishScene.isLoaded)
            result.finishActiveRenderers = finishScene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Renderer>(false).Count(renderer => renderer.enabled));
        if (result.finishActiveRenderers == 0)
        {
            Save(result, "Destination map scene has no active rendered geometry");
            yield break;
        }
        result.success = true;
        Save(result, null);
    }

    static float Distance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.y), new Vector2(b.x, b.y));

    static void Save(TravelResult result, string error)
    {
        result.error = error;
        string json = JsonUtility.ToJson(result);
        File.WriteAllText(Path.Combine(UnityAutoLogin.ResultDirectory, "autologin.travel"), json);
        Debug.Log("[LocalDemoTravel] " + json);
    }
}
