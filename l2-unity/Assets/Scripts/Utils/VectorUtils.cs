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
        return distance * 0.0165f;
    }

    public static float ConvertL2jDataOffset(float height)
    {
        return height / 0.5f;
    }

    public static float ConvertL2jDataOffsetRadius(float height)
    {
        return height;
    }
    //Высисляет заданная точка движения находится за спиной игрока или нет
    public static bool IsTargetBehindPlayer(Vector3 targetPosition, Transform playerTransform)
    {
        Vector3 playerForward = playerTransform.forward;
        Vector3 toTarget = (targetPosition - playerTransform.position).normalized;
        float angle = Vector3.Angle(playerForward, toTarget);
        return angle > 90f;
    }

    public static bool IsFacingAttacker(Transform transform , Vector3 attackerPosition , float angleThreshold)
    {
        Vector3 directionToAttacker = (attackerPosition - transform.position).normalized;
        Vector3 forwardDirection = transform.forward;
        float angle = Vector3.Angle(forwardDirection, directionToAttacker);

        return angle < angleThreshold;
    }

    //graph setting
    private static float _verticalVelocity = 0;
    private static float _gravity = 28;
    public static Vector3 ApplyGravityGround(Vector3 position, float elapsedTime)
    {

        RaycastHit hit;
        bool isGrounded = Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity, World.Instance.GroundMask);

        if (isGrounded)
        {
            // Если на земле, устанавливаем Y на уровень земли
            position.y = hit.point.y;
            _verticalVelocity = 0;
        }
        else
        {
            // Если не на земле, применяем гравитацию
            _verticalVelocity -= _gravity * elapsedTime;
            position.y += _verticalVelocity * elapsedTime;
        }

        // Дополнительно проверяем, не опустился ли игрок ниже уровня земли
        if (position.y < hit.point.y)
        {
            position.y = hit.point.y;
            _verticalVelocity = 0;
        }

        return position;
    }




    //Вычисляет сколько процентов игрок повернулся к заданой цели. Процент вычисляет между двумя векторами
    public static float InFaceProcent(Vector3 target, Transform transform , float maxAngle)
    {
        // Если разница между начальным углом и конечным углом меньше 20 градусов, то считаем что объект смотрит в сторону цели
        if (maxAngle <= 30) return 100;

        Vector3 targetVect = new Vector3(target.x, 0, target.z);
        Vector3 vectr = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 directionToTarget = (targetVect - vectr).normalized;

        
        // Угол между текущим направлением объекта и направлением к цели
        float angle = Vector3.Angle(transform.forward, directionToTarget);
        if(angle <= 0.5f) return 100;
       // Debug.Log("Вектор поворта угла " + angle);
        //Debug.Log("Вектор поворта угла maxAngle " + maxAngle);

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
            float angle = Vector3.Angle(heading, attacker.forward); // Вычисляем угол между heading и направлением target
            float particleHeight = height; // Высота частиц
            Vector3 direction = Quaternion.Euler(0, angle, 0) * attacker.forward; // Вычисляем новое направление с учетом угла

            // Возвращаем новую позицию, смещенную от позиции attacker
            return attacker.position + direction * raduis + Vector3.up * particleHeight;
    }

    public static Vector3 GetCollision(Vector3 attacker, Transform target)
    {
        // Get target's collision height
        float particleHeight = target.GetComponent<Entity>().Appearance.CollisionHeight * 1.25f;

        // Calculate distance to target
        float distanceToAttacker = Vector3.Distance(attacker, target.position);

        // If target is very close, aim for the center
        if (distanceToAttacker < 5.0f) // Adjust this threshold as needed
        {
            // Just return the center of the target at the collision height
            return target.position + Vector3.up * particleHeight;
        }

        // For normal distances, use the original calculation
        var heading = attacker - target.position;
        float angle = Vector3.Angle(heading, target.forward);
        float particleHeight1 = target.GetComponent<Entity>().Appearance.CollisionHeight * 1.25f;
        Vector3 direction = Quaternion.Euler(0, angle, 0) * target.forward;
        return target.position + direction * 0.15f + Vector3.up * particleHeight1;
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
    //use npc heading
    public static float HeadingToUnityQuaternionForNpc(float heading)
    {
        // Преобразуем heading из диапазона [0, 65535] в угол в градусах [0, 360]
        float angleInDegrees = (heading / 182.04444444444444444444444444444f) % 360;
        return angleInDegrees;

    }


    public static Vector3 CalcHitDirection(Vector3 currentPosition, Vector3 startPosition)
    {
        return (currentPosition - startPosition).normalized;
    }


}
