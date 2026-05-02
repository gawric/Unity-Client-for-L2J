using UnityEngine;
using System;

public class RotationService : MonoBehaviour , IRotation
{
    public static IRotation Instance { get; private set; }

    [SerializeField] private float angleThreshold = 1.0f; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public void RotateTowards(Transform actor, Vector3 targetPoint, Action onComplete , float speed = 10)
    {
        Vector3 direction = (targetPoint - actor.position).normalized;
        direction.y = 0;
        if (direction == Vector3.zero)
        {
            onComplete?.Invoke();
            Debug.Log("RotationService: Rotating exit 1 " + targetPoint);
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angleDiff = Quaternion.Angle(actor.rotation, targetRotation);

        // Если разница уже меньше порога - не крутим, шлем Action
        if (angleDiff <= angleThreshold)
        {
            actor.rotation = targetRotation; // Доворачиваем точно
            onComplete?.Invoke();
            Debug.Log("RotationService: Rotating exit 2 " + targetPoint);
        }
        else
        {
            Debug.Log("RotationService: Rotating towards " + targetPoint);
            actor.rotation = Quaternion.Slerp(actor.rotation, targetRotation, Time.deltaTime * speed);
        }
    }
}