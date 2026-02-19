using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public abstract class BaseAnimationManager
{
    protected Dictionary<int, AnimationModel> _animationControllers;
    protected Dictionary<int, string[]> _recentAnimationNames;
    protected Dictionary<int, string[]> _recentMonsterAnimationNames;
    protected Dictionary<int, TaskCompletionSource<bool>> _tcsMap;


    protected BaseAnimationManager()
    {
        _animationControllers = new Dictionary<int, AnimationModel>();
        _recentAnimationNames = new Dictionary<int, string[]>();
        _recentMonsterAnimationNames = new Dictionary<int, string[]>();
        _tcsMap = new();
    }
    public void RegisterController(int objectId, IAnimationController controller , Entity entity)
    {
        _animationControllers[objectId] = new AnimationModel(controller , entity);
        controller.SetInt(AnimatorUtils.OBJECT_ID, objectId);

    }



    public bool IsPlayerController(int objectId)
    {
        return _animationControllers.ContainsKey(objectId) &&
               _animationControllers[objectId].GetType() == typeof(PlayerAnimationController);
    }

    public bool IsMonsterController(int objectId)
    {
        return _animationControllers.ContainsKey(objectId) &&
               _animationControllers[objectId].GetType() == typeof(NetworkAnimationController);
    }

    public PlayerAnimationController GetPlayerController(int objectId)
    {
        if (IsPlayerController(objectId))
        {
            return _animationControllers[objectId].GetController() as PlayerAnimationController;
        }
        return null;
    }

    public AnimationEventsBase GetAnimationEvents(int objectId)
    {
        if (_animationControllers.ContainsKey(objectId))
        {
            return _animationControllers[objectId].GetEvents();
        }
        return null;
    }

    public AnimationModel GetModel(int objectId)
    {
        return  _animationControllers[objectId];
    }



    public Entity GetEntity(int objectId)
    {
        return _animationControllers[objectId].GetEntity();
    }

    public NetworkAnimationController GetMonsterController(int objectId)
    {
        if (IsMonsterController(objectId))
        {
            return _animationControllers[objectId].GetController() as NetworkAnimationController;
        }
        return null;
    }

    public string GetCurrentAnimationName(int objectId)
    {
        if (!_recentAnimationNames.ContainsKey(objectId) ||
            _recentAnimationNames[objectId][0] == null)
        {
            return "";
        }
        return _recentAnimationNames[objectId][0];
    }


    public void PlayerSetAllFloat(int objectId , Dictionary<string, float> floatValues)
    {
        PlayerAnimationController controller = GetPlayerController(objectId);
        controller.SetParametrs(floatValues);
    }



    public void PlayOriginalAnimation(int objectId , string animationName)
    {
        IAnimationController controllerAnimator = GetPlayerController(objectId);
        PlayerAnimationController.Instance.SetBool(animationName, true);
    }

    public string GetFinalNameAnim(int objectId , string animationName)
    {
        if(animationName.IndexOf("sit") > -1)
        {
            return "sit";
        }

        return animationName + GetEquipAnimName(objectId);
    }

    public string GetEquipAnimName(int objectId)
    {
        AnimationModel model = _animationControllers[objectId];
        PlayerEntity entity = (PlayerEntity)model.GetEntity();
        return entity.GetEquippedWeaponName();
    }


    public string GetLastAnimationName(int objectId)
    {
        if (!_recentAnimationNames.ContainsKey(objectId) ||
            _recentAnimationNames[objectId][1] == null)
        {
            return "";
        }
        return _recentAnimationNames[objectId][1];
    }


    public void StopCurrentAnimation(int objectId, string paramName, string runName = "")
    {
        if (!_animationControllers.ContainsKey(objectId))
        {
            Debug.LogError($"No animation controller found for object ID: {objectId}");
            return;
        }

        if (!string.IsNullOrEmpty(paramName))
        {
            _animationControllers[objectId].GetController().SetBool(paramName, false);
        }
        else
        {
            Debug.Log("AnimationManager>StopCurrentAnimation: Non-critical error stopping bool animation");
        }
    }

    protected void DisableLastMonsterAnimationElseTrue(int objectId, IAnimationController controllerAnimator, string animationName)
    {
  
        if (_recentMonsterAnimationNames.ContainsKey(objectId)) 
        {
            string[] arrAnim = _recentMonsterAnimationNames[objectId];

            if (arrAnim[0] != null && controllerAnimator.GetBool(arrAnim[0]))
            {
                Debug.Log($"MonsterAnimation> Stopping previous animation {arrAnim[0]} for monster/object {objectId}");
                controllerAnimator.SetBool(arrAnim[0], false);
            }
        }
    }



    protected void DesableLastPlayerAnimationElseTrue(int objectId, IAnimationController controller)
    {
        if (!string.IsNullOrEmpty(GetCurrentAnimationName(objectId)))
        {
            string currentAnimation = GetCurrentAnimationName(objectId);
            if (controller.GetBool(currentAnimation))
            {
                controller.SetBool(currentAnimation, false);
                //Debug.Log($"AnimationManager> stop name player  {_player.name} name animation {currentAnimation}");
            }

        }
    }


    protected void SetRecentName(int objectId, string animationName)
    {
        _recentAnimationNames[objectId] = _recentAnimationNames.TryGetValue(objectId, out var arrAnim)
            ? new[] { animationName, arrAnim[0] }
            : new[] { animationName, null };
    }


    protected void SetMonsterRecentName(int objId, string animationName)
    {
        if (!_recentMonsterAnimationNames.TryGetValue(objId, out var arrAnim))
        {
            arrAnim = new string[2];
            _recentMonsterAnimationNames[objId] = arrAnim;
        }

        arrAnim[1] = arrAnim[0];
        arrAnim[0] = animationName;
    }


}
