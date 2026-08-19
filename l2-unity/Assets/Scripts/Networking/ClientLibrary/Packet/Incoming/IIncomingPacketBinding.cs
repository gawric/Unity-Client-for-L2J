using System;

public interface IIncomingPacketBinding
{
    Type ModelType { get; }

    INetworkModel Parse(byte[] data);

    void ApplyModel(INetworkModel model);
}
