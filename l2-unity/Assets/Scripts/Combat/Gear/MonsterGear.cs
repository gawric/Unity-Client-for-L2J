using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Diagnostics;

public class MonsterGear : Gear
{
    private const string BONE_CLAVICLE_R_NAME = "Bip01 R UpperArm";
    private const string BONE_CLAVICLE_L_NAME = "Bip01 L UpperArm";
    private const string BONE_HEAD = "Bip01 head";
    //private const string BONE_TAIL = "Bip01 Tail";
    private const string BONE_TAIL = "Bip01 Spine";
    //Bip01 Spine

    private Transform clavicleRBone;
    private Transform clavicleLBone;
    private Transform headBone;
    private Transform tailBone;


    public Transform GetTailBone()
    {
        if (tailBone == null)
        {
            tailBone = FindRecursiveBone(BONE_TAIL);
        }
        return tailBone;
    }

    public Transform GetHeadBone()
    {
        if (headBone == null)
        {
            headBone = FindRecursiveBone(BONE_HEAD);
        }
        return headBone;
    }

    public Transform GetClavicledRBone()
    {
        if (clavicleRBone == null)
        {
            clavicleRBone = FindRecursiveBone(BONE_CLAVICLE_R_NAME);
        }
        return clavicleRBone;
    }

    public Transform GetClavicledLBone()
    {
        if (clavicleLBone == null)
        {
            clavicleLBone = FindRecursiveBone(BONE_CLAVICLE_L_NAME);
        }
        return clavicleLBone;
    }

    public Transform DetermineHitSide(Vector3 hitPoint, Transform target)
    {
        Transform headBone = GetHeadBone();
        Transform rightBone = GetClavicledRBone();
        Transform leftBone = GetClavicledLBone();
        Transform tailBone = GetTailBone();

        if (rightBone == null || leftBone == null || tailBone == null || headBone == null)
        {
            Debug.LogWarning("Required bones not found!");
            return null;
        }


        Vector3 characterForward = target.forward;
        Vector3 hitDirection = hitPoint - target.position;
        hitDirection.Normalize();

   
        float forwardDotProduct = Vector3.Dot(characterForward, hitDirection);

    
        Vector3 characterRight = target.right;
        float rightDotProduct = Vector3.Dot(characterRight, hitDirection);

        
        if (Mathf.Abs(rightDotProduct) >= 0.2f)
        {
            Transform selectedBone = (rightDotProduct > 0) ? rightBone : leftBone;
            Debug.Log($"Raycaster hit calculated: Hit detected on {(rightDotProduct > 0 ? "Right" : "Left")} side");
            return selectedBone;
        }

        // Если удар спереди (в сторону "лица")
        if (forwardDotProduct > 0)
        {
            Debug.Log("Raycaster hit calculated: Front side (face)");
            return headBone;
        }
        // Если удар сзади (в сторону "задницы")
        else
        {
            Debug.Log("Raycaster hit calculated: Back side (buttocks)");
            return tailBone;
        }
    }


    public void SetPositionArrowRandomlyNearCenter(GameObject attach, Transform targetBone, Vector3 hitDirection, bool isFrontOrBackHit = false)
    {
        Quaternion originalRotation = attach.transform.rotation;

  
        float baseLength = 0.1f;
        float lengthVariation = 0.1f;

  
        if (isFrontOrBackHit)
        {
            lengthVariation = 0.05f;
        }


        float arrowLength = baseLength + (Random.Range(-lengthVariation, lengthVariation));

        Vector3 arrowCenterPosition = targetBone.position - (hitDirection * (arrowLength * 0.5f));
        attach.transform.position = arrowCenterPosition;
        attach.transform.SetParent(targetBone);
        attach.transform.forward = hitDirection;
        attach.transform.rotation = originalRotation;
    }














}
