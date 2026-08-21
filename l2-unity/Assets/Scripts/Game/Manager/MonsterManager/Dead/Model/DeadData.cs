using UnityEngine;

public class DeadData
{
    private readonly Entity _entity;
    private float _elapsed;
    private Material[][] _instances;
    private bool _begun;

    public DeadData(Entity entity)
    {
        _entity = entity;
    }

    public Entity GetEntity()
    {
        return _entity;
    }

    public int GetIdEntity()
    {
        return _entity != null && _entity.Identity != null ? _entity.Identity.Id : 0;
    }

    public bool IsValid()
    {
        return _entity != null && _entity.gameObject != null;
    }

    public float Elapsed
    {
        get { return _elapsed; }
    }

    public void AddElapsed(float dt)
    {
        _elapsed += dt;
    }

    public bool TryBeginFade(L2ActorFade fade)
    {
        if (_begun)
        {
            return true;
        }

        if (!IsValid() || fade == null)
        {
            return false;
        }

        _begun = fade.TryBegin(_entity, out Renderer[] _, out _instances);
        return _begun;
    }

    public void SetAlphaByte(L2ActorFade fade, byte alphaByte)
    {
        if (fade == null)
        {
            return;
        }

        fade.SetAlphaByte(_instances, alphaByte);
    }
}
