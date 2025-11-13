using UnityEngine;

public class ValidatePosition : ClientPacket
{

    public ValidatePosition(float x , float y , float z) : base((byte)GameInterludeClientPacketType.ValidatePosition)
    {
        var location = VectorUtils.ConvertPosUnityToL2j(new Vector3(x, y, z));
        WriteI((int)location.x);
        WriteI((int)location.y);
        WriteI((int)location.z);

        WriteI(0);// No real need to validate heading.
        WriteI(0);// vehicle id

        BuildPacket();
    }
}
