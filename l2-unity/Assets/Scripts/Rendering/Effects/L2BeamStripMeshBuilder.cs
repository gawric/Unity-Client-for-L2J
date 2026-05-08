using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class L2MultiLayerBeamBuilder : MonoBehaviour
{
    [Header("Structure Settings")]
    [SerializeField, Range(1, 8)] private int _layerCount = 5;
    [SerializeField, Min(1)] private int _segments = 32;
    [SerializeField] private float _height = 10f;
    [SerializeField] private float _innerRadius = 0.2f;
    [SerializeField] private float _outerRadius = 2.0f;
    [SerializeField] private Vector2 _textureTiling = new Vector2(1, 1); // Добавили тайлинг

    [Header("Wobble (Noise) Settings")]
    [SerializeField] private float _wobbleSpeed = 2f;
    [SerializeField] private float _wobbleAmplitude = 0.5f;
    [SerializeField] private float _wobbleFrequency = 1.5f;

    private Mesh _runtimeMesh;
    private Vector3[] _baseVertices;
    private Vector3[] _displacedVertices; // Кэшируем массив для оптимизации

    private void Awake()
    {
        GenerateMesh();
    }

    private void Update()
    {
        UpdateWobble();
    }

    private void GenerateMesh()
    {
        _runtimeMesh = new Mesh { name = "L2_Beam_Procedural" };
        _runtimeMesh.MarkDynamic(); // Указываем Unity, что меш будет часто обновляться

        int vertsPerPlane = (_segments + 1) * 2;
        int totalVertices = _layerCount * 2 * vertsPerPlane;

        Vector3[] vertices = new Vector3[totalVertices];
        Vector2[] uvs = new Vector2[totalVertices];
        int[] triangles = new int[_layerCount * 2 * _segments * 6];

        int vIndex = 0;
        int tIndex = 0;

        for (int l = 0; l < _layerCount; l++)
        {
            float currentRadius = (_layerCount > 1)
                ? Mathf.Lerp(_innerRadius, _outerRadius, (float)l / (_layerCount - 1))
                : _innerRadius;

            for (int p = 0; p < 2; p++)
            {
                // Поворот плоскостей креста (0 и 90 градусов)
                float rotationAngle = p * 90f * Mathf.Deg2Rad;
                Vector3 localDir = new Vector3(Mathf.Cos(rotationAngle), 0, Mathf.Sin(rotationAngle)) * currentRadius;

                for (int s = 0; s <= _segments; s++)
                {
                    float t = (float)s / _segments;
                    float y = t * _height;

                    vertices[vIndex] = (-localDir) + new Vector3(0, y, 0);
                    vertices[vIndex + 1] = (localDir) + new Vector3(0, y, 0);

                    // Настройка UV согласно данным из RenderDoc
                    uvs[vIndex] = new Vector2(0f, t * _textureTiling.y);
                    uvs[vIndex + 1] = new Vector2(_textureTiling.x, t * _textureTiling.y);

                    if (s < _segments)
                    {
                        // Упорядочиваем треугольники (Clockwise)
                        triangles[tIndex++] = vIndex;
                        triangles[tIndex++] = vIndex + 2;
                        triangles[tIndex++] = vIndex + 1;

                        triangles[tIndex++] = vIndex + 1;
                        triangles[tIndex++] = vIndex + 2;
                        triangles[tIndex++] = vIndex + 3;
                    }
                    vIndex += 2;
                }
            }
        }

        _runtimeMesh.vertices = vertices;
        _runtimeMesh.uv = uvs;
        _runtimeMesh.triangles = triangles;
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.bounds = new Bounds(transform.position + Vector3.up * (_height / 2), new Vector3(20, _height + 10, 20));

        GetComponent<MeshFilter>().sharedMesh = _runtimeMesh;
        _baseVertices = vertices;
        _displacedVertices = new Vector3[vertices.Length];
    }

    private void UpdateWobble()
    {
        if (_baseVertices == null) return;

        float timeOffset = Time.time * _wobbleSpeed;

        for (int i = 0; i < _baseVertices.Length; i++)
        {
            Vector3 v = _baseVertices[i];

            // Используем Y координату вершины для создания волнообразного эффекта
            // Плюс небольшое смещение по слоям, чтобы они не двигались синхронно
            float noiseInput = v.y * _wobbleFrequency + timeOffset;

            float noiseX = Mathf.PerlinNoise(noiseInput, i * 0.01f) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(i * 0.01f, noiseInput) - 0.5f;

            v.x += noiseX * _wobbleAmplitude;
            v.z += noiseZ * _wobbleAmplitude;

            _displacedVertices[i] = v;
        }

        _runtimeMesh.vertices = _displacedVertices;
    }
}