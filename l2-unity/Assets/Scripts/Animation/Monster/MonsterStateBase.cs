using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterStateBase : StateMachineBehaviour
{
    protected MonsterAnimationAudioHandler audioHandler;
    protected NetworkAnimationController _networkAnimationController;
    protected Animator animator;
    protected Entity _entity;
    protected MonsterStateMachine _monsterStateMachine;
    public void LoadComponents(Animator animator) {
        if(this.animator == null) {
            this.animator = animator;
        }
        if (_entity == null) {
            _entity = animator.gameObject.GetComponent<Entity>();
        }
        if(_networkAnimationController == null) {
            _networkAnimationController = animator.gameObject.GetComponent<NetworkAnimationController>();
        }
        if (_monsterStateMachine == null)
        {
            _monsterStateMachine = animator.gameObject.GetComponent<MonsterStateMachine>();
        }
    }

}
