using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class IncomingGameQueue : IDisposable
{
    private readonly object _sync = new object();
    private BlockingCollection<IncomingRawPacket> _queue;
    private GameServerPacketHandler _handler;
    private CancellationTokenSource _cancelTokenSource;
    private Task _worker;
    private int _running;

    public IncomingGameQueue()
    {
        _queue = new BlockingCollection<IncomingRawPacket>();
        _cancelTokenSource = new CancellationTokenSource();
    }

    public void SetPacketHandler(ServerPacketHandler handler)
    {
        lock (_sync)
            _handler = (GameServerPacketHandler)handler;
        EnsureWorker();
    }

    public void AddItem(byte[] data, bool init, bool cryptEnabled)
    {
        BlockingCollection<IncomingRawPacket> queue;
        CancellationToken token;
        lock (_sync)
        {
            queue = _queue;
            token = _cancelTokenSource.Token;
        }

        try
        {
            EnsureWorker();
            int ahead = 0;
            try
            {
                ahead = queue.Count;
            }
            catch
            {
            }
            queue.Add(
                new IncomingRawPacket(data, init, cryptEnabled, Stopwatch.GetTimestamp(), ahead),
                token);
        }
        catch (OperationCanceledException){}
        catch (InvalidOperationException){}
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("IncomingGameQueue->AddItem " + ex);
        }
    }

    public void Stop()
    {
        Task worker;
        CancellationTokenSource oldCts;
        lock (_sync)
        {
            oldCts = _cancelTokenSource;
            try
            {
                if (oldCts != null && !oldCts.IsCancellationRequested)
                    oldCts.Cancel();
            }catch{}

            try{
                _queue?.CompleteAdding();
            }catch{}

            worker = _worker;
            _worker = null;
        }

        try
        {
            worker?.Wait(500);
        }catch{}

        lock (_sync)
        {
            try
            {
                oldCts?.Dispose();
            }
            catch{}

            _queue = new BlockingCollection<IncomingRawPacket>();
            _cancelTokenSource = new CancellationTokenSource();
            Volatile.Write(ref _running, 0);
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void EnsureWorker()
    {
        GameServerPacketHandler handler;
        BlockingCollection<IncomingRawPacket> queue;
        CancellationToken token;

        lock (_sync)
        {
            if (_handler == null)
                return;
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return;

            handler = _handler;
            queue = _queue;
            token = _cancelTokenSource.Token;
            _worker = Task.Run(() => Run(handler, queue, token));
        }
    }

    private void Run(
        GameServerPacketHandler handler,
        BlockingCollection<IncomingRawPacket> queue,
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                IncomingRawPacket item;
                try{
                    item = queue.Take(token);
                }
                catch (OperationCanceledException){
                    break;
                }
                catch (InvalidOperationException){
                    break;
                }

                try{
                    int remain = 0;
                    try
                    {
                        remain = queue.Count;
                    }
                    catch
                    {
                    }
                    PacketLatencyLog.SetIncomingRemain(remain);
                    handler.Handle(item);
                }
                catch (Exception ex){
                    UnityEngine.Debug.LogError("IncomingGameQueue->Handle " + ex);
                }
            }
        }
        finally{
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
