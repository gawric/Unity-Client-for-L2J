using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateBase : StateMachineBehaviour
{
    protected MonsterAnimationAudioHandler audioHandler;
    protected NetworkAnimationController _networkAnimationController;
    protected Animator animator;
    protected Entity _entity;
    public void LoadComponents(Animator animator) {
        if(this.animator == null) {
            this.animator = animator;
        }
        if (_entity == null) {
            _entity = animator.gameObject.GetComponent<Entity>();
            if (_entity == null)
                _entity = animator.GetComponentInParent<Entity>();
        }
        if(_networkAnimationController == null) {
            _networkAnimationController = animator.gameObject.GetComponent<NetworkAnimationController>();
            if (_networkAnimationController == null && _entity != null)
                _networkAnimationController = _entity.GetComponent<NetworkAnimationController>();
        }
        if (audioHandler == null) {
            audioHandler = animator.gameObject.GetComponent<MonsterAnimationAudioHandler>();
            if (audioHandler == null)
                audioHandler = animator.GetComponentInParent<MonsterAnimationAudioHandler>();
        }
    }

}
