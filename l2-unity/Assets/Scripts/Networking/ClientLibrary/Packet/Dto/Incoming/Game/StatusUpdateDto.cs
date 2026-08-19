using System.Collections.Generic;

public sealed class StatusUpdateDto : INetworkModel
{
    public int ObjectId;
    public List<StatusUpdate.Attribute> Attributes;
}
