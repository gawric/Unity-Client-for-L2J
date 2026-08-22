using UnityEngine;

/// <summary>
/// Behaviour attached to a dropped item's GameObject by World.DropItemOnTheGround (visuals and
/// collider come from DroppedItemFactory). Handles:
///  - the hover tooltip (name, and count only when the stack has more than one item), via
///    ClickManager.HoverObjectData - the same raycast it already runs every frame for entities.
///  - pickup on click: ClickManager already walks the player to the click point for any non-entity
///    hit, this only arms a pending pickup that fires once the player is close enough.
/// </summary>
public class DroppedItemEntity : MonoBehaviour
{
    // Floor for the tooltip lift - keeps flat/tiny drops from anchoring the tooltip right at ground level.
    private const float MIN_TOOLTIP_HEIGHT = 0.15f;
    // Extra gap above the model so the tooltip doesn't clip into it.
    private const float TOOLTIP_GAP = 0.05f;
    // How close the player has to walk before the pickup request is sent - a rough guess, not
    // taken from a real server's pickup radius.
    private const float PICKUP_RANGE = 0.6f;

    [SerializeField] private int _itemObjectId;
    [SerializeField] private string _tooltipText = "";
    [SerializeField] private long _itemsCount = 0;
    [SerializeField] private float _tooltipHeight = 0f;

    private bool _isHovered = false;
    private bool _pendingPickup = false;

    // Only one drop can be "being walked to" at a time - a stale pending pickup from an earlier,
    // never-reached click must not fire later just because the player happened to wander past it.
    private static DroppedItemEntity _pendingItem;

    public void Initialize(int itemObjectId, string tooltipText, long itemsCount, float tooltipHeight)
    {
        _itemObjectId = itemObjectId;
        _tooltipText = tooltipText;
        _itemsCount = itemsCount;
        _tooltipHeight = tooltipHeight;
    }

    void Update()
    {
        UpdateHover();
        UpdatePendingPickup();
    }

    void OnDestroy()
    {
        if (_isHovered)
        {
            _isHovered = false;
            L2TransparentTooltip.Instance?.HideWindow();
        }

        if (_pendingItem == this)
        {
            _pendingItem = null;
        }
    }

    private void UpdateHover()
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

    /// <summary>
    /// Called by ClickManager when this item is clicked - it already walks the player to the click
    /// point, this just remembers to send the pickup request once they arrive.
    /// </summary>
    public void RequestPickup()
    {
        if (_pendingItem != null && _pendingItem != this)
        {
            _pendingItem._pendingPickup = false;
        }

        _pendingItem = this;
        _pendingPickup = true;
    }

    private void UpdatePendingPickup()
    {
        if (!_pendingPickup || PlayerEntity.Instance == null)
        {
            return;
        }

        float distance = Vector3.Distance(PlayerEntity.Instance.transform.position, transform.position);
        if (distance > PICKUP_RANGE)
        {
            return;
        }

        _pendingPickup = false;
        if (_pendingItem == this)
        {
            _pendingItem = null;
        }

        SendPickupRequest();
    }

    /// <summary>
    /// L2J has no dedicated "get item" packet - picking up is just an Action (0x04) targeting the
    /// item's objectId, the same packet ClickManager.OnClickOnEntity sends for NPCs/players. The
    /// server tells apart what to do (talk, attack, pick up, ...) from the object type behind that id.
    /// </summary>
    private void SendPickupRequest()
    {
        Vector3 l2jPos = VectorUtils.ConvertPosUnityToL2j(transform.position);
        IncomingPacketActions.Game.Send(new ClickActionCommand(
            _itemObjectId, (int)l2jPos.x, (int)l2jPos.y, (int)l2jPos.z, 0));
    }
}
