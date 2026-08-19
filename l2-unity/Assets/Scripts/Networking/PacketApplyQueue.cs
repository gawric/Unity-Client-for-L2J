using System;

public sealed class PacketApplyQueue
{
    [ThreadStatic]
    private static int _applyDepth;

    private readonly EventProcessor _events;

    public PacketApplyQueue(EventProcessor events)
    {
        _events = events;
    }

    public void Queue(Action action)
    {
        if (_applyDepth > 0)
        {
            action();
            return;
        }

        EventProcessor processor = _events != null ? _events : EventProcessor.Instance;
        if (processor != null)
            processor.QueueEvent(action);
        else
            action();
    }

    public void QueueApply(Action action)
    {
        Queue(() =>
        {
            _applyDepth++;
            try
            {
                action();
            }
            finally
            {
                _applyDepth--;
            }
        });
    }
}
