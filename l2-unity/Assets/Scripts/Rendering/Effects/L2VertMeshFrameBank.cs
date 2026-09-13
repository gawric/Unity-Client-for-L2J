using System;
using UnityEngine;

/// <summary>
/// HE VertMesh frame bank (from ParticlePhysicsLog VERTMESH_FRAME_BANK).
/// GetFrame: scaled = animTime * frameCount; f0 = floor(scaled) % N; alpha = frac;
/// V = lerp(frame[f0], frame[f1], alpha). animTime += (seqRate/frameCount)*dt.
/// </summary>
[CreateAssetMenu(menuName = "L2/VertMesh Frame Bank", fileName = "VertMeshFrameBank")]
public sealed class L2VertMeshFrameBank : ScriptableObject
{
    public string sequenceName = "sh2";
    public int frameCount = 21;
    public int vertCount = 26;
    [Tooltip("AnimSeq rate@+0x18 from live dump (sh2 = 30)")]
    public float sequenceRate = 30f;
    [Tooltip("UC StartSize for VertMeshEmitter47")]
    public float startSizeUu = 0.12f;

    [Tooltip("RGBAHalf tex: width=vertCount, height=frameCount, RGB=XYZ in UU")]
    public Texture2D framePositionTex;

    [Tooltip("Mesh built from frame 0 + RenderDoc UVs/topology")]
    public Mesh baseMesh;

    public float AnimRatePerSecond =>
        frameCount > 0 ? sequenceRate / frameCount : 0f;

    public void EvaluateFrame(float animTime, out int frame0, out int frame1, out float alpha)
    {
        int n = Mathf.Max(1, frameCount);
        float scaled = Mathf.Max(0f, animTime) * n;
        int i0 = Mathf.FloorToInt(scaled);
        alpha = scaled - i0;
        frame0 = ((i0 % n) + n) % n;
        frame1 = ((i0 + 1) % n + n) % n;
    }

    public Vector3 SamplePositionUu(int frame, int vert)
    {
        if (framePositionTex == null || frame < 0 || frame >= frameCount ||
            vert < 0 || vert >= vertCount)
        {
            return Vector3.zero;
        }

        // Texture is not readable at runtime unless marked readable; prefer GetPixel if readable.
        if (!framePositionTex.isReadable)
        {
            return Vector3.zero;
        }

        Color c = framePositionTex.GetPixel(vert, frame);
        return new Vector3(c.r, c.g, c.b);
    }
}
