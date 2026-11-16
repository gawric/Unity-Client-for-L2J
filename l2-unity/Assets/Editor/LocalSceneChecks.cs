using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

public static class LocalSceneChecks
{
    public static void Inspect()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Resources/Data/UI/_Elements/Lineage2.asset");
        Debug.Log("[LocalSceneChecks] panel mode=" + settings.scaleMode + " scale=" + settings.scale + " reference=" + settings.referenceResolution);
        var scene = EditorSceneManager.OpenScene("Assets/Resources/Scenes/Game.unity");
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (CharacterController controller in root.GetComponentsInChildren<CharacterController>(true))
            {
                Vector3 scale = controller.transform.lossyScale;
                float height = controller.height * Mathf.Abs(scale.y);
                if (controller.stepOffset > height)
                    Debug.Log("[LocalSceneChecks] invalid controller=" + AnimationUtility.CalculateTransformPath(controller.transform, null) +
                        " height=" + controller.height + " radius=" + controller.radius + " scale=" + scale + " step=" + controller.stepOffset);
            }
        }
    }
}
