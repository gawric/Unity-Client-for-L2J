public sealed class MagicSkillCanceledDto : IWireDto
{
    public int ObjectId { get; private set; }

    public void ReadFrom(PacketReader reader)
    {
        ObjectId = reader.ReadI();
    }
}
