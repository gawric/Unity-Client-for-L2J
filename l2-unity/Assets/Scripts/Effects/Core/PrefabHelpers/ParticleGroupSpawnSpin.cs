using UnityEngine;

/// <summary>
/// UE SpriteEmitter StartSpinRange: random billboard spin at spawn without moving trail XYZ.
/// VampiricTouchSpark rotates quad vertices in the shader (not transform.localRotation).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleGroup))]
public sealed class ParticleGroupSpawnSpin : MonoBehaviour
{
    private static readonly int SpinParticlesId = Shader.PropertyToID("_SpinParticles");
    private static readonly int SpinsPerSecondRangeId = Shader.PropertyToID("_SpinsPerSecondRange");
    private static readonly int StartSpinRangeId = Shader.PropertyToID("_StartSpinRange");

    [SerializeField] private Vector2 _startSpinRevolutions = new Vector2(0f, 1f);
    [SerializeField] private bool _lockSpinAtSpawn = true;
    [SerializeField] private bool _usePresetAngles;
    [Tooltip("Preset spin steps (revolutions) similar to varied atlas orientation in original.")]
    [SerializeField] private float[] _presetSpinRevolutions = { 0f, 0.25f, 0.5f, 0.75f };

    public void Apply(Renderer renderer, float seed)
    {
        if (renderer == null)
        {
            return;
        }

        float spinRev = ResolveSpinRevolutions(seed);
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material mat = materials[i];
            if (mat == null)
            {
                continue;
            }

            if (mat.HasProperty(SpinParticlesId))
            {
                mat.SetFloat(SpinParticlesId, 1f);
            }

            if (mat.HasProperty(SpinsPerSecondRangeId))
            {
                mat.SetColor(SpinsPerSecondRangeId, new Color(0f, 0f, 0f, 0f));
            }

            if (!mat.HasProperty(StartSpinRangeId))
            {
                continue;
            }

            if (_lockSpinAtSpawn)
            {
                mat.SetColor(StartSpinRangeId, new Color(spinRev, spinRev, 0f, 0f));
            }
            else
            {
                mat.SetColor(StartSpinRangeId, new Color(_startSpinRevolutions.x, _startSpinRevolutions.y, 0f, 0f));
            }
        }
    }

    private float ResolveSpinRevolutions(float seed)
    {
        if (_usePresetAngles && _presetSpinRevolutions != null && _presetSpinRevolutions.Length > 0)
        {
            int idx = Mathf.Abs(Mathf.FloorToInt(seed * 12.9898f)) % _presetSpinRevolutions.Length;
            return Mathf.Repeat(_presetSpinRevolutions[idx], 1f);
        }

        float t = Mathf.Repeat(Mathf.Abs(seed) * 0.6180339887f, 1f);
        return Mathf.Lerp(_startSpinRevolutions.x, _startSpinRevolutions.y, t);
    }
}
