public sealed class MoveBackwardToLocationDto : IOutgoingDto
{
    public int TargetX;
    public int TargetY;
    public int TargetZ;
    public int OriginX;
    public int OriginY;
    public int OriginZ;
    public int CursorMode = 1;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteI(TargetX);
        writer.WriteI(TargetY);
        writer.WriteI(TargetZ);
        writer.WriteI(OriginX);
        writer.WriteI(OriginY);
        writer.WriteI(OriginZ);
        writer.WriteI(CursorMode);
    }
}
