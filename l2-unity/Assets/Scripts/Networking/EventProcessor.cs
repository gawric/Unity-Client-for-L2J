using System;
using System.Collections.Concurrent;
using UnityEngine;

public class EventProcessor : MonoBehaviour
{
    private static EventProcessor _instance;
    public static EventProcessor Instance { get { return _instance; } }

    private readonly ConcurrentQueue<Action> _events = new ConcurrentQueue<Action>();

    private void Awake()
    {
        if (_instance == null)
            _instance = this;
        else if (_instance != this)
            Destroy(this);
    }

    private void Start()
    {
        World world = IncomingPacketActions.GameWorld;
        if (world != null && world.OfflineMode)
        {
            this.enabled = false;
            return;
        }
    }

    public void QueueEvent(Action action)
    {
        _events.Enqueue(action);
    }

    private void Update()
    {
        Action action;
        while (_events.TryDequeue(out action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError("Критическая ошибка EventProcessor: " + ex);
                LobbyFlowLog.Exception("EventProcessor.Update (Apply on main thread)", ex);
            }
        }
    }
}
