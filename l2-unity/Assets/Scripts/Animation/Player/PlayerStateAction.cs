

using UnityEngine;


public class PlayerStateAction : PlayerStateBase
{
    protected bool ShouldSit()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.SITTING)
        {
            SetBool("sit", false, true, false); //Do not share sit animation (shared by server with ChangeWaitType)
                                                // At some point need to get rid of the ShareAnimation packet 
            return true;
        }

        return false;
    }

    protected bool ShouldJump(bool run)
    {
        if (PlayerStateMachine.Instance.State == PlayerState.IDLE || PlayerStateMachine.Instance.State == PlayerState.RUNNING)
        {
            if (InputManager.Instance.Jump)
            {
                CameraController.Instance.StickToBone = true;
                if (run)
                {
                    SetBool("run_jump", false, true);
                }
                else
                {
                    SetBool("jump", false, true);
                }
                return true;
            }
        }

        return false;
    }

    protected bool ShouldRun()
    {
       // UnityEngine.Debug.Log(" State MoveToIntention Actions " + PlayerStateMachine.Instance.State);
        if (PlayerStateMachine.Instance.State == PlayerState.RUNNING)
        {
            SetBool("run", true, true);
            return true;
        }

        return false;
    }

    protected bool ShouldWalk()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.WALKING)
        {
            SetBool("walk", true, true);
            return true;
        }

        return false;
    }

    protected bool ShouldIdle()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.IDLE | PlayerStateMachine.Instance.State == PlayerState.WAIT_RETURN)
        {
            //UnityEngine.Debug.Log("Dead Idle test 1");
            //if (PlayerStateMachine.Instance.IsAutoAttack)
           // {
                //Debug.Log("IDLE AutoAttack true");
             //   SetBool("atkwait", true, true);
           // }
           // else
           // {
            //    //Debug.Log("IDLE AutoAttack false");
            //    SetBool("wait", true, true, false);
           // }
            //SetBool("wait", true, true);
           // return true;
        }

        return false;
    }


    protected bool ShouldAttack()
    {
        //Debug.Log("SHOUULDDD ATACK!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        if (PlayerStateMachine.Instance.State == PlayerState.ATTACKING)
        {
            return true;
        }

        return false;
    }

    protected bool ShouldDie()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.DEAD)
        {
            if (!GetBool("death"))
            {
                SetBool("death", false, true);
                //UnityEngine.Debug.Log("Start Animation Die");
                //SetBool("death", false, false);
                return true;
            }
        }

        return false;
    }

    //protected bool ShouldAtkWait() {
    //    long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    //    if ((!InputManager.Instance.IsInputPressed(InputType.Move) && !PlayerController.Instance.RunningToDestination || !PlayerController.Instance.CanMove)
    //         && now - _entity.StopAutoAttackTime < 5000) {
    //        if (PlayerEntity.Instance.AttackTarget == null) {
    //            SetBool("atkwait_" + _weaponAnim, true, false);
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    protected bool ShouldStand()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.STANDING)
        {
            //UnityEngine.Debug.Log("SHold Stend ");
            SetBool("stand", false, true, false);
            return true;
        }

        return false;
    }

    protected bool ShouldRebirth()
    {
        if (PlayerStateMachine.Instance.State == PlayerState.REBIRTH)
        {
            SetBool("rebirth", false, true, false);
            return true;
        }

        return false;
    }

    //protected bool ShouldCast()
   // {
        //if (PlayerStateMachine.Instance.State == PlayerState.MAGIC_CAST)
       // {
           // bool start = GetBool("cast_short");
            //if (!start)
            //{
              //  SetBool("cast_short", false, true, false);
              //  PlayerStateMachine.Instance.ChangeState(PlayerState.ANIMATION_LOCKED);
            //}
            
            //return true;
       // }

       // return false;
   // }

  
    // protected bool ShouldCastLoop()
    // {
    // if (PlayerStateMachine.Instance.State == PlayerState.MAGIC_CAST_LOOP)
    //{
    //    SetBool("spatk_loop01", false, true, false);
    //    return true;
    // }

    //  return false;
    //}

    // protected bool ShouldCastShot()
     //{
       // if (PlayerStateMachine.Instance.State == PlayerState.MAGIC_CAST_SHOT)
        //{
          //  PlayerStateMachine.Instance.ChangeState(PlayerState.ANIMATION_LOCKED);
           //// bool start = GetBool("cast_short");
          //  if (!start)
           // {
            //    SetBool("magic_shot", false, true, false);
           // }

           // return true;
       // }

       // return false;
     //}

    protected bool ShouldAtkWait()
    {
       if (PlayerStateMachine.Instance.State == PlayerState.WAIT_RETURN)
       {
            Debug.Log(" Attack Sate to Intention TIMEOUT Запуск atk " + PlayerStateMachine.Instance.State + " startTime " + Time.time);
            SetBool("atkwait", true, true, false);
            return true;
       }
        return false;
    }
}
