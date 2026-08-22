public sealed class LoginFailDto : IWireDto
{
    public byte ReasonId;
    public string Message;

    public void ReadFrom(PacketReader reader)
    {
        ReasonId = reader.ReadB();
        Message = MapReason(ReasonId);
    }

    private static string MapReason(byte reasonId)
    {
        switch ((LoginFailReason)reasonId)
        {
            case LoginFailReason.REASON_USER_OR_PASS_WRONG:
                return "Reason user or pass wrong";
            case LoginFailReason.REASON_ACCESS_FAILED_TRY_AGAIN_LATER:
                return "Reason  access failed try again later";
            case LoginFailReason.REASON_ACCOUNT_IN_USE:
                return "Reason account in use";
            case LoginFailReason.REASON_NOT_AUTHED:
                return "Reason not authed";
            default:
                return "The reason is not known";
        }
    }
}
