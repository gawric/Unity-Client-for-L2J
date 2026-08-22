using UnityEngine;

/// <summary>
/// ActionType.Pickup ("Pick Up" / /pickup, action id=5 in ActionName_Classic-eu.txt) - "Pick up
/// items that are scattered nearby": grabs the closest dropped item in range instead of requiring
/// a click on it. Walks to it first if it's not already close enough, reusing the same
/// walk-then-request flow as clicking an item (see DroppedItemEntity.RequestPickup).
/// </summary>
public class PickupAction : L2Action
{
    // "Nearby" search radius for the action - not taken from a real server, just enough to reach
    // the closest item without having to click it directly.
    private const float SEARCH_RADIUS = 3f;

    public override void UseAction()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.DEAD)
        {
            return;
        }

        Vector3 playerPosition = PlayerEntity.Instance.transform.position;
        GameObject nearest = World.Instance.GetNearestDroppedItem(playerPosition, SEARCH_RADIUS);
        if (nearest == null)
        {
            return;
        }

        DroppedItemEntity item = nearest.GetComponent<DroppedItemEntity>();
        if (item == null)
        {
            return;
        }

        SendMoveToItem(playerPosition, nearest.transform.position);
        item.RequestPickup();
    }

    private void SendMoveToItem(Vector3 playerPosition, Vector3 itemPosition)
    {
        IncomingPacketActions.Game.Send(new MoveToCommand(playerPosition, itemPosition));
    }
}
