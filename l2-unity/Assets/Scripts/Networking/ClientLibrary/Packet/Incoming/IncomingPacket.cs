using System;

/// Incoming packet: opcode attribute registers the Interlude binding.
/// Parse runs on the network thread; Apply is queued on the render thread.
public abstract class IncomingPacket<TDto> : IIncomingPacketBinding where TDto : class, INetworkModel
{
    public Type ModelType
    {
        get { return typeof(TDto); }
    }

    public abstract TDto Read(PacketReader reader);

    protected virtual void OnParsed(TDto dto)
    {
    }

    public abstract void Apply(TDto dto);

    public INetworkModel Parse(byte[] data)
    {
        TDto dto = Read(new PacketReader(data, true));
        OnParsed(dto);
        return dto;
    }

    public void ApplyModel(INetworkModel model)
    {
        Apply((TDto)model);
    }
}
