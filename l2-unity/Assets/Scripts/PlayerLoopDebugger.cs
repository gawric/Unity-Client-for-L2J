
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class PlayerLoopDebugger : MonoBehaviour
{
    public Shader shaderToDisable; // ������� ������, ������� ����� ���������
    public bool istest = false;
    void Start()
    {
        //Shader foundShader = Shader.Find("L2Grass");
        //DisableShader(shaderToDisable);
    }

    public void Update()
    {
        if (istest)
        {
            istest = true;

            //Shader foundShader = Shader.Find("L2Grass");
           // Shader speedTreeShader1 = Shader.Find("Universal Render Pipeline/Nature/SpeedTree8_PBRLit");
           // Shader speedTreeShader2 = Shader.Find("Shader Graphs/TerrainAlphamapWater");
           // Shader speedTreeShader3 = Shader.Find("Hidden/BOXOPHOBIC/Atmospherics/Height Fog Global");

           // if (speedTreeShader1 != null)
            //{
            //    DisableShader(speedTreeShader1);
            //}
            //if (speedTreeShader2 != null)
            //{
             //   DisableShader(speedTreeShader1);
           // }
            //if (speedTreeShader3 != null)
            //{
            //    DisableShader(speedTreeShader1);
            //}
        }
        
    }

    void DisableShader(Shader shader)
    {
        if (shader == null)
        {
            Debug.LogWarning("Shader to disable is not assigned.");
            return;
        }

        // ������� ��� ��������� � �����
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                // ���������, ���������� �� �������� ��������� ������
                if (material != null && material.shader == shader)
                {
                    // ��������� ������, ������� ��� �� Unlit/Texture ��� ������ ��������
                    material.shader = Shader.Find("Unlit/Texture");
                    // ��� ����� ������������: material.shader = Shader.Find("Standard");
                    Debug.Log($"Shader {shader.name} disabled on {renderer.gameObject.name}");
                }
            }
        }
    }
}