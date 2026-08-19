public class EnchantResultDto : IWireDto
{
    private EnumResult _result;

    public EnumResult Result { get { return _result; } }
    
    public void ReadFrom(PacketReader reader)
    {

        int result = reader.ReadI();
        _result =  ParceResult(result);
    }

    private EnumResult ParceResult(int resultType)
    {
        switch (resultType)
        {
            case (int)EnumResult.Success:
                return EnumResult.Success;
            case (int)EnumResult.Unk_Result_1:
                return EnumResult.Unk_Result_1;
            case (int)EnumResult.Cancelled:
                return EnumResult.Cancelled;
            case (int)EnumResult.UnSuccess:
                return EnumResult.UnSuccess;
            case (int)EnumResult.Unk_Result_4:
                return EnumResult.Unk_Result_4;
        }
        return EnumResult.None;
    }

    public enum EnumResult
    {
        Success,
        Unk_Result_1,
        Cancelled,
        UnSuccess,
        Unk_Result_4,
        None
    }


}
