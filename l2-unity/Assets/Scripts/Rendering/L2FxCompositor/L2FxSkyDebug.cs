using System.Globalization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// Temporary diagnostics for black-sky. CPU state + GPU pixel from sky scratch / dest.
/// Remove once the sky blit is confirmed.
/// </summary>
public static class L2FxSkyDebug
{
    const float PeriodSec = 1.5f;
    static float _nextCpu = -1f;
    static float _nextLut = -1f;

    public static bool CpuDue => Time.unscaledTime >= _nextCpu;

    public static string ColorHex(Color c)
    {
        Color32 b = c;
        return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2} a={3:0.00}", b.r, b.g, b.b, c.a);
    }

    public static string RtInfo(RTHandle handle, string name)
    {
        if (handle == null)
            return name + "=null";
        RenderTexture rt = handle.rt;
        if (rt == null)
            return name + "=rtNull";
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1}x{2} fmt={3} sRGB={4}",
            name,
            rt.width,
            rt.height,
            rt.graphicsFormat,
            rt.sRGB);
    }

    public static void LogLut(float hours, L2DayNightLut.Sample sample, Material skyMat)
    {
        return;
        if (Time.unscaledTime < _nextLut)
            return;
        _nextLut = Time.unscaledTime + PeriodSec;

        Color mat = default;
        bool has = skyMat != null && skyMat.HasProperty("_GradientColor1");
        if (has)
            mat = skyMat.GetColor("_GradientColor1");

        Debug.Log(string.Format(
            CultureInfo.InvariantCulture,
            "[L2SkyColor] h={0:00.000} phase={1}({2}) lutSky={3} ({4:0.000},{5:0.000},{6:0.000}) " +
            "matG1={7} ({8:0.000},{9:0.000},{10:0.000}) sun={11} renderSkybox={12}",
            hours,
            sample.phase,
            L2DayNightLut.PhaseName(sample.phase),
            ColorHex(sample.sky),
            sample.sky.r, sample.sky.g, sample.sky.b,
            has ? ColorHex(mat) : "none",
            mat.r, mat.g, mat.b,
            ColorHex(sample.sun),
            RenderSettings.skybox != null ? RenderSettings.skybox.name : "NULL"));
    }

    public static void LogCpuAndEnqueueGpu(
        CommandBuffer cmd,
        string path,
        bool wantCelestial,
        bool includeSky,
        bool drewSkybox,
        string skipReason,
        RTHandle skyScratch,
        RTHandle dest,
        Camera camera,
        float fxGain,
        float boostStart,
        float boostFull)
    {
        return;
        if (!CpuDue)
            return;
        _nextCpu = Time.unscaledTime + PeriodSec;

        Material skybox = RenderSettings.skybox;
        Color g1 = default;
        bool hasG1 = skybox != null && skybox.HasProperty("_GradientColor1");
        if (hasG1)
            g1 = skybox.GetColor("_GradientColor1");

        CameraClearFlags flags = camera != null ? camera.clearFlags : (CameraClearFlags)(-1);
        Color bg = camera != null ? camera.backgroundColor : Color.magenta;

        Debug.Log(string.Format(
            CultureInfo.InvariantCulture,
            "[L2SkyBlit] path={0} wantCel={1} includeSky={2} drewSkybox={3} skip={4} " +
            "skybox={5} g1={6} ({7:0.000},{8:0.000},{9:0.000}) peak={10:0.000} " +
            "camClear={11} bg={12} gain={13:0.00} boost={14:0.00}-{15:0.00} | {16} | {17}",
            path,
            wantCelestial,
            includeSky,
            drewSkybox,
            string.IsNullOrEmpty(skipReason) ? "-" : skipReason,
            skybox != null ? skybox.name : "NULL",
            hasG1 ? ColorHex(g1) : "no_g1",
            g1.r, g1.g, g1.b,
            Mathf.Max(g1.r, Mathf.Max(g1.g, g1.b)),
            flags,
            ColorHex(bg),
            fxGain,
            boostStart,
            boostFull,
            RtInfo(skyScratch, "scratch"),
            RtInfo(dest, "dest")));

        EnqueueReadback(cmd, "scratch", skyScratch);
        EnqueueReadback(cmd, "dest", dest);
    }

    static void EnqueueReadback(CommandBuffer cmd, string tag, RTHandle handle)
    {
        if (cmd == null)
            return;
        RenderTexture rt = handle != null ? handle.rt : null;
        if (rt == null)
        {
            Debug.Log("[L2SkyBlit] gpu " + tag + " skipped (null RT)");
            return;
        }

        int x = Mathf.Max(0, rt.width / 2);
        int y = Mathf.Max(0, (rt.height * 4) / 5);
        GraphicsFormat fmt = rt.graphicsFormat;
        cmd.RequestAsyncReadback(rt, 0, x, y, 1, 1, 0, 1, req =>
        {
            if (req.hasError)
            {
                Debug.LogWarning("[L2SkyBlit] gpu " + tag + " readback error fmt=" + fmt);
                return;
            }

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "[L2SkyBlit] gpu {0} xy={1},{2} fmt={3} pixel={4}",
                tag, x, y, fmt, SamplePixel(req, fmt)));
        });
    }

    static string SamplePixel(AsyncGPUReadbackRequest req, GraphicsFormat fmt)
    {
        try
        {
            var data = req.GetData<Color32>();
            if (data.Length > 0)
            {
                Color32 c = data[0];
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Color32 {0},{1},{2},{3} #{4:X2}{5:X2}{6:X2}",
                    c.r, c.g, c.b, c.a, c.r, c.g, c.b);
            }
        }
        catch
        {
            // HDR / float targets.
        }

        try
        {
            var data = req.GetData<Color>();
            if (data.Length > 0)
            {
                Color c = data[0];
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Color {0:0.000},{1:0.000},{2:0.000},{3:0.000} {4}",
                    c.r, c.g, c.b, c.a, ColorHex(c));
            }
        }
        catch
        {
            // ignore
        }

        return "unreadable fmt=" + fmt;
    }
}
