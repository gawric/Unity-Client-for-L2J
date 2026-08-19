using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class L2InstancedPropRenderer : MonoBehaviour
{
    private const int DrawChunk = 1023;

    [SerializeField] private L2InstancedPropSet _propSet;
    [SerializeField] private int _layer = 15;
    [SerializeField] private float _cellSize = 80f;
    [SerializeField] private float _maxDrawDistance = 30f;
    [SerializeField] private bool _receiveShadows;

    private readonly Matrix4x4[] _drawBuffer = new Matrix4x4[DrawChunk];
    private List<Cell> _cells;
    private Transform _transform;

    private struct Cell
    {
        public Mesh mesh;
        public Material material;
        public ShadowCastingMode shadows;
        public Vector3 center;
        public float radius;
        public Matrix4x4[] matrices;
    }

    public L2InstancedPropSet PropSet
    {
        get { return _propSet; }
        set { _propSet = value; }
    }

    public float MaxDrawDistance
    {
        get { return _maxDrawDistance; }
        set { _maxDrawDistance = value; }
    }

    public int Layer
    {
        get { return _layer; }
        set { _layer = value; }
    }

    private void OnEnable()
    {
        _transform = transform;
        BuildCells();
    }

    private void LateUpdate()
    {
        if (_cells == null || _cells.Count == 0)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float maxDistance = _maxDrawDistance > 0f ? _maxDrawDistance : camera.farClipPlane;
        Vector3 cameraPos = camera.transform.position;
        Matrix4x4 world = _transform.localToWorldMatrix;

        for (int i = 0; i < _cells.Count; i++)
        {
            Cell cell = _cells[i];
            Vector3 worldCenter = world.MultiplyPoint3x4(cell.center);
            if ((cameraPos - worldCenter).sqrMagnitude > (maxDistance + cell.radius) * (maxDistance + cell.radius))
            {
                continue;
            }

            DrawCell(cell, world);
        }
    }

    private void DrawCell(Cell cell, Matrix4x4 world)
    {
        int remaining = cell.matrices.Length;
        int offset = 0;
        while (remaining > 0)
        {
            int count = remaining < DrawChunk ? remaining : DrawChunk;
            for (int i = 0; i < count; i++)
            {
                _drawBuffer[i] = world * cell.matrices[offset + i];
            }

            Graphics.DrawMeshInstanced(
                cell.mesh,
                0,
                cell.material,
                _drawBuffer,
                count,
                null,
                cell.shadows,
                _receiveShadows,
                _layer);

            offset += count;
            remaining -= count;
        }
    }

    private void BuildCells()
    {
        _cells = new List<Cell>();
        if (_propSet == null || _propSet.batches == null || _cellSize <= 0f)
        {
            return;
        }

        float invCell = 1f / _cellSize;
        var buckets = new Dictionary<CellKey, List<Matrix4x4>>();
        var meta = new Dictionary<CellKey, BatchMeta>();

        for (int b = 0; b < _propSet.batches.Length; b++)
        {
            L2InstancedPropBatch batch = _propSet.batches[b];
            if (batch == null || batch.mesh == null || batch.material == null || batch.matrices == null)
            {
                continue;
            }

            if (!batch.material.enableInstancing)
            {
                batch.material.enableInstancing = true;
            }

            float meshRadius = batch.mesh.bounds.extents.magnitude;
            for (int i = 0; i < batch.matrices.Length; i++)
            {
                Vector3 pos = batch.matrices[i].GetColumn(3);
                int cx = Mathf.FloorToInt(pos.x * invCell);
                int cz = Mathf.FloorToInt(pos.z * invCell);
                var key = new CellKey(b, cx, cz);
                if (!buckets.TryGetValue(key, out List<Matrix4x4> list))
                {
                    list = new List<Matrix4x4>(64);
                    buckets.Add(key, list);
                    meta.Add(key, new BatchMeta
                    {
                        mesh = batch.mesh,
                        material = batch.material,
                        shadows = batch.castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        meshRadius = meshRadius
                    });
                }

                list.Add(batch.matrices[i]);
            }
        }

        foreach (var pair in buckets)
        {
            CellKey key = pair.Key;
            List<Matrix4x4> matrices = pair.Value;
            BatchMeta info = meta[key];
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < matrices.Count; i++)
            {
                Vector3 p = matrices[i].GetColumn(3);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Vector3 center = (min + max) * 0.5f;
            float radius = Vector3.Distance(min, max) * 0.5f + info.meshRadius;
            _cells.Add(new Cell
            {
                mesh = info.mesh,
                material = info.material,
                shadows = info.shadows,
                center = center,
                radius = radius,
                matrices = matrices.ToArray()
            });
        }
    }

    private struct CellKey : System.IEquatable<CellKey>
    {
        public readonly int batch;
        public readonly int x;
        public readonly int z;

        public CellKey(int batch, int x, int z)
        {
            this.batch = batch;
            this.x = x;
            this.z = z;
        }

        public bool Equals(CellKey other)
        {
            return batch == other.batch && x == other.x && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is CellKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (batch * 73856093) ^ (x * 19349663) ^ (z * 83492791);
        }
    }

    private struct BatchMeta
    {
        public Mesh mesh;
        public Material material;
        public ShadowCastingMode shadows;
        public float meshRadius;
    }
}
