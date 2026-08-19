/// Default incoming path: new TDto + IWireDto.ReadFrom, then Apply.
/// Override CreateDto when the DTO needs constructor arguments (UserInfoDto).
public abstract class IncomingWirePacket<TDto> : IncomingPacket<TDto> where TDto : class, IWireDto, new()
{
    protected virtual TDto CreateDto()
    {
        return new TDto();
    }

    public override TDto Read(PacketReader reader)
    {
        TDto dto = CreateDto();
        dto.ReadFrom(reader);
        return dto;
    }
}
