using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;


public class IncomingGameMessageQueue: IQueue
{

    private ConcurrentQueue<ItemServer> _queue;
    private static IncomingGameMessageQueue _instance;
    private GSInterludeMessageHandler _serverPacketHandler;
    private static CancellationTokenSource _cancelTokenSource;
    private static CancellationToken _token;
    private bool _isRunning = false;

    public static IncomingGameMessageQueue Instance()
    {
        if (_instance == null)
        {
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
            _instance = new IncomingGameMessageQueue();

        }

        return _instance;
    }



    public void SetPacketHandler(ServerPacketHandler serverPacketHandler)
    {
        this._serverPacketHandler = (GSInterludeMessageHandler) serverPacketHandler;
    }

    public ServerPacketHandler GetPacketHandler()
    {
        return _serverPacketHandler;
    }

    public IncomingGameMessageQueue()
    {
        RestartQueue();
    }


    public void AddItem(ItemServer item)
    {
        try
        {
            if (FilterAccessType.IsAccessTypeMessage(item))
            {
                _queue.Enqueue(item);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("IncomingGameMessageQueue->AddItem " + ex.ToString());
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
                Debug.Log("Error in WaitData->CombatData " + ex.Message);
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
            _serverPacketHandler.HandlePacket(item);
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

    
