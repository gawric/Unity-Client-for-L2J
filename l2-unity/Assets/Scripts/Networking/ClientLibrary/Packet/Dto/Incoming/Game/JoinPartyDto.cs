/// <summary>
/// Sent back to the inviter once the invited player answers RequestAnswerJoinParty - tells the
/// requestor whether their invite was accepted (1) or declined (0).
/// </summary>
public sealed class JoinPartyDto : IWireDto
{
    public int Response { get; private set; }
    public bool Accepted => Response == 1;

    public void ReadFrom(PacketReader reader)
    {
        Response = reader.ReadI();
    }
}
