using UnityEngine;

/// <summary>
/// Main-thread tick for <see cref="ItemDropPresentationService"/> (no coroutines).
/// Bound from <see cref="World"/> Awake so it always runs on the game scene object.
/// </summary>
public sealed class ItemDropPresentationRunner : MonoBehaviour
{
    ItemDropPresentationService _service;

    public void Bind(ItemDropPresentationService service)
    {
        _service = service;
    }

    void Update()
    {
        _service?.Tick(Time.deltaTime);
    }
}
