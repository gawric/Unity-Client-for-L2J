public class LoginFail : ServerPacket
{
    private byte reason;
    private string message;
    public int RessionId { get { return reason; } }
    public string Message { get { return message; } }
    public LoginFail(byte[] d) : base(d)
    {
        Parse();
    }

    public override void Parse()
    {
        reason = ReadB();
        message = ParceId(reason);
    }

    private string ParceId(byte rId)
    {
        switch ((LoginFailReason)rId)
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



