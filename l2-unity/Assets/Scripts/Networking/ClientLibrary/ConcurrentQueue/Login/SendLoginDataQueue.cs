using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SendLoginDataQueue : IDisposable
{
    private readonly object _sync = new object();
    private readonly IProtocol _protocol;
    private BlockingCollection<ItemSendLogin> _queue;
    private ClientPacketHandler _clientPacketHandler;
    private CancellationTokenSource _cancelTokenSource;
    private Task _worker;
    private int _running;

    public SendLoginDataQueue(IProtocol protocol)
    {
        _protocol = protocol;
        _queue = new BlockingCollection<ItemSendLogin>();
        _cancelTokenSource = new CancellationTokenSource();
    }

    public void SetPacketHandler(ClientPacketHandler clientPacketHandler)
    {
        lock (_sync)
            _clientPacketHandler = clientPacketHandler;
        EnsureWorker();
    }

    public void AddItem(INetworkCommand command)
    {
        if (command == null)
            return;

        BlockingCollection<ItemSendLogin> queue;
        CancellationToken token;
        lock (_sync)
        {
            queue = _queue;
            token = _cancelTokenSource.Token;
        }

        try
        {
            EnsureWorker();
            queue.Add(new ItemSendLogin(command), token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogError("SendLoginDataQueue->AddItem " + ex);
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
            }
            catch
            {
            }

            try
            {
                _queue?.CompleteAdding();
            }
            catch
            {
            }

            worker = _worker;
            _worker = null;
        }

        try
        {
            worker?.Wait(500);
        }
        catch
        {
        }

        lock (_sync)
        {
            try
            {
                oldCts?.Dispose();
            }
            catch
            {
            }

            _queue = new BlockingCollection<ItemSendLogin>();
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
        ClientPacketHandler handler;
        BlockingCollection<ItemSendLogin> queue;
        CancellationToken token;

        lock (_sync)
        {
            if (_clientPacketHandler == null)
                return;
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                return;

            handler = _clientPacketHandler;
            queue = _queue;
            token = _cancelTokenSource.Token;
            _worker = Task.Run(() => Run(handler, queue, token));
        }
    }

    private void Run(
        ClientPacketHandler handler,
        BlockingCollection<ItemSendLogin> queue,
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                ItemSendLogin item;
                try
                {
                    item = queue.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (InvalidOperationException)
                {
                    break;
                }

                try
                {
                    IOutgoingPacket packet = _protocol.EncodeLogin(item.Command);
                    if (packet == null)
                        continue;

                    ((LoginClientPacketHandler)handler).SendPacket(packet);
                }
                catch (Exception ex)
                {
                    Debug.LogError("SendLoginDataQueue->Send " + ex);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
