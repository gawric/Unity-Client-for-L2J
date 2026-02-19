using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class IncomingGameDataQueue : IQueue
{
    private ConcurrentQueue<ItemServer> _queue;
    private static IncomingGameDataQueue _instance;
    private ServerPacketHandler _serverPacketHandler;
    private static CancellationTokenSource _cancelTokenSource;
    private static CancellationToken _token;
    private bool _isRunning = false;
    public static IncomingGameDataQueue Instance()
    {
        if (_instance == null)
        {
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
            _instance = new IncomingGameDataQueue();
        }

        return _instance;
    }

    public void SetPacketHandler(ServerPacketHandler serverPacketHandler)
    {
        this._serverPacketHandler = serverPacketHandler;
    }
    public IncomingGameDataQueue()
    {
        RestartQueue();
    }

    public ItemServer CreateItem(byte[] data, bool init, bool cryptEnbled)
    {
        return new ItemServer(data, init, cryptEnbled);
    }


    public void AddItem(ItemServer item)
    {
        try
        {
            if (FilterAccessType.IsAccessTypeGame(item))
            {
                _queue.Enqueue(item);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("IncomingGameDataQueue->AddItem " + ex.ToString());
        }
    }



    private void RestartQueue()
    {
        if (_queue == null)
        {
            _queue = new ConcurrentQueue<ItemServer>();
        }
        if (!_isRunning)
        {
            _isRunning = true;
            WaitData();
        }

    }


    public void WaitData()
    {
        Task.Run(() =>
        {
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    UseNormal();
                }
            }
            catch (Exception ex)
            {
                Debug.Log("Error in WaitData->GameData " + ex.Message);
                _isRunning = false;
                RestartQueue();
            }
            finally
            {
                _isRunning = false;
            }
        });
    }


    private void UseNormal()
    {
        if (_queue.TryDequeue(out ItemServer item))
        {
            Task.Run(() =>
            {
                _serverPacketHandler.HandlePacket(item);
            });
        }

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
