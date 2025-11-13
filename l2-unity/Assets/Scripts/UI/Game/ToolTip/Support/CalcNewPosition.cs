using UnityEngine;

public class CalcNewPosition
{
    private const float BorderOffset = 2f;
    private const float TooltipVerticalOffset = 28f;

    /// <summary>
    /// Calculate new position based on whether the point is inside root and if force below is required
    /// </summary>
    /// <param name="newPoint">Original position to calculate from</param>
    /// <param name="offset">Offset value to use in calculations</param>
    /// <param name="heightContent">Height of content element</param>
    /// <param name="insideRoot">Whether the point is inside the root element</param>
    /// <param name="isForceBelow">Whether to force the position below the original point</param>
    /// <returns>Calculated new position</returns>
    public Vector2 GetNewPosition(Vector2 newPoint, float offset, float heightContent, bool insideRoot, bool isForceBelow)
    {
        return insideRoot
            ? CalculateInsidePosition(newPoint, offset, heightContent , isForceBelow)
            : CalculateOutsidePosition(newPoint, offset, isForceBelow);
    }

    /// <summary>
    /// Calculate position when point is inside root element
    /// </summary>
    private Vector2 CalculateInsidePosition(Vector2 point, float offset, float heightContent, bool forceBelow)
    {
        if (forceBelow)
        {
            float adjustedY1 = point.y + (offset + BorderOffset);
            return new Vector2(point.x, adjustedY1);
        }

        float adjustedY = point.y - heightContent - (offset + BorderOffset);
        return new Vector2(point.x, adjustedY);
    }

    /// <summary>
    /// Calculate position when point is outside root element
    /// </summary>
    private Vector2 CalculateOutsidePosition(Vector2 point, float offset, bool forceBelow)
    {
        if (forceBelow)
        {
            float adjustedY = point.y + (offset + BorderOffset);
            return new Vector2(point.x, adjustedY);
        }

        float adjustedX = point.x + (offset + BorderOffset);
        return new Vector2(adjustedX, 0);
    }

    /// <summary>
    /// Calculate highest point position with additional vertical offset
    /// </summary>
    /// <param name="newPoint">Original position to calculate from</param>
    /// <param name="elementHeight">Height of the element to consider</param>
    /// <returns>Calculated highest position</returns>
    public Vector2 HighestPoint(Vector2 newPoint, float elementHeight)
    {
        float adjustedY = newPoint.y - (elementHeight + TooltipVerticalOffset);
        return new Vector2(newPoint.x, adjustedY);
    }

    /// <summary>
    /// Get position with only horizontal offset
    /// </summary>
    public Vector2 GetHorizontalOffsetPosition(Vector2 point, float offset)
    {
        return new Vector2(point.x + offset, point.y);
    }

    /// <summary>
    /// Get position with only vertical offset
    /// </summary>
    public Vector2 GetVerticalOffsetPosition(Vector2 point, float offset)
    {
        return new Vector2(point.x, point.y + offset);
    }

    /// <summary>
    /// Get position centered horizontally around a point
    /// </summary>
    public Vector2 GetCenteredPosition(Vector2 point, float elementWidth)
    {
        float centerX = point.x - (elementWidth / 2f);
        return new Vector2(centerX, point.y);
    }
}
