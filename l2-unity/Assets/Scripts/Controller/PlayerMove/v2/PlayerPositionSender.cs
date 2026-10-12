using UnityEngine;

public class PlayerPositionSender
{
    // ALineagePlayerController+0xA00, period dbl_1F44120
    public const float Interval = 1.2f;

    private float _timer;

    public void TickWhileMoving(Vector3 playerPosition, int heading, float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer > 0f)
            return;

        _timer += Interval;
        SendData(playerPosition, heading);
    }

    public void SendServerArrivedPosition(Vector3 playerPosition)
    {
        int heading = 0;
        if (PlayerController.Instance != null)
            heading = ToL2Heading(PlayerController.Instance.transform.eulerAngles.y);
        SendData(playerPosition, heading);
    }

    public void SendData(Vector3 playerPosition, int heading)
    {
        if (IncomingPacketActions.Game == null)
            return;
        IncomingPacketActions.Game.Send(new ValidatePositionCommand(playerPosition.x, playerPosition.y, playerPosition.z, heading));
    }

    public static int ToL2Heading(float yawDegrees)
    {
        int heading = Mathf.RoundToInt(yawDegrees / 360f * 65536f) % 65536;
        if (heading < 0)
            heading += 65536;
        return heading;
    }
}
