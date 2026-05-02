using UnityEngine;

public class TrackedSword
{
    public Transform basePt;
    public Transform tipPt;
    public float range;
    public Transform target;
    public TrackedSword(Transform basePt , Transform tipPt, Transform target , float range)
    {
        this.basePt = basePt;
        this.tipPt = tipPt;
        this.range = range;
        this.target = target;
    }

    public Vector3 PositionBasePt()
    {
        return basePt.position;
    }

    public Vector3 PositiontipPt()
    {
        return tipPt.position;
    }
}
