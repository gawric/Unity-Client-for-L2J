using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

public sealed class UnityChainTicker : ITickable
{
    public void Tick()
    {
        Keyboard keyboard = Keyboard.current;
        // if (keyboard != null && keyboard.f4Key.wasPressedThisFrame)
            // UnityChainLog.Toggle();

        // UnityChainLog.Tick();
        // UnityReplaySession.Tick();
    }
}
