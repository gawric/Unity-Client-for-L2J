using System.Collections.Generic;

/// <summary>
/// Interlude UNetworkHandler::Tick drains every packet, but OnMoveToLocation only
/// overwrites AController.Destination (AddMoveTo). Later APawn::physWalking steps.
/// Unity was applying each CharMoveToLocation/StopMove as a full action restart.
/// Keep the last move/stop/pawn packet per objectId and apply it once per batch.
/// </summary>
public static class MovementPacketLatest
{
    enum Kind
    {
        Move,
        Stop,
        Pawn
    }

    struct Slot
    {
        public Kind Kind;
        public CharMoveToLocationDto Move;
        public StopMoveDto Stop;
        public MoveToPawnDto Pawn;
    }

    static readonly Dictionary<int, Slot> Pending = new Dictionary<int, Slot>(1024);
    static readonly List<int> Ids = new List<int>(1024);

    public static int Count { get { return Ids.Count; } }

    public static void PutMove(CharMoveToLocationDto dto)
    {
        if (dto == null)
            return;
        Set(dto.ObjId, new Slot { Kind = Kind.Move, Move = dto });
    }

    public static void PutStop(StopMoveDto dto)
    {
        if (dto == null)
            return;
        Set(dto.ObjId, new Slot { Kind = Kind.Stop, Stop = dto });
    }

    public static void PutPawn(MoveToPawnDto dto)
    {
        if (dto == null)
            return;
        Set(dto.ObjId, new Slot { Kind = Kind.Pawn, Pawn = dto });
    }

    public static void Flush(WorldPacketApply world)
    {
        if (world == null || Ids.Count == 0)
            return;

        for (int i = 0; i < Ids.Count; i++)
        {
            Slot slot;
            if (!Pending.TryGetValue(Ids[i], out slot))
                continue;

            switch (slot.Kind)
            {
                case Kind.Move:
                    if (slot.Move != null)
                        world.MoveTo(slot.Move.ObjId, slot.Move.NewPosition, slot.Move.OldPosition, slot.Move);
                    break;
                case Kind.Stop:
                    world.StopMove(slot.Stop);
                    break;
                case Kind.Pawn:
                    world.MoveToPawn(slot.Pawn);
                    break;
            }
        }

        Pending.Clear();
        Ids.Clear();
    }

    static void Set(int objectId, Slot slot)
    {
        if (!Pending.ContainsKey(objectId))
            Ids.Add(objectId);
        Pending[objectId] = slot;
    }
}
