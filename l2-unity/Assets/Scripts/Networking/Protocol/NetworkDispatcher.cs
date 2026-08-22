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

        EventProcessor events = EventProcessor.Instance;
        int mainPending = events != null ? events.PendingCount : 0;
        PacketLatencyLog.OnQueued(model, mainPending);
        _applyQueue.QueueApply(() =>
        {
            PacketLatencyLog.OnApply(model);
            _handlers.Handle(model);
        });
    }
}
