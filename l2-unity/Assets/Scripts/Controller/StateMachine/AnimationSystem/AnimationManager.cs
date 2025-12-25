using System;
using System.Collections.Generic;
using UnityEngine;


public class AnimationManager : IAnimationManager
{
   
    private PlayerEntity _player;
    private static AnimationManager _instance;
    private string[] recentAnimationNames = new string[2];
    private Dictionary<int, string[]> recentMonsterAnimationNames = new Dictionary<int, string[]>();
    private List<string> listTriggerAfterStart = new List<string>(10);
    private float _remainingAtkTime = 0;
    public event Action<string> OnAnimationFinished;
    public event Action<string , float> OnAnimationStartShoot;
    public event Action<string> OnAnimationLoadArrow;

    public void SetAnimationManager(PlayerAnimationController controller, PlayerEntity player)
    {
        _player = player;
        PlayerAnimationController.Instance.OnAnimationFinished += AnimationFinishedPlayerCallback;
        PlayerAnimationController.Instance.OnAnimationStartShoot += AnimationShootPlayerCallback;
        PlayerAnimationController.Instance.OnAnimationStartLoadArrow += AnimationLoadArrowPlayerCallback;
    }
    public static IAnimationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnimationManager();
            }
            return _instance;
        }
    }

    
 
    public void PlayAnimation(string animationName, bool disableTriggerAfterStart)
    {
       
        string finalAnimName = GetFinalNameAnim(animationName);

        DesibleLastAnimationElseTrue();

        //Debug.Log($"AnimationManager> start name player  {_player.name} animation {finalAnimName}");
        SetRecentName(finalAnimName);
        AddDebugInfo(finalAnimName);
        PlayerAnimationController.Instance.SetBool(finalAnimName, true , _player.name);
    }


    public void PlayMonsterAnimation(int mId , NetworkAnimationController controllerAnimator , string animationName)
    {
        
        DesibleLastAnimationElseTrue(mId , controllerAnimator, animationName);
        SetMonsterRecentName(mId , animationName);
        controllerAnimator.SetBool(animationName, true);
    }
    //PlayMonsterAnimation может запускать 2 состояния однорвеменно т.к BaseState  требует время на запуск после запуска анимации и не всегда может успеть отключить анимацию!
    private void DesibleLastAnimationElseTrue(int mId, NetworkAnimationController controllerAnimator , string animationName)
    {
        if(recentMonsterAnimationNames.ContainsKey(mId))
        {
            string[] arrAnim = recentMonsterAnimationNames[mId];

            if (controllerAnimator.GetBool(arrAnim[0]) == true)
            {
                    //Debug.Log("MosterAnimation> start animation alert stop " + arrAnim[0] + " disabled. startAnimatorName " + animationName  + " name animator " + controllerAnimator.name + " monster ID " + mId);
                    controllerAnimator.SetBool(arrAnim[0], false);
            }
        }
    }

    private void DesibleLastAnimationElseTrue()
    {
        if (!string.IsNullOrEmpty(GetCurrentAnimationName()))
        {
            string currentAnimation = GetCurrentAnimationName();
            if (PlayerAnimationController.Instance.GetBool(currentAnimation))
            {
                PlayerAnimationController.Instance.SetBool(currentAnimation, false);
            }

            //Debug.Log($"AnimationManager> start name player  {_player.name} name animation {currentAnimation}");
        }
    }

    

    public void StopMonsterCurrentAnimation(Animator animator, string animationName)
    {
        //Debug.Log("MosterAnimation> stop animation " + animationName + " animatorName " + animator.name);
        animator.SetBool(animationName, false);
    }




    public Dictionary<string, float>  PlayerGetAllFloat()
    {
       return  PlayerAnimationController.Instance.GetParametrs();
    }


    public void  PlayerSetAllFloat(Dictionary<string, float> floatValues)
    {
        PlayerAnimationController.Instance.SetParametrs(floatValues);
    }



    public void PlayOriginalAnimation(string animationName)
    {
        AddDebugInfo(animationName);
        PlayerAnimationController.Instance.SetBool(animationName, true);
    }

    public string GetFinalNameAnim(string animationName)
    {
        return animationName + GetEquipAnimName();
    }

    private void SetRecentName(string animationName)
    {
        if(recentAnimationNames[0] == null)
        {
            recentAnimationNames[0] = animationName;
        }
        else
        {
            recentAnimationNames[1] = recentAnimationNames[0];
            recentAnimationNames[0] = animationName;
        }
    }
    //Используется для правильного переключений анимации но это когда настройки анимации не верные Transition Duration выставлен > 0.1 тогда может сработать этот код
    //Основная его цель это дебуг анимации
    private void SetMonsterRecentName(int objId , string animationName)
    {
        if (recentMonsterAnimationNames.ContainsKey(objId))
        {
            string[] arrAnim = recentMonsterAnimationNames[objId];

            if (arrAnim[0] == null)
            {
                arrAnim[0] = animationName;
            }
            else
            {
                arrAnim[1] = arrAnim[0];
                arrAnim[0] = animationName;
            }
        }
        else
        {
            string[] arrAnim = new string[2];
            arrAnim[0] = animationName;
            recentMonsterAnimationNames.Add(objId, arrAnim);
        }
      
    }

    private void AddDebugInfo(string animationName)
    {
        if (listTriggerAfterStart.Count < 10)
        {
            listTriggerAfterStart.Add(animationName);
        }
        else
        {
            listTriggerAfterStart.RemoveAt(0);
            listTriggerAfterStart.Add(animationName);
        }
    }

    private void DeleteDebugInfo(string animationName)
    {
        if (listTriggerAfterStart.Contains(animationName))
        {
            listTriggerAfterStart.Remove(animationName);
        }
    }

    public void PrintRecentAnimationNames()
    {
        if(listTriggerAfterStart.Count != 0)
        {
            Debug.Log("++++++ BEGIN Debug Info List +++++++ ");
            foreach (var animName in listTriggerAfterStart)
            {
                Debug.Log("Debug Info List Need Stop Triggers " + animName);
            }
            Debug.Log("++++++ END Debug Info List +++++++ ");
        }

    }

    public string GetLastAnimationName()
    {
        if(recentAnimationNames[1] == null)
        {
            return "";
        }
        return recentAnimationNames[1];
    }

    public string GetCurrentAnimationName()
    {
        if (recentAnimationNames[0] == null)
        {
            return "";
        }

        return recentAnimationNames[0];
    }

    public string GetEquipAnimName()
    {
        return _player.GetEquippedWeaponName();
    }

    public void StopCurrentAnimation(string paramName , string runName = "")
    {
        //Debug.Log("Walking State STOP Event param name 1 " + paramName);
        if (!string.IsNullOrEmpty(paramName))
        {
            //Debug.Log("Walking State STOP Event param name 2 " + paramName);
            if (PlayerAnimationController.Instance != null)
            {
                //Debug.Log("Walking State STOP Event param name 3 " + paramName);
                PlayerAnimationController.Instance.SetBool(paramName, false , runName);
            }
            
        }
        else
        {
            Debug.Log("AnimationManager>StopCurrentAnimation: Не критическая ошибка остановки bool анимации ");
        }
    }

    public void AnimationFinishedPlayerCallback(string animationName)
    {
        OnAnimationFinished?.Invoke(animationName);
    }

    public void AnimationShootPlayerCallback(string animationName)
    {
        OnAnimationStartShoot?.Invoke(animationName , _remainingAtkTime);
    }

    public void AnimationLoadArrowPlayerCallback(string animationName)
    {
        OnAnimationLoadArrow?.Invoke(animationName);
    }

    public void UpdateRemainingAtkTime(float remainingAtkTime)
    {
        _remainingAtkTime = remainingAtkTime;
    }

    public float GetRemainingAtkTime()
    {
        return _remainingAtkTime;
    }


}
