using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SendGameDataQueue : IDisposable
{
    private readonly object _sync = new object();
    private readonly IProtocol _protocol;
    private BlockingCollection<ItemSendServer> _queue;
    private ClientPacketHandler _clientPacketHandler;
    private CancellationTokenSource _cancelTokenSource;
    private Task _worker;
    private int _running;

    public SendGameDataQueue(IProtocol protocol)
    {
        _protocol = protocol;
        _queue = new BlockingCollection<ItemSendServer>();
        _cancelTokenSource = new CancellationTokenSource();
    }

    public void SetPacketHandler(ClientPacketHandler clientPacketHandler)
    {
        lock (_sync)
            _clientPacketHandler = clientPacketHandler;
        EnsureWorker();
    }

    public void AddItem(INetworkCommand command, bool encrypt)
    {
        if (command == null)
            return;

        BlockingCollection<ItemSendServer> queue;
        CancellationToken token;
        lock (_sync)
        {
            queue = _queue;
            token = _cancelTokenSource.Token;
        }

        try
        {
            EnsureWorker();
            queue.Add(new ItemSendServer(command, encrypt), token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogError("SendGameDataQueue->AddItem " + ex);
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

            _queue = new BlockingCollection<ItemSendServer>();
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
        BlockingCollection<ItemSendServer> queue;
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
        BlockingCollection<ItemSendServer> queue,
        CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                ItemSendServer item;
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
                    IOutgoingPacket packet = _protocol.EncodeGame(item.Command, item.Encrypt);
                    if (packet == null)
                    {
                        LobbyFlowLog.Warn("TX EncodeGame returned null command=" + item.Command.GetType().Name);
                        continue;
                    }

                    LobbyFlowLog.Info("TX " + item.Command.GetType().Name + " encrypt=" + item.Encrypt + " opcode=0x" + packet.GetPacketType().ToString("X2"));
                    ((GameClientPacketHandler)handler).SendPacket(packet);
                }
                catch (Exception ex)
                {
                    Debug.LogError("SendGameDataQueue->Send " + ex);
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
