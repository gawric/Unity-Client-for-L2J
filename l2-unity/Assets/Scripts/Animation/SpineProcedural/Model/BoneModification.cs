using UnityEngine;


public class BoneModification
{
    public Vector3 RotationOffset; 
    public Vector3 PositionOffset; 
    public float Weight = 1.0f;    

    public BoneModification(Vector3 rotation, Vector3 position = default, float weight = 1.0f)
    {
        RotationOffset = rotation;
        PositionOffset = position;
        Weight = weight;
    }
}