
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static UnityEngine.Rendering.DebugUI;
using UnityEngine;



public class IncomingLoginDataQueue : IDisposable
{
    private BlockingCollection<ItemLogin> _queue;
    private static IncomingLoginDataQueue _instance;
    private ServerPacketHandler _serverPacketHandler;
    private static CancellationTokenSource _cancelTokenSource;
    private static CancellationToken _token;
    public static IncomingLoginDataQueue Instance()
    {
        if (_instance == null)
        {
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
            _instance =  new IncomingLoginDataQueue();
        }
        
      return _instance;
    }

    public void SetPacketHandler(ServerPacketHandler serverPacketHandler)
    {
        this._serverPacketHandler = serverPacketHandler;
    }
    public IncomingLoginDataQueue() {
        RestartQueue();
    }
    public void AddItem(byte[] data , bool init , bool cryptEnbled)
    {
        _queue.Add(new ItemLogin(data, init, cryptEnbled));
    }

    private void RestartQueue()
    {
        if (_queue == null)
        {
            _queue = new();
            WaitData();
        }
    }


    public void WaitData()
    {
        Task.Run(() =>
        {
            while (true)
            {
                if (_token.IsCancellationRequested)
                {
                    break;
                }

                if (_queue.Count > 0)
                {
                    ItemLogin item = _queue.Take();
                    if (item != null)
                    {
                        _serverPacketHandler.HandlePacket(item);
                    }
                }
                Thread.Sleep(10);
            }
        });
    }

    public void Dispose()
    {
        if (!_cancelTokenSource.IsCancellationRequested)
        {
            _cancelTokenSource.Cancel();
            _queue = null;
            _instance = null;
        }
    }
}
