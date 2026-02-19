using UnityEngine;

public class GearEffects : MonoBehaviour
{
    [Header("Body")]
    [SerializeField] protected Transform _bodyCenter;
    //bone01-04 meybe center bone 
    //Bone01-03 - size object
    public Transform GetBodyCenter()
    {
        if (_bodyCenter == null)
        {
            _bodyCenter = PlayerAnimationController.Instance.transform.FindRecursive("Bone01-03");
        }
        return _bodyCenter;
    }
}
