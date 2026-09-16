using UnityEngine;

/// <summary>
/// Myst strip mesh only. Cap/belt clouds load from
/// Resources/Data/StaticMeshes/L2Sky_S (L2sky_sp02 / L2sky_sp01).
/// </summary>
public static class L2CloudMeshes
{
    // EID 1566: 24 verts / 66 idx. Small vertical strip (not the haze tube).
    public const string CylName = "L2sky_cloud_cyl_strip";

        static readonly Vector3[] CylPos =
        {
            new Vector3(-2.4804f, -6.8375f, -0.0526f), new Vector3(-2.4804f, 6.8375f, -0.0526f),
            new Vector3(2.4804f, 6.8375f, -0.0526f), new Vector3(2.4804f, -6.8375f, -0.0526f),
            new Vector3(-25.5002f, -6.8375f, -6.1602f), new Vector3(-25.5002f, 6.8375f, -6.1602f),
            new Vector3(-21.344f, 6.8375f, -4.1701f), new Vector3(-21.344f, -6.8375f, -4.1701f),
            new Vector3(-16.903299f, 6.8375f, -2.5453f), new Vector3(-16.903299f, -6.8375f, -2.5453f),
            new Vector3(-12.2373f, 6.8375f, -1.3073f), new Vector3(-12.2373f, -6.8375f, -1.3073f),
            new Vector3(-7.4082f, 6.8375f, -0.4727f), new Vector3(-7.4082f, -6.8375f, -0.4727f),
            new Vector3(25.5002f, 6.8375f, -6.1602f), new Vector3(25.5002f, -6.8375f, -6.1602f),
            new Vector3(21.344f, -6.8375f, -4.1701f), new Vector3(21.344f, 6.8375f, -4.1701f),
            new Vector3(16.903299f, -6.8375f, -2.5453f), new Vector3(16.903299f, 6.8375f, -2.5453f),
            new Vector3(12.2373f, -6.8375f, -1.3073f), new Vector3(12.2373f, 6.8375f, -1.3073f),
            new Vector3(7.4082f, -6.8375f, -0.4727f), new Vector3(7.4082f, 6.8375f, -0.4727f),
        };

        static readonly Vector2[] CylUv =
        {
            new Vector2(0.5455f, 1.0f), new Vector2(0.5455f, 0.0f), new Vector2(0.4545f, 0.0f),
            new Vector2(0.4545f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f),
            new Vector2(0.9091f, 0.0f), new Vector2(0.9091f, 1.0f), new Vector2(0.8182f, 0.0f),
            new Vector2(0.8182f, 1.0f), new Vector2(0.7273f, 0.0f), new Vector2(0.7273f, 1.0f),
            new Vector2(0.6364f, 0.0f), new Vector2(0.6364f, 1.0f), new Vector2(0.0f, 0.0f),
            new Vector2(0.0f, 1.0f), new Vector2(0.0909f, 1.0f), new Vector2(0.0909f, 0.0f),
            new Vector2(0.1818f, 1.0f), new Vector2(0.1818f, 0.0f), new Vector2(0.2727f, 1.0f),
            new Vector2(0.2727f, 0.0f), new Vector2(0.3636f, 1.0f), new Vector2(0.3636f, 0.0f),
        };

        static readonly int[] CylIdx =
        {
            4, 5, 6, 7, 6, 8,
            6, 7, 4, 9, 8, 10,
            8, 9, 7, 10, 11, 9,
            11, 10, 12, 13, 12, 1,
            12, 13, 11, 0, 1, 2,
            1, 0, 13, 2, 3, 0,
            3, 2, 23, 22, 23, 21,
            23, 22, 3, 16, 17, 14,
            14, 15, 16, 20, 21, 19,
            21, 20, 22, 17, 16, 18,
            18, 19, 17, 19, 18, 20,
        };

    public static Mesh BuildCyl()
    {
        var mesh = new Mesh { name = CylName };
        mesh.vertices = CylPos;
        mesh.uv = CylUv;
        mesh.triangles = CylIdx;
        mesh.RecalculateBounds();
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 400f);
        return mesh;
    }
}
