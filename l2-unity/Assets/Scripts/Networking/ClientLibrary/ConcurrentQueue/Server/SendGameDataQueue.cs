using L2_login;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SendGameDataQueue
{
    private ConcurrentQueue<ItemSendServer> _queue;
    private static SendGameDataQueue _instance;
    protected ClientPacketHandler _clientPacketHandler;
    private static CancellationTokenSource _cancelTokenSource;
    private static CancellationToken _token;
    private bool _isRunning = false;
    public static SendGameDataQueue Instance()
    {
        if (_instance == null)
        {
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
            _instance = new SendGameDataQueue();
        }

        return _instance;
    }

    public void SetPacketHandler(ClientPacketHandler clientPacketHandler)
    {
        this._clientPacketHandler = clientPacketHandler;
    }
    public SendGameDataQueue()
    {
        if (_queue == null) _queue = new();
        WaitSendData();
    }
    //public void AddItem(ClientPacket packet, bool encrypt, bool blowfish)
   // {
        //if (_queue == null) _queue = new();
       // _queue.Add(new ItemSendServer(packet, encrypt, blowfish));
    //}

    public void AddItem(ClientPacket packet, bool encrypt, bool blowfish)
    {
        try
        {
            if (_queue == null) _queue = new();
            _queue.Enqueue(new ItemSendServer(packet, encrypt, blowfish));

        }
        catch (Exception ex)
        {
            Debug.LogError("SendGameDataQueue->AddItem " + ex.ToString());
        }
    }

    public void WaitSendData()
    {
        Debug.Log("Start Wait Send Data Game CLient");
        Task.Run(() =>
        {
            try
            {
                    while (!_token.IsCancellationRequested)
                    {
                        if (_queue.TryDequeue(out ItemSendServer item))
                        {
                            var sender = (GameClientInterludePacketHandler)_clientPacketHandler;
                            sender.SendPacket(item.GetPacket());
                        }
                    }
            }
            catch (Exception ex)
            {
                Debug.Log("Error in SendGameDataQueue->WaitSendData " + ex.Message);
                RestartQueue();
            }
            finally
            {
                _isRunning = false;
            }

        });
    }

    private void RestartQueue()
    {
        if (_queue == null)
        {
            _queue = new ConcurrentQueue<ItemSendServer>();
        }
        if (!_isRunning)
        {
            _isRunning = true;
            WaitSendData();
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
