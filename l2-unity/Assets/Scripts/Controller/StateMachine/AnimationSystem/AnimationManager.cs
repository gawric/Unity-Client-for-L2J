using NUnit.Framework;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;


public class AnimationManager : IAnimationManager
{
    private PlayerAnimationController _controller;
    private PlayerEntity _player;
    private static AnimationManager _instance;
    private string[] recentAnimationNames = new string[2];
    private Dictionary<int, string[]> recentMonsterAnimationNames = new Dictionary<int, string[]>();
    private List<string> listTriggerAfterStart = new List<string>(10);
    public void SetAnimationManager(PlayerAnimationController controller , PlayerEntity player)
    {
        _controller = controller;
        _player = player;


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
        SetRecentName(finalAnimName);
        AddDebugInfo(finalAnimName);
        PlayerAnimationController.Instance.SetBool(finalAnimName, true);
        //Debug.Log("Walking State PlayAnimation " + finalAnimName);
    }


    public void PlayMonsterAnimation(int mId , NetworkAnimationController controllerAnimator , string animationName)
    {
        
        DesibleLastAnimationElseTrue(mId , controllerAnimator, animationName);
        //Debug.Log("MosterAnimation> start animation " + animationName + " animatorName " + controllerAnimator.name);
        SetMonsterRecentName(mId , animationName);
        controllerAnimator.SetBool(animationName, true);
    }
    //PlayMonsterAnimation ����� ��������� 2 ��������� ������������ �.� BaseState  ������� ����� �� ������ ����� ������� �������� � �� ������ ����� ������ ��������� ��������!
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

    public void StopMonsterCurrentAnimation(Animator animator, string animationName)
    {
        //Debug.Log("MosterAnimation> stop animation " + animationName + " animatorName " + animator.name);
        animator.SetBool(animationName, false);
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
    //������������ ��� ����������� ������������ �������� �� ��� ����� ��������� �������� �� ������ Transition Duration ��������� > 0.1 ����� ����� ��������� ���� ���
    //�������� ��� ���� ��� ����� ��������
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

    public void StopCurrentAnimation(string paramName)
    {
        //Debug.Log("Walking State STOP Event param name 1 " + paramName);
        if (!string.IsNullOrEmpty(paramName))
        {
            //Debug.Log("Walking State STOP Event param name 2 " + paramName);
            if (PlayerAnimationController.Instance != null)
            {
                //Debug.Log("Walking State STOP Event param name 3 " + paramName);
                PlayerAnimationController.Instance.SetBool(paramName, false);
            }
            
        }
        else
        {
            Debug.Log("AnimationManager>StopCurrentAnimation: �� ����������� ������ ��������� bool �������� ");
        }
    }

   
}
