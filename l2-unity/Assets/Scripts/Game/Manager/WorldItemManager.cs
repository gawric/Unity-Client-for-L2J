using UnityEngine;

/// <summary>
/// Shows the L2TransparentTooltip (the same popup slots/skills use) while the mouse hovers a
/// dropped item. Hover state comes from ClickManager.HoverObjectData - the same raycast it already
/// runs every frame for entities and click-to-move - which requires the item to carry a collider
/// (added by DroppedItemFactory).
/// </summary>
public class WorldItemManager : MonoBehaviour
{
    // Floor for the tooltip lift - keeps flat/tiny drops from anchoring the tooltip right at ground level.
    private const float MIN_TOOLTIP_HEIGHT = 0.15f;
    // Extra gap above the model so the tooltip doesn't clip into it.
    private const float TOOLTIP_GAP = 0.05f;

    [SerializeField] private string _tooltipText = "";
    [SerializeField] private long _itemsCount = 0;
    [SerializeField] private float _tooltipHeight = 0f;

    private bool _isHovered = false;

    void Update()
    {
        bool isHoveredNow = ClickManager.Instance != null
            && ClickManager.Instance.HoverObjectData != null
            && ClickManager.Instance.HoverObjectData.ObjectTransform == transform;

        if (isHoveredNow == _isHovered)
        {
            return;
        }

        _isHovered = isHoveredNow;
        UpdateTooltipVisibility();
    }

    void OnDestroy()
    {
        if (_isHovered)
        {
            _isHovered = false;
            L2TransparentTooltip.Instance?.HideWindow();
        }
    }

    private void UpdateTooltipVisibility()
    {
        if (L2TransparentTooltip.Instance == null)
        {
            return;
        }

        if (_isHovered)
        {
            Vector3 anchor = transform.position + Vector3.up * (Mathf.Max(_tooltipHeight, MIN_TOOLTIP_HEIGHT) + TOOLTIP_GAP);
            L2TransparentTooltip.Instance.UpdateTooltipWorld(_tooltipText, anchor, Camera.main);
        }
        else
        {
            L2TransparentTooltip.Instance.HideWindow();
        }
    }

    public void SetTooltipText(string text)
    {
        _tooltipText = text;
    }

    public void SetItemsCount(long count)
    {
        _itemsCount = count;
    }

    public void SetTooltipHeight(float height)
    {
        _tooltipHeight = height;
    }
}
