using System;
using UnityEngine;

public interface IRotation
{
    public void RotateTowards(Transform actor, Vector3 targetPoint, Action onComplete , float speed = 10f);
}
