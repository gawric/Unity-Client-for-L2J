using System;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class AnimationModel
{
    private IAnimationController _controller;
    private Entity _entity;
    private Type _typeController;
    private AnimationEventsBase _eventSource;

    public event Action<string, int> OnAnimationFinishedWithId;


    public AnimationModel(IAnimationController controller , Entity entity)
    {
        _controller = controller;
        _entity = entity;
        _typeController = controller.GetType();
        _eventSource = controller as AnimationEventsBase;
    }

  

    public IAnimationController GetController() => _controller;
    public Entity GetEntity() => _entity;
    public Type GetType() => _typeController;

  

    public AnimationEventsBase GetEvents() => _eventSource;

    public void SubscribeToInternalEvents()
    {
        if (_eventSource != null)
        {
            // Подписываемся на базовое событие из AnimationEventsBase
            _eventSource.OnAnimationFinished += HandleBaseAnimationFinished;
        }
    }

 
    private void HandleBaseAnimationFinished(string animName)
    {
        int objectId = _entity.IdentityInterlude.Id;
        OnAnimationFinishedWithId?.Invoke(animName, objectId);
    }

    public void Dispose()
    {
        if (_eventSource != null)
        {
            _eventSource.OnAnimationFinished -= HandleBaseAnimationFinished;
        }
    }

}
