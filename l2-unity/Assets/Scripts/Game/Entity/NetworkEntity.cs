public class NetworkEntity : Entity
{
    public override float UpdateMAtkSpeed(int mAtkSpd)
    {
        float converted = base.UpdateMAtkSpeed(mAtkSpd);
        _networkAnimationReceive.SetMAtkSpd(converted);

        return converted;
    }

    public override float UpdatePAtkSpeed(int pAtkSpd)
    {
        float converted = base.UpdatePAtkSpeed(pAtkSpd);
        _networkAnimationReceive.SetPAtkSpd(converted);

        return converted;
    }

    public override float UpdateRunSpeed(float speed)
    {
        float converted = base.UpdateRunSpeed(speed);
        _networkAnimationReceive.SetRunSpeed(converted);
        return converted;
    }
    public override float UpdateWalkSpeed(float speed)
    {
        float converted = base.UpdateWalkSpeed(speed);
        _networkAnimationReceive.SetWalkSpeed(converted);
        return converted;
    }
}