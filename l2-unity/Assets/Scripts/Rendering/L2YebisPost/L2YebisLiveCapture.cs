using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// One frame of the texture the live combine actually samples, plus the graded result.
/// Raw layout matches the client capture: file row 0 is the top of the picture.
/// </summary>
public static class L2YebisLiveCapture
{
    public const string Root =
        @"C:\games\Lineage II HighElfes\ADEU-P464-D20240703-P-240117-240724-1\system\logs\YebisCapture\unity_live";

    static int _request;
    static int _gradeStage;

    public static int GradeStage => _gradeStage;

    public static void CycleGradeStage()
    {
        _gradeStage = _gradeStage >= 4 ? 0 : _gradeStage + 1;
        // Debug.Log("[Yebis] F7 стадия " + _gradeStage + " " + GradeStageName(_gradeStage));
    }

    public static string GradeStageName(int stage)
    {
        switch (stage)
        {
            case 1: return "A выборка s0";
            case 2: return "B матрица, в RT значения выше 1 станут 255";
            case 3: return "C перед степенью 0.625";
            case 4: return "D после степени 0.625";
            default: return "полный выход, то же что D";
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InstallKey()
    {
        var go = new GameObject("L2YebisLiveCaptureKey")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<L2YebisLiveCaptureKey>();
    }

    public static void Request()
    {
        _request = 1;
    }

    public static bool Consume()
    {
        if (_request == 0)
        {
            return false;
        }

        _request = 0;
        return true;
    }

    public static string CreateFolder(float worldHours, string cameraFormat, string colorSpace)
    {
        string folder = Path.Combine(Root, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(folder);
        var state = new StringBuilder();
        state.AppendLine("role=unity-live");
        state.AppendLine("worldHours=" + worldHours.ToString("0.###", CultureInfo.InvariantCulture));
        state.AppendLine("colorSpace=" + colorSpace);
        state.AppendLine("cameraFormat=" + cameraFormat);
        state.AppendLine("bloom=0");
        state.AppendLine("captureFormula=1");
        state.AppendLine("glare=black");
        state.AppendLine("gradeStage=" + _gradeStage + " " + GradeStageName(_gradeStage));
        state.AppendLine("where=camera-color-before-combine");
        File.WriteAllText(Path.Combine(folder, "state.txt"), state.ToString(), Encoding.UTF8);
        File.WriteAllText(Path.Combine(Root, "latest.txt"), folder, Encoding.UTF8);
        // Debug.Log("[Yebis] live capture " + folder);
        return folder;
    }

    public static void ReadAfterFrame(string folder, RenderTexture scene, RenderTexture graded, RenderTexture control)
    {
        if (L2YebisLiveCaptureKey.Host == null)
        {
            // Debug.LogWarning("[Yebis] нет объекта клавиши, кадр не прочитан");
            return;
        }

        L2YebisLiveCaptureKey.Host.StartCoroutine(ReadWhenPresented(folder, scene, graded, control));
    }

    static IEnumerator ReadWhenPresented(string folder, RenderTexture scene, RenderTexture graded, RenderTexture control)
    {
        yield return new WaitForEndOfFrame();
        SaveControl(folder, control);
        SavePresented(folder, scene, "input", "unity-s0-before-combine", "Вход combine, 8-битный, как текстура камеры");
        SavePresented(folder, graded, "output", "unity-after-combine", "Выход combine, 8-битный");
        WriteProbe(folder, scene, graded);
        WriteTerrainColor0(folder, scene);
        // Debug.Log("[Yebis] live capture written " + folder);
    }

    static void WriteTerrainColor0(string folder, RenderTexture scene)
    {
        Camera cam = Camera.main;
        var text = new StringBuilder();
        text.AppendLine("role=unity-near-path-color0");
        text.AppendLine("Color0 треугольника под той же долей экрана, что и ближняя тропа клиента. Пиксель получает интерполяцию вершин.");
        text.AppendLine("shader_color0 повторяет вершинный шейдер: чёрный COLOR становится белым, белый обнуляется, дальше ambient, HSV и байт intensity этой точки.");
        if (cam == null)
        {
            text.AppendLine("camera=missing");
            File.WriteAllText(Path.Combine(folder, "terrain_color0.txt"), text.ToString(), Encoding.UTF8);
            return;
        }

        Vector4 origin = Shader.GetGlobalVector("_L2TerrainIntensityOrigin");
        Vector4 ambient = Shader.GetGlobalVector("_L2TerrainAmbient");
        Vector4 hsv = Shader.GetGlobalVector("_L2HsvTerrainLight");
        float fogEnd = Shader.GetGlobalFloat("_L2FogEnd");
        float fogScale = Shader.GetGlobalFloat("_L2FogScale");
        float fogEnable = Shader.GetGlobalFloat("_L2FogEnable");
        var intensityTex = Shader.GetGlobalTexture("_L2TerrainIntensityMap") as Texture2D;
        text.AppendLine("ambient " + Fmt(new Vector3(ambient.x, ambient.y, ambient.z)));
        text.AppendLine("hsv " + Fmt(new Vector3(hsv.x, hsv.y, hsv.z)));
        text.AppendLine(
            "intensity_origin " + origin.x.ToString("0.###", CultureInfo.InvariantCulture) + " " +
            origin.y.ToString("0.###", CultureInfo.InvariantCulture) + " cell " +
            origin.z.ToString("0.###", CultureInfo.InvariantCulture) + " grid " +
            origin.w.ToString("0.###", CultureInfo.InvariantCulture));
        text.AppendLine("intensity_texture=" + (intensityTex != null ? "yes" : "no"));

        var regions = new[]
        {
            new Region("near_left", 0.24f, 0.83f),
            new Region("far", 0.49f, 0.42f),
            new Region("near_right", 0.69f, 0.88f)
        };
        foreach (Region region in regions)
        {
            text.AppendLine("region=" + region.Name + " screen_from_top=" +
                region.X.ToString("0.000", CultureInfo.InvariantCulture) + "," +
                region.Y.ToString("0.000", CultureInfo.InvariantCulture));
            if (scene != null && scene.width > 1 && scene.height > 1)
            {
                int px = Mathf.Clamp(Mathf.RoundToInt(region.X * scene.width), 0, scene.width - 1);
                int pyTop = Mathf.Clamp(Mathf.RoundToInt(region.Y * scene.height), 0, scene.height - 1);
                int py = scene.height - 1 - pyTop;
                Color32 sample = ReadOne(scene, px, py);
                text.AppendLine("scene_before_curve " + sample.r + " " + sample.g + " " + sample.b +
                    " pixel " + px + "," + pyTop);
            }
            Ray ray = cam.ViewportPointToRay(new Vector3(region.X, 1f - region.Y, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, 400f);
            RaycastHit ground = default;
            bool found = false;
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].normal.y < 0.45f || hits[i].distance >= best)
                {
                    continue;
                }

                best = hits[i].distance;
                ground = hits[i];
                found = true;
            }

            if (!found)
            {
                text.AppendLine("hit=none");
                continue;
            }

            AppendHit(text, cam, ground, origin, ambient, hsv, intensityTex, fogEnd, fogScale, fogEnable);
        }

        File.WriteAllText(Path.Combine(folder, "terrain_color0.txt"), text.ToString(), Encoding.UTF8);
    }

    struct Region
    {
        public readonly string Name;
        public readonly float X;
        public readonly float Y;

        public Region(string name, float x, float y)
        {
            Name = name;
            X = x;
            Y = y;
        }
    }

    static void AppendHit(
        StringBuilder text,
        Camera cam,
        RaycastHit ground,
        Vector4 origin,
        Vector4 ambient,
        Vector4 hsv,
        Texture2D intensityTex,
        float fogEnd,
        float fogScale,
        float fogEnable)
    {
        Vector3 amb = new Vector3(ambient.x, ambient.y, ambient.z);
        Vector3 plane = new Vector3(hsv.x, hsv.y, hsv.z);
        float viewAbsZ = Mathf.Abs(cam.worldToCameraMatrix.MultiplyPoint(ground.point).z);
        float fogF = fogEnable > 0.5f ? Mathf.Clamp01((fogEnd - viewAbsZ) * fogScale) : 1f;
        text.AppendLine("hit_world " + Fmt(ground.point) + " viewAbsZ " + viewAbsZ.ToString("0.###", CultureInfo.InvariantCulture) +
            " fog_f " + fogF.ToString("0.###", CultureInfo.InvariantCulture));
        AppendPoint(text, "hit", ground.point, Vector3.zero, origin, amb, plane, intensityTex);

        MeshCollider meshCollider = ground.collider as MeshCollider;
        Mesh mesh = meshCollider != null ? meshCollider.sharedMesh : null;
        if (mesh == null || ground.triangleIndex < 0)
        {
            text.AppendLine("triangle=unavailable");
            return;
        }

        int[] tris;
        try
        {
            tris = mesh.triangles;
        }
        catch (Exception)
        {
            text.AppendLine("triangle=unreadable");
            return;
        }
        int tri = ground.triangleIndex * 3;
        if (tri + 2 >= tris.Length)
        {
            text.AppendLine("triangle=out_of_range");
            return;
        }

        Vector3[] vertices;
        Color32[] colors = null;
        try
        {
            vertices = mesh.vertices;
            if (mesh.HasVertexAttribute(VertexAttribute.Color))
            {
                colors = mesh.colors32;
            }
        }
        catch (Exception)
        {
            text.AppendLine("triangle=unreadable");
            return;
        }
        Vector3 bary = ground.barycentricCoordinate;
        Vector3 interpolated = Vector3.zero;
        for (int v = 0; v < 3; v++)
        {
            int index = tris[tri + v];
            Vector3 world = meshCollider.transform.TransformPoint(vertices[index]);
            Vector3 meshColor = Vector3.zero;
            if (colors != null && index < colors.Length)
            {
                meshColor = new Vector3(colors[index].r / 255f, colors[index].g / 255f, colors[index].b / 255f);
            }

            Vector3 color0 = TerrainShaderColor0(meshColor, amb, plane, Intensity01(world, origin, intensityTex));
            interpolated += color0 * bary[v];
            text.AppendLine(
                "v" + v + " world " + Fmt(world) +
                " mesh " + (colors != null && index < colors.Length
                    ? colors[index].r + " " + colors[index].g + " " + colors[index].b
                    : "none") +
                " shader_color0 " + Fmt(color0));
            AppendPoint(text, "v" + v, world, meshColor, origin, amb, plane, intensityTex);
        }

        text.AppendLine("pixel_color0 " + Fmt(interpolated));
    }

    static void AppendPoint(
        StringBuilder text,
        string tag,
        Vector3 world,
        Vector3 meshColor,
        Vector4 origin,
        Vector3 ambient,
        Vector3 hsv,
        Texture2D intensityTex)
    {
        float cell = Mathf.Max(origin.z, 1f);
        float grid = Mathf.Max(origin.w, 1f);
        float worldUuX = world.x * 52.5f;
        float worldUuY = world.z * 52.5f;
        float gx = (worldUuY - origin.x) / cell;
        float gy = (worldUuX - origin.y) / cell;
        int ix = Mathf.FloorToInt(gx);
        int iy = Mathf.FloorToInt(gy);
        int sectorX = ix >= 0 ? (ix / 16) * 16 : -(((-ix) + 15) / 16) * 16;
        int sectorY = iy >= 0 ? (iy / 16) * 16 : -(((-iy) + 15) / 16) * 16;
        int localX = ix - sectorX;
        int localY = iy - sectorY;
        int intensity = -1;
        int valid = 0;
        if (intensityTex != null && ix >= 0 && iy >= 0 && ix < intensityTex.width && iy < intensityTex.height)
        {
            Color32 pixel = intensityTex.GetPixel(ix, iy);
            intensity = pixel.r;
            valid = pixel.g;
        }

        float intensity01 = intensity >= 0 ? intensity / 255f : Shader.GetGlobalFloat("_L2TerrainIntensity");
        Vector3 color0 = TerrainShaderColor0(meshColor, ambient, hsv, intensity01);
        text.AppendLine(
            tag + "_cell gx=" + ix + " gy=" + iy +
            " sector=" + sectorX + "," + sectorY +
            " local=" + localX + "," + localY +
            " intensity_byte=" + intensity +
            " valid=" + valid +
            " color0 " + Fmt(color0));
    }

    static float Intensity01(Vector3 world, Vector4 origin, Texture2D intensityTex)
    {
        float cell = Mathf.Max(origin.z, 1f);
        float worldUuX = world.x * 52.5f;
        float worldUuY = world.z * 52.5f;
        int ix = Mathf.FloorToInt((worldUuY - origin.x) / cell);
        int iy = Mathf.FloorToInt((worldUuX - origin.y) / cell);
        if (intensityTex != null && ix >= 0 && iy >= 0 && ix < intensityTex.width && iy < intensityTex.height)
        {
            return intensityTex.GetPixel(ix, iy).r;
        }

        return Shader.GetGlobalFloat("_L2TerrainIntensity");
    }

    static Vector3 TerrainShaderColor0(Vector3 meshColor, Vector3 ambient, Vector3 hsv, float intensity01)
    {
        Vector3 vtx = meshColor;
        if (Mathf.Max(vtx.x, Mathf.Max(vtx.y, vtx.z)) < 0.001f)
        {
            vtx = Vector3.one;
        }

        float along = Mathf.Clamp01(intensity01);
        Vector3 sunByte = new Vector3(
            Mathf.Floor(hsv.x * 0.5f * along * 255f),
            Mathf.Floor(hsv.y * 0.5f * along * 255f),
            Mathf.Floor(hsv.z * 0.5f * along * 255f));
        Vector3 ambByte = new Vector3(
            Mathf.Floor(Mathf.Clamp01(ambient.x) * 255f + 0.5f),
            Mathf.Floor(Mathf.Clamp01(ambient.y) * 255f + 0.5f),
            Mathf.Floor(Mathf.Clamp01(ambient.z) * 255f + 0.5f));
        Vector3 ambShift = new Vector3(
            Mathf.Floor(ambByte.x * 0.5f),
            Mathf.Floor(ambByte.y * 0.5f),
            Mathf.Floor(ambByte.z * 0.5f));
        if (Mathf.Min(vtx.x, Mathf.Min(vtx.y, vtx.z)) > 0.98f)
        {
            vtx = Vector3.zero;
        }

        Vector3 baseByte = new Vector3(
            Mathf.Floor(Mathf.Clamp01(vtx.x) * 255f + 0.5f),
            Mathf.Floor(Mathf.Clamp01(vtx.y) * 255f + 0.5f),
            Mathf.Floor(Mathf.Clamp01(vtx.z) * 255f + 0.5f));
        float scale = Shader.GetGlobalFloat("_L2TerrainColor0Scale");
        if (scale < 0.001f)
        {
            scale = 1f;
        }

        Vector3 color0 = Vector3.Min(baseByte + sunByte + ambShift, Vector3.one * 255f) * (scale / 255f);
        return Vector3.Min(Vector3.Max(color0, Vector3.zero), Vector3.one);
    }

    static void WriteProbe(string folder, RenderTexture scene, RenderTexture graded)
    {
        if (scene == null || graded == null || scene.width < 8 || scene.height < 8)
        {
            return;
        }

        int x = scene.width / 2;
        int y = scene.height / 8;
        Color32 sample = ReadOne(scene, x, y);
        Color32 stored = ReadOne(graded, x, y);
        Vector3 a = new Vector3(sample.r / 255f, sample.g / 255f, sample.b / 255f);
        Vector3 b = CaptureMatrix(a);
        Vector3 filmic = CaptureFilmic(b);
        const float bias = 0.00499999523f;
        Vector3 beforePow = filmic + (Vector3.one - filmic) * bias;
        Vector3 gradedValue = CaptureGamma(beforePow);
        int stage = _gradeStage;
        Vector3 shown = stage == 1 ? a : stage == 2 ? b : stage == 3 ? beforePow : gradedValue;
        int fileRow = scene.height - 1 - y;
        var text = new StringBuilder();
        text.AppendLine("colorSpace=" + QualitySettings.activeColorSpace);
        text.AppendLine("sceneFormat=" + scene.graphicsFormat);
        text.AppendLine("gradeFormat=" + graded.graphicsFormat);
        text.AppendLine("stage=" + stage + " " + GradeStageName(stage));
        text.AppendLine("Точка ReadPixels, ноль снизу слева: x=" + x + " y=" + y);
        text.AppendLine("Та же точка в input.png, строка 0 сверху: x=" + x + " row=" + fileRow);
        text.AppendLine("Если режим A и персонаж стоит ногами вниз, точка на полу перед ним.");
        text.AppendLine("A байты " + sample.r + " " + sample.g + " " + sample.b);
        text.AppendLine("A " + Fmt(a));
        text.AppendLine("B матрица без обрезки " + Fmt(b));
        text.AppendLine("filmic " + Fmt(filmic));
        text.AppendLine("C перед степенью, с чёрным бликом 0.005 " + Fmt(beforePow));
        text.AppendLine("D после степени 0.625 " + Fmt(gradedValue));
        text.AppendLine("RT байты выхода " + stored.r + " " + stored.g + " " + stored.b);
        text.AppendLine("Ожидание записи этой стадии, байты " + ToByte(shown.x) + " " + ToByte(shown.y) + " " + ToByte(shown.z));
        text.AppendLine("Проект Gamma, формат камеры 8-битный: второй гаммы при записи в RT нет. Байт 255 значит, что значение стадии было выше 1.");
        File.WriteAllText(Path.Combine(folder, "probe.txt"), text.ToString(), Encoding.UTF8);
        // Debug.Log("[Yebis] probe A " + Fmt(a) + " B " + Fmt(b) + " C " + Fmt(beforePow) + " D " + Fmt(gradedValue) + " rt " + stored.r + " " + stored.g + " " + stored.b);
    }

    static Color32 ReadOne(RenderTexture rt, int x, int y)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(x, y, 1f, 1f), 0, 0, false);
        tex.Apply(false, false);
        RenderTexture.active = previous;
        Color32 pixel = tex.GetPixel(0, 0);
        UnityEngine.Object.Destroy(tex);
        return pixel;
    }

    static Vector3 CaptureMatrix(Vector3 scene)
    {
        const float bias = 0.00499999523f;
        const float floor = 5.96046448e-8f;
        float r = scene.x * 4.75511837f + scene.y * 0.177011937f + scene.z * 0.0178695004f + bias;
        float g = scene.x * 0.0526184812f + scene.y * 4.87951183f + scene.z * 0.0178695004f + bias;
        float b = scene.x * 0.0526184812f + scene.y * 0.177011937f + scene.z * 4.72036934f + bias;
        return new Vector3(Mathf.Max(r, floor), Mathf.Max(g, floor), Mathf.Max(b, floor));
    }

    static Vector3 CaptureFilmic(Vector3 x)
    {
        const float filmicA = 2.65814137f;
        const float filmicB = 0.664993107f;
        return new Vector3(FilmicChannel(x.x, filmicA, filmicB), FilmicChannel(x.y, filmicA, filmicB), FilmicChannel(x.z, filmicA, filmicB));
    }

    static float FilmicChannel(float x, float filmicA, float filmicB)
    {
        float e = Mathf.Exp(-filmicA * x);
        float oneMinusBe = 1f - filmicB * e;
        return Mathf.Clamp01((1f - e) * oneMinusBe * oneMinusBe);
    }

    static Vector3 CaptureGamma(Vector3 combined)
    {
        const float floor = 5.96046448e-8f;
        return new Vector3(GammaChannel(combined.x, floor), GammaChannel(combined.y, floor), GammaChannel(combined.z, floor));
    }

    static float GammaChannel(float value, float floor)
    {
        float safe = Mathf.Max(value, floor);
        return Mathf.Clamp01(Mathf.Exp(Mathf.Log(safe) * 0.625f));
    }

    static string Fmt(Vector3 value)
    {
        return value.x.ToString("0.####", CultureInfo.InvariantCulture) + " " +
               value.y.ToString("0.####", CultureInfo.InvariantCulture) + " " +
               value.z.ToString("0.####", CultureInfo.InvariantCulture);
    }

    static void SaveControl(string folder, RenderTexture rt)
    {
        if (rt == null)
        {
            // Debug.LogWarning("[Yebis] control rt null");
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, false);
        tex.Apply(false, false);
        RenderTexture.active = previous;
        Color32 pixel = tex.GetPixel(0, 0);
        int peak = 0;
        Color32[] pixels = tex.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            peak = Math.Max(peak, Math.Max(pixels[i].r, Math.Max(pixels[i].g, pixels[i].b)));
        }

        File.WriteAllText(
            Path.Combine(folder, "control.txt"),
            "format=" + rt.graphicsFormat + "\n" +
            "created=" + rt.IsCreated() + "\n" +
            "size=" + rt.width + "x" + rt.height + "\n" +
            "expected=0.8 0.2 0.05\n" +
            "pixel0=" + pixel.r + " " + pixel.g + " " + pixel.b + "\n" +
            "peak=" + peak + "\n",
            Encoding.UTF8);
        // Debug.Log("[Yebis] control " + rt.graphicsFormat + " " + rt.width + "x" + rt.height + " created=" + rt.IsCreated() + " pixel0=" + pixel.r + "," + pixel.g + "," + pixel.b + " peak=" + peak);
        UnityEngine.Object.Destroy(tex);
    }

    static void SaveHdr(string folder, RenderTexture rt, string name, string where, string title)
    {
        if (rt == null)
        {
            // Debug.LogWarning("[Yebis] " + name + " rt null");
            return;
        }

        AsyncGPUReadbackRequest request = AsyncGPUReadback.Request(rt, 0, GraphicsFormat.R32G32B32A32_SFloat);
        request.WaitForCompletion();
        if (request.hasError)
        {
            // Debug.LogWarning("[Yebis] " + name + " float readback failed " + rt.graphicsFormat);
            return;
        }

        NativeArray<float> data = request.GetData<float>();
        int width = rt.width;
        int height = rt.height;
        int count = width * height;
        if (data.Length < count * 4)
        {
            // Debug.LogWarning("[Yebis] " + name + " float buffer " + data.Length);
            return;
        }

        var file = new byte[count * 16];
        var preview = new Color32[count];
        float minR = float.MaxValue, minG = float.MaxValue, minB = float.MaxValue;
        float maxR = float.MinValue, maxG = float.MinValue, maxB = float.MinValue;
        int above = 0;
        int near = 0;
        ulong hash = 14695981039346656037ul;
        int bandCap = (height / 3 + 1) * width;
        var bands = new Band[3];
        for (int i = 0; i < 3; i++)
        {
            bands[i] = Band.Create(bandCap);
        }

        for (int y = 0; y < height; y++)
        {
            int fileY = height - 1 - y;
            int band = y < height / 3 ? 0 : (y < 2 * height / 3 ? 1 : 2);
            for (int x = 0; x < width; x++)
            {
                int src = (y * width + x) * 4;
                float r = data[src];
                float g = data[src + 1];
                float b = data[src + 2];
                float a = data[src + 3];
                int dst = (fileY * width + x) * 4;
                WriteFloat(file, dst * 4, r);
                WriteFloat(file, (dst + 1) * 4, g);
                WriteFloat(file, (dst + 2) * 4, b);
                WriteFloat(file, (dst + 3) * 4, a);
                HashFloat(ref hash, r);
                HashFloat(ref hash, g);
                HashFloat(ref hash, b);
                minR = Mathf.Min(minR, r);
                minG = Mathf.Min(minG, g);
                minB = Mathf.Min(minB, b);
                maxR = Mathf.Max(maxR, r);
                maxG = Mathf.Max(maxG, g);
                maxB = Mathf.Max(maxB, b);
                float hi = Mathf.Max(r, Mathf.Max(g, b));
                if (hi > 1.001f)
                {
                    above++;
                }
                if (hi < 1f / 255f)
                {
                    near++;
                }
                preview[src / 4] = new Color32(ToByte(r), ToByte(g), ToByte(b), 255);
                bands[band].Add(r, g, b);
            }
        }

        File.WriteAllBytes(Path.Combine(folder, name + ".raw"), file);
        WriteMeta(folder, name, width, height, 16, rt.graphicsFormat.ToString(), "R32G32B32A32F", where);
        WritePng(folder, name + ".png", preview, width, height);
        var text = new StringBuilder();
        text.AppendLine(title + " " + width + "x" + height);
        text.AppendLine("sceneFormat=" + rt.graphicsFormat);
        text.AppendLine("created=" + rt.IsCreated());
        text.AppendLine("min RGB " + minR.ToString("0.####", CultureInfo.InvariantCulture) + " " + minG.ToString("0.####", CultureInfo.InvariantCulture) + " " + minB.ToString("0.####", CultureInfo.InvariantCulture));
        text.AppendLine("max RGB " + maxR.ToString("0.####", CultureInfo.InvariantCulture) + " " + maxG.ToString("0.####", CultureInfo.InvariantCulture) + " " + maxB.ToString("0.####", CultureInfo.InvariantCulture));
        text.AppendLine("канал > 1: " + above + " из " + count);
        text.AppendLine("пик < 1/255: " + near);
        text.AppendLine("hash=" + hash.ToString("X16"));
        text.AppendLine("png — превью, значения выше 1 обрезаны к 255. Числа в raw.");
        text.AppendLine("y в массиве чтения снизу вверх. Файл raw: строка 0 сверху.");
        int y0 = height / 3;
        int y1 = 2 * height / 3;
        text.AppendLine("низ y=[" + 0 + "," + y0 + ") медиана " + bands[0].MedianText());
        text.AppendLine("середина y=[" + y0 + "," + y1 + ") медиана " + bands[1].MedianText());
        text.AppendLine("верх y=[" + y1 + "," + height + ") медиана " + bands[2].MedianText());
        string statsName = name == "input" ? "input_stats.txt" : "output_stats.txt";
        File.WriteAllText(Path.Combine(folder, statsName), text.ToString(), Encoding.UTF8);
        // Debug.Log("[Yebis] " + name + " " + rt.graphicsFormat + " max=" + maxR.ToString("0.###") + "," + maxG.ToString("0.###") + "," + maxB.ToString("0.###") + " above1=" + above + " hash=" + hash.ToString("X16"));
    }

    static void HashFloat(ref ulong hash, float value)
    {
        byte[] bits = BitConverter.GetBytes(value);
        for (int i = 0; i < 4; i++)
        {
            hash ^= bits[i];
            hash *= 1099511628211ul;
        }
    }

    static void SavePresented(string folder, RenderTexture rt, string name, string where, string title)
    {
        if (rt == null)
        {
            // Debug.LogWarning("[Yebis] " + name + " rt null");
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0, false);
        tex.Apply(false, false);
        RenderTexture.active = previous;

        Color32[] pixels = tex.GetPixels32();
        int width = rt.width;
        int height = rt.height;
        int count = width * height;
        var file = new byte[count * 4];
        int peak = 0;
        int near = 0;
        int atMax = 0;
        ulong hash = 14695981039346656037ul;
        int bandCap = (height / 3 + 1) * width;
        var bands = new Band[3];
        for (int i = 0; i < 3; i++)
        {
            bands[i] = Band.Create(bandCap);
        }

        for (int y = 0; y < height; y++)
        {
            int fileY = height - 1 - y;
            int band = y < height / 3 ? 0 : (y < 2 * height / 3 ? 1 : 2);
            for (int x = 0; x < width; x++)
            {
                Color32 c = pixels[y * width + x];
                int dst = (fileY * width + x) * 4;
                file[dst] = c.r;
                file[dst + 1] = c.g;
                file[dst + 2] = c.b;
                file[dst + 3] = c.a;
                hash ^= c.r;
                hash *= 1099511628211ul;
                hash ^= c.g;
                hash *= 1099511628211ul;
                hash ^= c.b;
                hash *= 1099511628211ul;
                peak = Math.Max(peak, Math.Max(c.r, Math.Max(c.g, c.b)));
                float r = c.r / 255f;
                float g = c.g / 255f;
                float b = c.b / 255f;
                float hi = Mathf.Max(r, Mathf.Max(g, b));
                if (hi < 1f / 255f)
                {
                    near++;
                }
                if (c.r >= 254 || c.g >= 254 || c.b >= 254)
                {
                    atMax++;
                }
                bands[band].Add(r, g, b);
            }
        }

        File.WriteAllBytes(Path.Combine(folder, name + ".raw"), file);
        WriteMeta(folder, name, width, height, 4, "R8G8B8A8", "R8G8B8A8", where);
        File.WriteAllBytes(Path.Combine(folder, name + ".png"), tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
        string statsName = name == "input" ? "input_stats.txt" : "output_stats.txt";
        WriteBands(Path.Combine(folder, statsName), title, width, height, count, 0, near, bands, name != "input", atMax);
        File.AppendAllText(
            Path.Combine(folder, statsName),
            "hash=" + hash.ToString("X16") + "\n" +
            "cameraFormat=R8G8B8A8_UNorm. Канал выше 1 в этом входе невозможен: текстура камеры 8-битная.\n" +
            "Если min и max равны 1 0 1, копия draw не легла поверх пурпурной метки.\n",
            Encoding.UTF8);
        // Debug.Log("[Yebis] " + name + " " + rt.graphicsFormat + " " + width + "x" + height + " peak=" + peak + " hash=" + hash.ToString("X16"));
    }

    public static void SaveFloat(string folder, AsyncGPUReadbackRequest request, int width, int height)
    {
        if (request.hasError || width <= 0 || height <= 0)
        {
            // Debug.LogWarning("[Yebis] live input readback failed");
            return;
        }

        NativeArray<float> data = request.GetData<float>();
        int pixels = width * height;
        if (data.Length < pixels * 4)
        {
            // Debug.LogWarning("[Yebis] live input short " + data.Length);
            return;
        }

        var file = new byte[pixels * 16];
        var preview = new Color32[pixels];
        int above = 0;
        int near = 0;
        int bandCap = (height / 3 + 1) * width;
        var bands = new Band[3];
        for (int i = 0; i < 3; i++)
        {
            bands[i] = Band.Create(bandCap);
        }

        for (int y = 0; y < height; y++)
        {
            int fileY = height - 1 - y;
            int band = y < height / 3 ? 0 : (y < 2 * height / 3 ? 1 : 2);
            for (int x = 0; x < width; x++)
            {
                int src = (y * width + x) * 4;
                float r = data[src];
                float g = data[src + 1];
                float b = data[src + 2];
                float a = data[src + 3];
                int dst = (fileY * width + x) * 4;
                WriteFloat(file, dst * 4, r);
                WriteFloat(file, (dst + 1) * 4, g);
                WriteFloat(file, (dst + 2) * 4, b);
                WriteFloat(file, (dst + 3) * 4, a);
                preview[src / 4] = new Color32(ToByte(r), ToByte(g), ToByte(b), 255);
                float peak = Mathf.Max(r, Mathf.Max(g, b));
                if (peak > 1.001f)
                {
                    above++;
                }
                if (peak < 1f / 255f)
                {
                    near++;
                }
                bands[band].Add(r, g, b);
            }
        }

        File.WriteAllBytes(Path.Combine(folder, "input.raw"), file);
        WriteMeta(folder, "input", width, height, 16, "R32G32B32A32F", "R32G32B32A32F", "unity-s0-before-combine");
        WritePng(folder, "input.png", preview, width, height);
        WriteBands(Path.Combine(folder, "input_stats.txt"), "Вход combine, сырые числа", width, height, pixels, above, near, bands, false);
    }

    public static void SaveColor8(string folder, AsyncGPUReadbackRequest request, int width, int height)
    {
        if (request.hasError || width <= 0 || height <= 0)
        {
            // Debug.LogWarning("[Yebis] live output readback failed");
            return;
        }

        NativeArray<byte> data = request.GetData<byte>();
        int pixels = width * height;
        int stride = data.Length / Mathf.Max(pixels, 1);
        if (stride < 4)
        {
            // Debug.LogWarning("[Yebis] live output stride " + stride);
            return;
        }

        var file = new byte[pixels * 4];
        var preview = new Color32[pixels];
        int atMax = 0;
        int bandCap = (height / 3 + 1) * width;
        var bands = new Band[3];
        for (int i = 0; i < 3; i++)
        {
            bands[i] = Band.Create(bandCap);
        }

        for (int y = 0; y < height; y++)
        {
            int fileY = height - 1 - y;
            int band = y < height / 3 ? 0 : (y < 2 * height / 3 ? 1 : 2);
            for (int x = 0; x < width; x++)
            {
                int src = (y * width + x) * stride;
                byte r = data[src];
                byte g = data[src + 1];
                byte b = data[src + 2];
                byte a = data[src + 3];
                int dst = (fileY * width + x) * 4;
                file[dst] = r;
                file[dst + 1] = g;
                file[dst + 2] = b;
                file[dst + 3] = a;
                preview[y * width + x] = new Color32(r, g, b, 255);
                if (r >= 254 || g >= 254 || b >= 254)
                {
                    atMax++;
                }
                bands[band].Add(r / 255f, g / 255f, b / 255f);
            }
        }

        File.WriteAllBytes(Path.Combine(folder, "output.raw"), file);
        WriteMeta(folder, "output", width, height, 4, "R8G8B8A8", "R8G8B8A8", "unity-after-combine");
        WritePng(folder, "output.png", preview, width, height);
        WriteBands(Path.Combine(folder, "output_stats.txt"), "Выход combine, 8-битный", width, height, pixels, 0, 0, bands, true, atMax);
        // Debug.Log("[Yebis] live capture written " + folder);
    }

    static void WriteMeta(string folder, string name, int width, int height, int bpp, string formatName, string order, string where)
    {
        var text = new StringBuilder();
        text.AppendLine("width=" + width);
        text.AppendLine("height=" + height);
        text.AppendLine("formatName=" + formatName);
        text.AppendLine("memoryOrder=" + order);
        text.AppendLine("stride=" + (width * bpp));
        text.AppendLine("size=" + (width * height * bpp));
        text.AppendLine("where=" + where);
        text.AppendLine("present=1");
        text.AppendLine("origin=top-row-first");
        File.WriteAllText(Path.Combine(folder, name + ".meta.txt"), text.ToString(), Encoding.UTF8);
    }

    static void WritePng(string folder, string name, Color32[] pixels, int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        tex.SetPixelData(pixels, 0);
        tex.Apply(false, false);
        File.WriteAllBytes(Path.Combine(folder, name), tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
    }

    static void WriteBands(string path, string title, int width, int height, int pixels, int above, int near, Band[] bands, bool output, int atMax = 0)
    {
        var text = new StringBuilder();
        text.AppendLine(title + " " + width + "x" + height);
        if (!output)
        {
            text.AppendLine("пикселей с каналом > 1: " + above + " из " + pixels);
            text.AppendLine("пикселей почти чёрных, пик < 1/255: " + near);
        }
        else
        {
            text.AppendLine("пикселей с каналом >= 254: " + atMax + " из " + pixels);
        }
        text.AppendLine("Полосы кадра, не одни и те же камни. y растёт снизу вверх.");
        string[] names = { "низ", "середина", "верх" };
        for (int i = 0; i < 3; i++)
        {
            text.AppendLine(names[i] + " медиана RGB " + bands[i].MedianText() + "  пикселей " + bands[i].Count);
        }
        File.WriteAllText(path, text.ToString(), Encoding.UTF8);
    }

    static void WriteFloat(byte[] file, int offset, float value)
    {
        byte[] bits = BitConverter.GetBytes(value);
        file[offset] = bits[0];
        file[offset + 1] = bits[1];
        file[offset + 2] = bits[2];
        file[offset + 3] = bits[3];
    }

    static byte ToByte(float value)
    {
        int n = Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        return (byte)n;
    }

    struct Band
    {
        public float[] R;
        public float[] G;
        public float[] B;
        public int Count;

        public static Band Create(int capacity)
        {
            return new Band
            {
                R = new float[capacity],
                G = new float[capacity],
                B = new float[capacity],
                Count = 0
            };
        }

        public void Add(float r, float g, float b)
        {
            if (Count >= R.Length)
            {
                return;
            }
            R[Count] = r;
            G[Count] = g;
            B[Count] = b;
            Count++;
        }

        public string MedianText()
        {
            return Median(R).ToString("0.####", CultureInfo.InvariantCulture) + " " +
                   Median(G).ToString("0.####", CultureInfo.InvariantCulture) + " " +
                   Median(B).ToString("0.####", CultureInfo.InvariantCulture);
        }

        float Median(float[] values)
        {
            if (Count <= 0)
            {
                return 0f;
            }
            var copy = new float[Count];
            Array.Copy(values, copy, Count);
            Array.Sort(copy);
            return copy[Count / 2];
        }
    }
}

/// <summary>
/// G writes actor light constants. F8 opens the skill bar.
/// The frame capture and the F7 grade stage stay off.
/// The Game view must have focus.
/// </summary>
sealed class L2YebisLiveCaptureKey : MonoBehaviour
{
    public static L2YebisLiveCaptureKey Host { get; private set; }

    void Awake()
    {
        Host = this;
    }

    void OnDestroy()
    {
        if (Host == this)
        {
            Host = null;
        }
    }

    void LateUpdate()
    {
        if (!Input.GetKeyDown(KeyCode.G))
        {
            return;
        }

        L2ActorLightCapture.Write();
    }
}
