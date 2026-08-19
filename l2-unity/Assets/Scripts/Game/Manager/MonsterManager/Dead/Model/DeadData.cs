using UnityEngine;

public class DeadData
{
    private bool _useAntiGravity;
    private float _currentPos;
    private float _zeroPos;
    private bool _isRefresh;
    private Entity _entity;

    public DeadData(bool useAntiGravity  , Entity entity)
    {
        _useAntiGravity = useAntiGravity;
        _entity = entity;
    }

    public int GetIdEntity()
    {
        return _entity.Identity.Id;
    }
    public void SetRefresh(bool refresh)
    {
        _isRefresh = refresh;
    }

    public void SetAntiGravity(bool useAntiGravity)
    {
        _useAntiGravity = useAntiGravity;
    }

    public Entity GetEntity()
    {
        return _entity;
    }
    public void SetZeroPos(float zeroPos)
    {
        _zeroPos = zeroPos;
    }

    public void SetCurrentPos(float currentPos)
    {
        _currentPos = currentPos;
    }

    public float GetZeroPos()
    {
        return _zeroPos;
    }

    public float GetCurrentPos()
    {
        return _currentPos;
    }

    public bool IsAntiGravity()
    {
        return _useAntiGravity;
    }

    public bool IsRefresh()
    {
        return _isRefresh;
    }

    public Renderer[]  GetRender()
    {
        return _entity.gameObject.GetComponentsInChildren<Renderer>();
    }
}
