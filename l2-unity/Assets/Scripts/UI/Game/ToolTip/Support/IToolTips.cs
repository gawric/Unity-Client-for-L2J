using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IToolTips
{
    public void NewPosition(Vector2 newPoint, float sdfig , bool forceBelow);
    public void ResetPosition(Vector2 vector2);
}
