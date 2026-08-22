using UnityEngine;

public class MonsterStateAtkWait : MonsterStateBase
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        LoadComponents(animator);
        int id = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        Debug.Log(
            "[ANIM] AtkWait ENTER id=" + id +
            " name=" + EntityActionCombatLog.NameOf(_entity) +
            " inCombat=" + (_entity != null && _entity.InCombat) +
            " action=" + (_entity != null ? _entity.ActionSlot.Action.ToString() : "none"));
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        int id = animator.GetInteger(AnimatorUtils.OBJECT_ID);
        Debug.Log("[ANIM] AtkWait EXIT id=" + id);
    }
}
