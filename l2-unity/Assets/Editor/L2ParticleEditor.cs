using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(L2Particle))]
public class L2ParticleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        L2Particle script = (L2Particle)target;

        GUILayout.Space(15);
        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); 

        if (GUILayout.Button("PLAY EFFECT (Reset Timer)", GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                script.ResetTimer();
            }
            else
            {
                Debug.LogWarning("Ёффект можно запустить только во врем€ »гры (Play Mode)!");
            }
        }
        GUI.backgroundColor = Color.white;
    }
}
