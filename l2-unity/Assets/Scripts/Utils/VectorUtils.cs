using UnityEngine;

public class VectorUtils : MonoBehaviour {
    public static Vector3 ConvertPosToUnity(Vector3 ueVector) {
        return new Vector3(ueVector.y, ueVector.z, ueVector.x) * (1f / 52.5f);
    }

    public static Vector3 ConvertPosUnityToL2j(Vector3 ueVector)
    {
        Vector3 convert =   new Vector3(ueVector.y, ueVector.x, ueVector.z) / (1f / 52.5f);
        return new Vector3(convert.z, convert.y, convert.x);
    }

    public static Vector3 ConvertRotToUnity(Vector3 ueRot) {
        return new Vector3(
                            -360.00f * ueRot.x / 65536,
                            360.00f * ueRot.y / 65536,
                            -360.00f * ueRot.z / 65536
                        );
    }
    //default 0.0191 unit to metr

    public static float ConvertL2jDistance(float distance)
    {
        return distance * 0.0190f;
    }

    public static float ConvertL2jDataOffset(float height)
    {
        return height / 0.5f;
    }

    public static float ConvertL2jDataOffsetRadius(float height)
    {
        return height;
    }
    //��������� �������� ����� �������� ��������� �� ������ ������ ��� ���
    public static bool IsTargetBehindPlayer(Vector3 targetPosition, Transform playerTransform)
    {
        Vector3 playerForward = playerTransform.forward;
        Vector3 toTarget = (targetPosition - playerTransform.position).normalized;
        float angle = Vector3.Angle(playerForward, toTarget);
        return angle > 90f;
    }


    //��������� ������� ��������� ����� ���������� � ������� ����. ������� ��������� ����� ����� ���������
    public static float InFaceProcent(Vector3 target, Transform transform , float maxAngle)
    {
        // ���� ������� ����� ��������� ����� � �������� ����� ������ 20 ��������, �� ������� ��� ������ ������� � ������� ����
        if (maxAngle <= 30) return 100;

        Vector3 targetVect = new Vector3(target.x, 0, target.z);
        Vector3 vectr = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 directionToTarget = (targetVect - vectr).normalized;

        
        // ���� ����� ������� ������������ ������� � ������������ � ����
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        if(angle <= 0.5f) return 100;
       // Debug.Log("������ ������� ���� " + angle);
        //Debug.Log("������ ������� ���� maxAngle " + maxAngle);

        float percentage = Mathf.Clamp01(1 - (angle / maxAngle));

        return percentage * 100;
    }

    public static float GetMaxAngle(Vector3 target , Transform transform)
    {
        Vector3 targetVect = new Vector3(target.x, 0, target.z);
        Vector3 vectr = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 directionToTarget = (targetVect - vectr).normalized;
        return Vector3.Angle(transform.forward, directionToTarget);
    }


    public static Vector3 GetCollisionEffect(Transform attacker, Transform target , float height , float raduis)
    {
            var heading = target.position - attacker.position;
            float angle = Vector3.Angle(heading, attacker.forward); // ��������� ���� ����� heading � ������������ target
            float particleHeight = height; // ������ ������
            Vector3 direction = Quaternion.Euler(0, angle, 0) * attacker.forward; // ��������� ����� ����������� � ������ ����

            // ���������� ����� �������, ��������� �� ������� attacker
            return attacker.position + direction * raduis + Vector3.up * particleHeight;
    }

    public static Vector3 ConvertToUnityUnscaled(Vector3 ueVector) {
        return new Vector3(ueVector.y, ueVector.z, ueVector.x);
    }

    public static Vector3 ScaleToUnity(Vector3 ueVector) {
        return ueVector * (1f / 52.5f);
    }

    public static Vector2 RotateVector2(Vector2 vector, float angle) {
        float radians = angle * Mathf.Deg2Rad;
        return RotateVector2Rad(vector, radians);
    }

    public static Vector2 RotateVector2Rad(Vector2 vector, float radians) {
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        float newX = vector.x * cos - vector.y * sin;
        float newY = vector.x * sin + vector.y * cos;

        return new Vector2(newX, newY);
    }

    public static Vector3 FloorToNearest(Vector3 vector, float step) {
        return new Vector3(NumberUtils.FloorToNearest(vector.x, step),
            NumberUtils.FloorToNearest(vector.y, step),
            NumberUtils.FloorToNearest(vector.z, step));
    }

    public static Vector3 To2D(Vector3 pos) {
        return new Vector3(pos.x, 0, pos.z);
    }

    public static float Distance2D(Vector3 from, Vector3 to) {
        return Vector3.Distance(To2D(from), To2D(to));
    }

    public static bool IsVectorZero2D(Vector3 vector) {
        return vector.x == 0 && vector.z == 0;
    }

    public static float CalculateMoveDirectionAngle(Vector3 from, Vector3 to) {
        // Calculate the direction vector (destination - current position)
        float directionX = to.x - from.x;
        float directionZ = to.z - from.z;

        return CalculateMoveDirectionAngle(directionX, directionZ);
    }

    public static float CalculateMoveDirectionAngle(float directionX, float directionZ) {
        float angle = Mathf.Atan2(directionX, directionZ) * Mathf.Rad2Deg;
        return angle;
    }
}
