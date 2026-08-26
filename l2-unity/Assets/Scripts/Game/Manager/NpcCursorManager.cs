using UnityEngine;
using VContainer;

public class NpcCursorManager : MonoBehaviour
{
    [Inject] ItemDropPicker _dropPicker;
    private Texture2D _defaultCursor;
    private Texture2D _hoverCursorTalk;
    private Texture2D _hoverCursorAtk;
    private Texture2D _hoverCursorPickup;
    private LayerMask _entityMask;
    private int _currentCursor;
    private ItemEntity _stickyDropItem;
    const int DefaultCursorId = 0;
    const int AtkCursorId = 1;
    const int TalkCursorId = 2;
    const int PickupCursorId = 3;
    const float PickDistance = 1000f;

    void Start()
    {
        _defaultCursor = IconManager.Instance.LoadCursorByName("Default");
        _hoverCursorAtk = IconManager.Instance.LoadCursorByName("Attack");
        _hoverCursorTalk = IconManager.Instance.LoadCursorByName("Talk");
        _hoverCursorPickup = IconManager.Instance.LoadCursorByName("Pickup");
        _entityMask = LayerMask.GetMask("EntityClick");
        _currentCursor = -1;
    }

    void Update()
    {
        if (Camera.main == null)
        {
            ApplyCursor(DefaultCursorId, _defaultCursor);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        ItemEntity dropEntity = null;
        bool dropHit = _dropPicker != null &&
            _dropPicker.TryPick(ray, PickDistance, _stickyDropItem, out dropEntity, out _);
        _stickyDropItem = dropHit ? dropEntity : null;

        if (dropHit && dropEntity != null)
        {
            ApplyCursor(PickupCursorId, _hoverCursorPickup);
            return;
        }

        if (L2GameUI.Instance != null && L2GameUI.Instance.MouseOverUI)
        {
            ApplyCursor(DefaultCursorId, _defaultCursor);
            return;
        }

        if (!Physics.Raycast(ray, out RaycastHit hit, PickDistance, _entityMask))
        {
            ApplyCursor(DefaultCursorId, _defaultCursor);
            return;
        }

        Entity entity = hit.collider.GetComponentInParent<Entity>();
        if (entity != null &&
            entity.Identity != null &&
            PlayerEntity.Instance != null &&
            entity.Identity.Id == PlayerEntity.Instance.TargetId)
        {
            ApplyTargetedEntityCursor(entity);
            return;
        }

        ApplyCursor(DefaultCursorId, _defaultCursor);
    }

    void ApplyTargetedEntityCursor(Entity entity)
    {
        switch (entity)
        {
            case MonsterEntity _:
                ApplyCursor(AtkCursorId, _hoverCursorAtk);
                break;
            case NpcEntity _:
                ApplyCursor(TalkCursorId, _hoverCursorTalk);
                break;
            default:
                ApplyCursor(DefaultCursorId, _defaultCursor);
                break;
        }
    }

    void ApplyCursor(int id, Texture2D texture)
    {
        if (_currentCursor == id)
        {
            Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
            return;
        }

        _currentCursor = id;
        Cursor.SetCursor(texture, Vector2.zero, CursorMode.Auto);
    }
}
