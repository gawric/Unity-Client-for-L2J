using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Draws L2 HeadDisplay bubbles (Normal / Target / Attack) with separate batches
/// so URP deferred draws do not overwrite each other via a shared StructuredBuffer.
/// </summary>
public sealed class NameplateBubbleDrawer
{
    private readonly L2NameplateScreenBatch _batchNormal = new L2NameplateScreenBatch();
    private readonly L2NameplateScreenBatch _batchTarget = new L2NameplateScreenBatch();
    private readonly L2NameplateScreenBatch _batchAttack = new L2NameplateScreenBatch();

    private Texture2D _texNormal;
    private Texture2D _texTarget;
    private Texture2D _texAttack;
    private bool _enabled = true;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public void EnsureResources()
    {
        if (!_enabled)
        {
            return;
        }

        if (_texNormal == null)
        {
            _texNormal = L2NameplateBubbles.LoadTexture(L2NameplateBubbles.NormalResourcePath);
        }

        if (_texTarget == null)
        {
            _texTarget = L2NameplateBubbles.LoadTexture(L2NameplateBubbles.TargetResourcePath);
        }

        if (_texAttack == null)
        {
            _texAttack = L2NameplateBubbles.LoadTexture(L2NameplateBubbles.AttackResourcePath);
        }

        if (_texNormal != null)
        {
            _batchNormal.EnsureMaterial(_texNormal);
        }

        if (_texTarget != null)
        {
            _batchTarget.EnsureMaterial(_texTarget);
        }

        if (_texAttack != null)
        {
            _batchAttack.EnsureMaterial(_texAttack);
        }
    }

    public void Draw(Camera cam, List<NameplatePaintItem> paintList)
    {
        if (!_enabled || cam == null || paintList == null)
        {
            return;
        }

        bool anyReady = _batchNormal.IsReady || _batchTarget.IsReady || _batchAttack.IsReady;
        if (!anyReady)
        {
            return;
        }

        bool anyNormal = false;
        bool anyTarget = false;
        bool anyAttack = false;

        for (int i = 0; i < paintList.Count; i++)
        {
            NameplatePaintItem item = paintList[i];
            if (!item.ScreenValid)
            {
                continue;
            }

            switch (item.BubbleType)
            {
                case L2TargetRenderType.Normal:
                    anyNormal = true;
                    break;
                case L2TargetRenderType.Target:
                    anyTarget = true;
                    break;
                case L2TargetRenderType.Attack:
                    anyAttack = true;
                    break;
            }
        }

        float screenH = cam.pixelHeight;

        if (anyNormal)
        {
            DrawForType(cam, screenH, paintList, L2TargetRenderType.Normal, _texNormal, _batchNormal);
        }

        if (anyTarget)
        {
            DrawForType(cam, screenH, paintList, L2TargetRenderType.Target, _texTarget, _batchTarget);
        }

        if (anyAttack)
        {
            DrawForType(cam, screenH, paintList, L2TargetRenderType.Attack, _texAttack, _batchAttack);
        }
    }

    public void Dispose()
    {
        _batchNormal.Dispose();
        _batchTarget.Dispose();
        _batchAttack.Dispose();
    }

    private static void DrawForType(
        Camera cam,
        float screenH,
        List<NameplatePaintItem> paintList,
        L2TargetRenderType type,
        Texture2D tex,
        L2NameplateScreenBatch batch)
    {
        if (tex == null || batch == null)
        {
            return;
        }

        batch.EnsureMaterial(tex);
        batch.BeginFrame();

        for (int i = 0; i < paintList.Count; i++)
        {
            NameplatePaintItem item = paintList[i];
            if (!item.ScreenValid || item.BubbleType != type)
            {
                continue;
            }

            L2NameplateBubbles.AppendPair(
                batch,
                item.XName,
                item.NameW,
                item.YNameTop,
                item.Depth,
                screenH,
                0f,
                Color.white);
        }

        batch.UploadAndDraw(cam);
    }
}
