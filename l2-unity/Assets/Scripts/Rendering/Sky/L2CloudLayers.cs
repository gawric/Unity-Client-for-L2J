using UnityEngine;

/// <summary>
/// Legacy combined cloud component that sat on L2Haze and broke WhiteRing.
/// Strips old children and removes itself.
/// </summary>
[ExecuteInEditMode]
public class L2CloudLayers : MonoBehaviour
{
    void OnEnable()
    {
        StripLegacyCloudChildren(transform);
        if (Application.isPlaying)
            Destroy(this);
        else
            DestroyImmediate(this);
    }

    public static void StripLegacyCloudChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null)
                continue;
            string n = child.name;
            // Only the previous combined port — do not touch L2CloudSimple_* / current myst mesh.
            if (n == "L2CloudDomeA" || n == "L2CloudPatch" || n == "L2CloudDomeB" || n == "L2CloudMyst")
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
    }
}
