using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Unity stand-in for AActor::SetAlphaTexModifier(uchar).
/// Swaps the live Lit instance onto L2/Actor/Fade, then drives _ActorAlpha.
/// Game-scope singleton: shader is resolved when the container builds.
/// </summary>
public sealed class L2ActorFade
{
    public const string ShaderName = "L2/Actor/Fade";
    public const float DurationSeconds = 2f;
    public const float AlphaPerSecond = 127.5f;

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

    public byte AlphaByte(float elapsed)
    {
        float value = 255f - elapsed * AlphaPerSecond;
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

    public bool TryBegin(Entity entity, out Renderer[] renderers, out Material[][] instances)
    {
        renderers = null;
        instances = null;
        if (entity == null || entity.gameObject == null || _shader == null)
        {
            return false;
        }

        Renderer[] found = entity.gameObject.GetComponentsInChildren<Renderer>(true);
        int count = 0;
        for (int i = 0; i < found.Length; i++)
        {
            if (IsFadeRenderer(found[i]))
            {
                count++;
            }
        }

        if (count == 0)
        {
            return false;
        }

        renderers = new Renderer[count];
        instances = new Material[count][];
        int write = 0;
        for (int i = 0; i < found.Length; i++)
        {
            Renderer renderer = found[i];
            if (!IsFadeRenderer(renderer))
            {
                continue;
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

        SetAlpha(instances, 1f);
        return true;
    }

    public void SetAlphaByte(Material[][] instances, byte alphaByte)
    {
        SetAlpha(instances, alphaByte / 255f);
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
