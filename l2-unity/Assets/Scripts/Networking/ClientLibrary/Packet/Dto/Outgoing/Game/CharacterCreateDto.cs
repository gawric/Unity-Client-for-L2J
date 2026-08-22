public sealed class CharacterCreateDto : IOutgoingDto
{
    public string Name;
    public int Race;
    public int Female;
    public int ClassId;
    public int BaseInt;
    public int BaseStr;
    public int BaseCon;
    public int BaseMen;
    public int BaseDex;
    public int BaseWit;
    public int HairStyle;
    public int HairColor;
    public int Face;

    public void WriteTo(PacketWriter writer)
    {
        writer.WriteSOther(Name);
        writer.WriteChar((char)0);
        writer.WriteI(Race);
        writer.WriteI(Female);
        writer.WriteI(ClassId);
        writer.WriteI(BaseInt);
        writer.WriteI(BaseStr);
        writer.WriteI(BaseCon);
        writer.WriteI(BaseMen);
        writer.WriteI(BaseDex);
        writer.WriteI(BaseWit);
        writer.WriteI(HairStyle);
        writer.WriteI(HairColor);
        writer.WriteI(Face);
    }
}
