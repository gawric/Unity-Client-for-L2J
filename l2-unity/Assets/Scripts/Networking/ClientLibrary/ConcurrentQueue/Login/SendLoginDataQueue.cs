using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class SendLoginDataQueue : IDisposable
{
    private BlockingCollection<ItemSendLogin> _queue;
    private static SendLoginDataQueue _instance;
    protected ClientPacketHandler _clientPacketHandler;
    private static CancellationTokenSource _cancelTokenSource;
    private static CancellationToken _token;
    public static SendLoginDataQueue Instance()
    {
        if (_instance == null)
        {
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
            _instance = new SendLoginDataQueue();
        }

        return _instance;
    }

    public void SetPacketHandler(ClientPacketHandler clientPacketHandler)
    {
        this._clientPacketHandler = clientPacketHandler;
    }
    public SendLoginDataQueue()
    {
        if (_queue == null) _queue = new();
        WaitSendData();
    }
    public void AddItem(ClientPacket packet , bool encrypt, bool blowfish)
    {
        if (_queue == null) _queue = new();
        _queue.Add(new ItemSendLogin(packet , encrypt, blowfish));
    }

    public void WaitSendData()
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
                    ItemSendLogin item = _queue.Take();
                    if (item != null)
                    {
                        var sender = (LoginClientInterludePacketHandler)_clientPacketHandler;
                        sender.SendPacket(item.GetPacket());
                    }
                    
                }
                Thread.Sleep(100);
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
