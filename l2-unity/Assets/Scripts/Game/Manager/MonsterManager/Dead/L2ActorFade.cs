using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Unity stand-in for AActor::SetAlphaTexModifier(uchar).
/// Swaps body materials onto L2/Actor/Fade and drives _ActorAlpha.
/// </summary>
public sealed class L2ActorFade
{
    public const string ShaderName = "L2/Actor/Fade";
    public const float DurationSeconds = 2f;
    public const float AlphaPerSecond = 127.5f;
    public const byte AppearStartAlpha = 1;

    static readonly int ActorAlphaId = Shader.PropertyToID("_ActorAlpha");
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    readonly Shader _shader;

    public L2ActorFade()
    {
        _shader = Shader.Find(ShaderName);
        if (_shader == null)
        {
            Debug.LogError("[L2ActorFade] Shader not found: " + ShaderName);
        }
    }

    public Shader FadeShader
    {
        get { return _shader; }
    }

    /// <summary>Death / logout fade-out: 255 → 0 over DurationSeconds.</summary>
    public byte AlphaByte(float elapsed)
    {
        return ClampAlphaByte(255f - elapsed * AlphaPerSecond);
    }

    /// <summary>CharInfo appear fade-in: start at uchar 1, +127.5/s for ~2s.</summary>
    public byte AppearAlphaByte(float elapsed)
    {
        return ClampAlphaByte(AppearStartAlpha + elapsed * AlphaPerSecond);
    }

    public bool TryBegin(Entity entity, out Renderer[] renderers, out Material[][] instances)
    {
        return TryBegin(entity, 255, false, out renderers, out instances, out _);
    }

    public bool TryBegin(
        Entity entity,
        byte startAlphaByte,
        out Renderer[] renderers,
        out Material[][] instances,
        out Material[][] sharedBackup)
    {
        return TryBegin(entity, startAlphaByte, true, out renderers, out instances, out sharedBackup);
    }

    public void SetAlphaByte(Material[][] instances, byte alphaByte)
    {
        SetAlpha(instances, alphaByte / 255f);
    }

    /// <summary>Drop fade instances and put shared materials back (ClearAlphaTexModifier).</summary>
    public void Restore(Renderer[] renderers, Material[][] sharedBackup, Material[][] instances)
    {
        if (renderers == null || sharedBackup == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            Material[] shared = i < sharedBackup.Length ? sharedBackup[i] : null;
            if (renderer == null || shared == null)
            {
                continue;
            }

            renderer.sharedMaterials = shared;

            Material[] fadeMats = instances != null && i < instances.Length ? instances[i] : null;
            if (fadeMats == null)
            {
                continue;
            }

            for (int m = 0; m < fadeMats.Length; m++)
            {
                DestroyIfNotShared(fadeMats[m], shared);
            }
        }
    }

    bool TryBegin(
        Entity entity,
        byte startAlphaByte,
        bool captureShared,
        out Renderer[] renderers,
        out Material[][] instances,
        out Material[][] sharedBackup)
    {
        renderers = null;
        instances = null;
        sharedBackup = null;
        if (entity == null || entity.gameObject == null || _shader == null)
        {
            return false;
        }

        Renderer[] found = entity.gameObject.GetComponentsInChildren<Renderer>(true);
        int count = CountFadeRenderers(found);
        if (count == 0)
        {
            return false;
        }

        renderers = new Renderer[count];
        instances = new Material[count][];
        if (captureShared)
        {
            sharedBackup = new Material[count][];
        }

        int write = 0;
        for (int i = 0; i < found.Length; i++)
        {
            Renderer renderer = found[i];
            if (!IsFadeRenderer(renderer))
            {
                continue;
            }

            if (captureShared)
            {
                sharedBackup[write] = renderer.sharedMaterials;
            }

            Material[] mats = renderer.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                ApplyFadeShader(mats[m]);
            }

            renderers[write] = renderer;
            instances[write] = mats;
            write++;
        }

        SetAlphaByte(instances, startAlphaByte);
        return true;
    }

    static int CountFadeRenderers(Renderer[] found)
    {
        int count = 0;
        for (int i = 0; i < found.Length; i++)
        {
            if (IsFadeRenderer(found[i]))
            {
                count++;
            }
        }

        return count;
    }

    static byte ClampAlphaByte(float value)
    {
        if (value <= 0f)
        {
            return 0;
        }

        if (value >= 255f)
        {
            return 255;
        }

        return (byte)value;
    }

    static void SetAlpha(Material[][] instances, float alpha)
    {
        if (instances == null)
        {
            return;
        }

        for (int i = 0; i < instances.Length; i++)
        {
            Material[] mats = instances[i];
            if (mats == null)
            {
                continue;
            }

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                if (mat != null && mat.HasProperty(ActorAlphaId))
                {
                    mat.SetFloat(ActorAlphaId, alpha);
                }
            }
        }
    }

    static bool IsFadeRenderer(Renderer renderer)
    {
        if (renderer == null || !renderer.enabled)
        {
            return false;
        }

        if (renderer is ParticleSystemRenderer)
        {
            return false;
        }

        if (renderer.gameObject.name == "click_area")
        {
            return false;
        }

        return renderer is SkinnedMeshRenderer || renderer is MeshRenderer;
    }

    static void DestroyIfNotShared(Material instance, Material[] shared)
    {
        if (instance == null)
        {
            return;
        }

        for (int s = 0; s < shared.Length; s++)
        {
            if (instance == shared[s])
            {
                return;
            }
        }

        Object.Destroy(instance);
    }

    void ApplyFadeShader(Material mat)
    {
        if (mat == null)
        {
            return;
        }

        Texture baseMap = mat.HasProperty(BaseMapId) ? mat.GetTexture(BaseMapId) : null;
        Texture mainTex = mat.HasProperty(MainTexId) ? mat.GetTexture(MainTexId) : null;
        Color baseColor = mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;

        mat.shader = _shader;
        Texture albedo = baseMap != null ? baseMap : mainTex;
        if (albedo != null && mat.HasProperty(BaseMapId))
        {
            mat.SetTexture(BaseMapId, albedo);
        }

        if (mat.HasProperty(BaseColorId))
        {
            mat.SetColor(BaseColorId, baseColor);
        }

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetFloat(ActorAlphaId, 1f);
    }
}
