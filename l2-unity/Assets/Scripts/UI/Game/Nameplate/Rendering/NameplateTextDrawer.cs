using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projects nameplates and batches name + title glyphs.
/// Fills screen layout fields on each <see cref="NameplatePaintItem"/> for bubble drawing.
/// </summary>
public sealed class NameplateTextDrawer
{
    private readonly L2NameplateScreenBatch _batch = new L2NameplateScreenBatch();
    private L2BitmapFont _font;
    private string _atlasResourcePath;
    private string _csvResourcePath;

    public float PixelScale { get; set; } = 1f;
    public float TitleGapPixels { get; set; } = 2f;
    public bool SnapAnchorToPixels { get; set; } = true;
    public bool SnapDiscardSubpixelFrac { get; set; } = true;

    public bool IsReady => _font != null && _batch.IsReady;

    public void ConfigurePaths(string atlasResourcePath, string csvResourcePath)
    {
        _atlasResourcePath = atlasResourcePath;
        _csvResourcePath = csvResourcePath;
    }

    public void EnsureResources()
    {
        if (_font == null)
        {
            _font = L2BitmapFont.LoadFromResources(_atlasResourcePath, _csvResourcePath);
        }

        if (_font != null)
        {
            _batch.EnsureMaterial(_font.Atlas);
        }
    }

    public void Draw(Camera cam, List<NameplatePaintItem> paintList, NameplatePixelSnap snap)
    {
        if (cam == null || paintList == null || _font == null || !_batch.IsReady)
        {
            return;
        }

        _batch.BeginFrame();

        float scale = PixelScale > 0f ? PixelScale : 1f;
        float screenH = cam.pixelHeight;
        float lineH = _font.MeasureHeight(scale);
        float titleGap = Mathf.Max(0f, TitleGapPixels);
        bool discardFrac = SnapAnchorToPixels && SnapDiscardSubpixelFrac;

        float orbitDist = 0f;
        if (CameraController.Instance != null)
        {
            orbitDist = CameraController.Instance.CurrentDistance;
        }

        for (int i = 0; i < paintList.Count; i++)
        {
            NameplatePaintItem item = paintList[i];
            item.ScreenValid = false;

            Vector3 screen = cam.WorldToScreenPoint(item.World);
            if (screen.z <= 0f)
            {
                paintList[i] = item;
                continue;
            }

            float depth = screen.z;
            float ax = screen.x;
            float ay = screen.y;
            if (SnapAnchorToPixels && snap != null)
            {
                float snapDist = (item.IsLocalPlayer && orbitDist > 0.05f) ? orbitDist : depth;
                ax = snap.Snap(item.Id, screen.x, true, snapDist);
                ay = snap.Snap(item.Id, screen.y, false, snapDist);
            }

            float yNameTop = screenH - ay - lineH;
            if (!string.IsNullOrEmpty(item.Title))
            {
                float titleW = _font.MeasureWidth(item.Title, scale);
                float xTitle = ax - titleW * 0.5f;
                float yTitleTop = yNameTop - lineH - titleGap;
                _batch.AppendLine(
                    _font, item.Title, xTitle, yTitleTop, scale, item.TitleColor,
                    depth, screenH, discardFrac);
            }

            float nameW = _font.MeasureWidth(item.Name, scale);
            float xName = ax - nameW * 0.5f;
            _batch.AppendLine(
                _font, item.Name, xName, yNameTop, scale, item.NameColor,
                depth, screenH, discardFrac);

            item.ScreenValid = true;
            item.Depth = depth;
            item.NameW = nameW;
            item.XName = xName;
            item.YNameTop = yNameTop;
            paintList[i] = item;
        }

        _batch.UploadAndDraw(cam);
    }

    public void Dispose()
    {
        _batch.Dispose();
    }
}
