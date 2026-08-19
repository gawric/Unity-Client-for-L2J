public sealed class PlayOkDto : IWireDto
{
    public int PlayOk1;
    public int PlayOk2;

    public void ReadFrom(PacketReader reader)
    {
        PlayOk1 = reader.ReadI();
        PlayOk2 = reader.ReadI();
    }
}
