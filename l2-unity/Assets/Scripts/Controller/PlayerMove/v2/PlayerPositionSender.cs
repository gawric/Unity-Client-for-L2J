using UnityEngine;

public class PlayerPositionSender 
{
    private int _currentCount = 0;
    public void SendServerValidPositionEvery1ms(Vector3 playerPosition , int count)
    {

        if (_currentCount != count)
        {
            //TimeUtils.PrintFullTime("Отправляем позицию серверу count" + count);
            _currentCount = count;
            SendData(playerPosition);
        }
    }

    public void SendServerArrivedPosition(Vector3 playerPosition)
    {
        //TimeUtils.PrintFullTime("Отправляем позицию серверу StopMove ");
        SendData(playerPosition);
    }



    public void SendData(Vector3 playerPosition)
    {
        ValidatePosition sendPaket = CreatorPacketsUser.CreateValidatePosition(playerPosition.x, playerPosition.y, playerPosition.z);
        bool enable = GameClient.Instance.IsCryptEnabled();
        SendGameDataQueue.Instance().AddItem(sendPaket, enable, enable);
        //Debug.Log("ValidatePosition  отправили данные для синхронизации с сервером");
    }
}
