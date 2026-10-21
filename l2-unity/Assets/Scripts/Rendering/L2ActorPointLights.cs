using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Uploads up to five local lights into actor vertex color.
/// Each character gets the five nearest torches that reach him.
/// A camera-wide fallback covers a pawn for the moment before his renderer is found.
/// </summary>
public static class L2ActorPointLights
{
    public const int Max = 5;

    // Снимок комнаты применил один цвет. Слоты 1–4 остаются нулём. Шейдер их тоже не складывает.
    const int PassCount = 1;
    const float RefreshSec = 1f;

    static readonly List<L2ActorLocalLight> Lights = new List<L2ActorLocalLight>(32);
    static readonly List<ActorGroup> Groups = new List<ActorGroup>(32);
    static readonly Dictionary<int, ActorGroup> GroupsByRoot = new Dictionary<int, ActorGroup>(64);
    static readonly int[] Chosen = new int[Max];

    static readonly Vector4[] GlobalPos = new Vector4[Max];
    static readonly Vector4[] GlobalColor = new Vector4[Max];
    static readonly Vector4[] GlobalAtten = new Vector4[Max];

    static readonly int PosId = Shader.PropertyToID("_L2ActorPointPos");
    static readonly int ColorId = Shader.PropertyToID("_L2ActorPointColor");
    static readonly int AttenId = Shader.PropertyToID("_L2ActorPointAtten");

    static float _nextRefresh = -1f;
    static float _nextLog = -1f;
    static bool _rewriteActors;
    const float MoveEpsilonSqr = 0.25f;

    // Captured indoor fire, monster search 20260929_165643.
    static readonly Vector3 ClientC227 = new Vector3(0.130504623f, 0.105057523f, 0.0745209977f);

    public static void Push()
    {
        if (Time.unscaledTime >= _nextRefresh)
        {
            Refresh();
            _nextRefresh = Time.unscaledTime + RefreshSec;
        }

        Vector3 origin = Vector3.zero;
        Camera cam = Camera.main;
        if (cam != null)
            origin = cam.transform.position;
        Fill(origin, GlobalPos, GlobalColor, GlobalAtten);
        Shader.SetGlobalVectorArray(PosId, GlobalPos);
        Shader.SetGlobalVectorArray(ColorId, GlobalColor);
        Shader.SetGlobalVectorArray(AttenId, GlobalAtten);

        for (int g = 0; g < Groups.Count; g++)
        {
            ActorGroup group = Groups[g];
            if (group.Root == null)
                continue;

            Vector3 actorOrigin = group.Root.position;
            bool moved = (actorOrigin - group.AppliedOrigin).sqrMagnitude >= MoveEpsilonSqr;
            if (!_rewriteActors && group.Applied && !moved && (group.AppliedChosen > 0 || !NearLight(actorOrigin)))
                continue;

            int chosen = Fill(actorOrigin, group.Pos, group.Color, group.Atten);
            if (!_rewriteActors && group.Applied && !moved && chosen == group.AppliedChosen)
                continue;

            for (int r = 0; r < group.Renderers.Count; r++)
            {
                Renderer renderer = group.Renderers[r];
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(group.Block);
                group.Block.SetVectorArray(PosId, group.Pos);
                group.Block.SetVectorArray(ColorId, group.Color);
                group.Block.SetVectorArray(AttenId, group.Atten);
                renderer.SetPropertyBlock(group.Block);
            }

            group.Applied = true;
            group.AppliedChosen = chosen;
            group.AppliedOrigin = actorOrigin;
        }

        _rewriteActors = false;

        if (Time.unscaledTime >= _nextLog)
        {
            _nextLog = Time.unscaledTime + 2f;
            // Debug.Log(NearestReport());
        }
    }

    public static void AppendCapture(StringBuilder text)
    {
        if (Time.unscaledTime >= _nextRefresh)
            Refresh();
        text.Append(FullReport());
    }

    static int Select(Vector3 origin)
    {
        int chosen = 0;
        for (int slot = 0; slot < PassCount; slot++)
        {
            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Lights.Count; i++)
            {
                L2ActorLocalLight light = Lights[i];
                if (!Usable(light) || AlreadyChosen(i, chosen))
                    continue;
                float radius = light.radius;
                float dist = (light.WorldPosition - origin).sqrMagnitude;
                if (dist > radius * radius || dist >= bestDist)
                    continue;
                best = i;
                bestDist = dist;
            }

            if (best < 0)
                break;
            Chosen[chosen] = best;
            chosen++;
        }

        return chosen;
    }

    static int Fill(Vector3 origin, Vector4[] pos, Vector4[] color, Vector4[] atten)
    {
        int chosen = Select(origin);

        for (int i = 0; i < Max; i++)
        {
            if (i >= chosen)
            {
                pos[i] = Vector4.zero;
                color[i] = Vector4.zero;
                atten[i] = Vector4.zero;
                continue;
            }

            L2ActorLocalLight light = Lights[Chosen[i]];
            Vector3 p = light.WorldPosition;
            Color c = light.ReadyColor;
            pos[i] = new Vector4(p.x, p.y, p.z, 1f);
            color[i] = new Vector4(c.r, c.g, c.b, 1f);
            atten[i] = new Vector4(light.constantAttenuation, light.linearAttenuation, light.radius, 0f);
        }

        return chosen;
    }

    static bool NearLight(Vector3 origin)
    {
        for (int i = 0; i < Lights.Count; i++)
        {
            L2ActorLocalLight light = Lights[i];
            if (!Usable(light))
                continue;
            float reach = light.radius + 0.5f;
            if ((light.WorldPosition - origin).sqrMagnitude <= reach * reach)
                return true;
        }

        return false;
    }

    static bool Usable(L2ActorLocalLight light)
    {
        return light != null && light.IsActive;
    }

    static bool AlreadyChosen(int index, int chosen)
    {
        for (int i = 0; i < chosen; i++)
        {
            if (Chosen[i] == index)
                return true;
        }
        return false;
    }

    static void Refresh()
    {
        int previousLights = Lights.Count;
        Lights.Clear();
        L2ActorLocalLight[] found = Object.FindObjectsByType<L2ActorLocalLight>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
                Lights.Add(found[i]);
        }

        if (Lights.Count != previousLights)
            _rewriteActors = true;

        PruneActors();
        AdoptActors();
    }

    static void PruneActors()
    {
        for (int i = Groups.Count - 1; i >= 0; i--)
        {
            ActorGroup group = Groups[i];
            if (group.Root != null)
                continue;
            GroupsByRoot.Remove(group.RootId);
            Groups.RemoveAt(i);
        }
    }

    static void AdoptActors()
    {
        Animator[] animators = Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
                continue;
            Transform root = animator.transform;
            int rootId = root.GetInstanceID();
            if (GroupsByRoot.ContainsKey(rootId))
                continue;
            if (animator.GetComponentInParent<Animator>() != animator)
                continue;

            SkinnedMeshRenderer[] skinned = animator.GetComponentsInChildren<SkinnedMeshRenderer>(false);
            ActorGroup group = null;
            for (int s = 0; s < skinned.Length; s++)
            {
                SkinnedMeshRenderer renderer = skinned[s];
                if (!IsActor(renderer))
                    continue;
                if (group == null)
                {
                    group = new ActorGroup();
                    group.Root = root;
                    group.RootId = rootId;
                }

                group.Renderers.Add(renderer);
            }

            if (group == null)
                continue;
            GroupsByRoot.Add(rootId, group);
            Groups.Add(group);
        }
    }

    static bool IsActor(Renderer renderer)
    {
        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null || material.shader == null)
                continue;
            string name = material.shader.name;
            if (name == "L2/World/ActorModulate2X" || name == "L2/World/ActorEnvSpecular")
                return true;
        }
        return false;
    }

    static string NearestReport()
    {
        Camera cam = Camera.main;
        Vector3 origin = cam != null ? cam.transform.position : Vector3.zero;
        ActorGroup nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < Groups.Count; i++)
        {
            ActorGroup group = Groups[i];
            if (group.Root == null)
                continue;
            float dist = (group.Origin - origin).sqrMagnitude;
            if (dist >= best)
                continue;
            best = dist;
            nearest = group;
        }

        var text = new StringBuilder();
        text.Append("[ActorPoint] localLights=").Append(Lights.Count);
        text.Append(" actors=").Append(Groups.Count);
        if (nearest == null)
        {
            text.Append(" no actor renderer");
            return text.ToString();
        }

        AppendActor(text, nearest, cam);
        return text.ToString();
    }

    static string FullReport()
    {
        var text = new StringBuilder();
        text.Append("pointSlots=").Append(Max);
        text.Append(" localLights=").Append(Lights.Count);
        text.Append(" actors=").Append(Groups.Count).Append('\n');
        text.Append("client_c227=").Append(Vec(ClientC227)).Append('\n');
        text.Append("note=ready is light.color * intensity. The vertex adds ready * NdotL / (a + b*meters), then saturates with ambient and sun.\n");
        text.Append("note=contrib is that term at NdotL=1. color0_ceiling adds sun NdotL=1 as well, so it is a ceiling, not one real vertex.\n");
        text.Append("note=whiteTex is saturate(color0 * 2), the modulate2x result on a white texel.\n");

        for (int i = 0; i < Lights.Count; i++)
        {
            L2ActorLocalLight light = Lights[i];
            if (light == null)
                continue;
            Color src = light.SourceColor;
            Color ready = light.ReadyColor;
            text.Append("light ").Append(i).Append(' ').Append(light.name);
            text.Append(" active=").Append(light.IsActive ? 1 : 0);
            text.Append(" pos=").Append(Vec(light.WorldPosition));
            text.Append(" unityColor=").Append(Vec(src));
            text.Append(" intensity=").Append(Num(light.Intensity));
            text.Append(" ready=").Append(Vec(ready));
            text.Append(" a=").Append(Num(light.constantAttenuation));
            text.Append(" b=").Append(Num(light.linearAttenuation));
            text.Append(" radius=").Append(Num(light.radius));
            text.Append(" unityRange=").Append(Num(light.UnityRange));
            text.Append('\n');
        }

        Camera cam = Camera.main;
        for (int i = 0; i < Groups.Count; i++)
        {
            ActorGroup group = Groups[i];
            if (group.Root == null)
                continue;
            if (cam != null && (group.Origin - cam.transform.position).sqrMagnitude > 40f * 40f)
                continue;
            AppendActor(text, group, cam);
        }

        return text.ToString();
    }

    static void AppendActor(StringBuilder text, ActorGroup group, Camera cam)
    {
        Vector3 origin = group.Origin;
        int chosen = Select(origin);
        Color ambient = Shader.GetGlobalColor("_L2ActorAmbientHalf");
        Color plane = Shader.GetGlobalColor("_L2HsvActorLight");
        float sunScale = Shader.GetGlobalFloat("_L2ActorDirectionalScale");
        if (sunScale <= 0f)
            sunScale = 0.7f;
        Vector3 sun = new Vector3(plane.r * sunScale, plane.g * sunScale, plane.b * sunScale);
        Vector3 sum = Vector3.zero;

        text.Append("actor ").Append(group.Root.name);
        text.Append(" origin=").Append(Vec(origin));
        if (cam != null)
            text.Append(" camDist=").Append(Num(Vector3.Distance(origin, cam.transform.position)));
        text.Append(" renderers=").Append(group.Renderers.Count);
        text.Append(" slots=").Append(chosen);
        text.Append(" c251=").Append(Vec(ambient));
        text.Append(" c250=").Append(Vec(sun));
        text.Append('\n');

        float nearestDist = 0f;
        for (int s = 0; s < chosen; s++)
        {
            L2ActorLocalLight light = Lights[Chosen[s]];
            float dist = Vector3.Distance(origin, light.WorldPosition);
            if (s == 0)
                nearestDist = dist;
            float atten = 1f / Mathf.Max(light.constantAttenuation + light.linearAttenuation * dist, 1e-5f);
            Color ready = light.ReadyColor;
            Vector3 contrib = new Vector3(ready.r, ready.g, ready.b) * atten;
            sum += contrib;
            text.Append("  slot").Append(s).Append(' ').Append(light.name);
            text.Append(" d=").Append(Num(dist)).Append("m");
            text.Append(" uu=").Append(Num(dist * 52.5f));
            text.Append(" intensity=").Append(Num(light.Intensity));
            text.Append(" ready=").Append(Vec(ready));
            text.Append(" atten=").Append(Num(atten));
            text.Append(" contrib=").Append(Vec(contrib));
            text.Append('\n');
        }

        Vector3 color0 = new Vector3(
            Mathf.Clamp01(ambient.r + sun.x + sum.x),
            Mathf.Clamp01(ambient.g + sun.y + sum.y),
            Mathf.Clamp01(ambient.b + sun.z + sum.z));
        Vector3 white = new Vector3(
            Mathf.Clamp01(color0.x * 2f),
            Mathf.Clamp01(color0.y * 2f),
            Mathf.Clamp01(color0.z * 2f));
        text.Append("  sumNdotl1=").Append(Vec(sum));
        text.Append(" color0_ceiling=").Append(Vec(color0));
        text.Append(" whiteTex=").Append(Vec(white));
        if (chosen > 0)
        {
            float clientAtten = 1f / Mathf.Max(0.431085169f + 0.021897854f * nearestDist, 1e-5f);
            text.Append(" clientOneAtNearest=").Append(Vec(ClientC227 * clientAtten));
        }
        text.Append('\n');
    }

    static string Num(float v)
    {
        return v.ToString("0.000", CultureInfo.InvariantCulture);
    }

    static string Vec(Color c)
    {
        return Num(c.r) + " " + Num(c.g) + " " + Num(c.b);
    }

    static string Vec(Vector3 v)
    {
        return Num(v.x) + " " + Num(v.y) + " " + Num(v.z);
    }

    sealed class ActorGroup
    {
        public Transform Root;
        public int RootId;
        public bool Applied;
        public int AppliedChosen;
        public Vector3 AppliedOrigin;
        public readonly List<Renderer> Renderers = new List<Renderer>(4);
        public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        public readonly Vector4[] Pos = new Vector4[Max];
        public readonly Vector4[] Color = new Vector4[Max];
        public readonly Vector4[] Atten = new Vector4[Max];

        public Vector3 Origin
        {
            get
            {
                Vector3 sum = Vector3.zero;
                int count = 0;
                for (int i = 0; i < Renderers.Count; i++)
                {
                    Renderer renderer = Renderers[i];
                    if (renderer == null)
                        continue;
                    sum += renderer.bounds.center;
                    count++;
                }

                return count > 0 ? sum / count : Root.position;
            }
        }
    }
}
