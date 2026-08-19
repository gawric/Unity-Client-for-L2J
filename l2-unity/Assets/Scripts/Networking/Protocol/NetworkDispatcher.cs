public sealed class NetworkDispatcher : INetworkDispatcher
{
    private readonly INetworkHandlers _handlers;
    private readonly PacketApplyQueue _applyQueue;

    public NetworkDispatcher(INetworkHandlers handlers, PacketApplyQueue applyQueue)
    {
        _handlers = handlers;
        _applyQueue = applyQueue;
    }

    public void Dispatch(INetworkModel model)
    {
        if (model == null)
            return;

        _applyQueue.QueueApply(() => _handlers.Handle(model));
    }
}
