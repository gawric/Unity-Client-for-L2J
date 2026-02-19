using System;
using System.Text;
using UnityEngine;

public class RequestAuthLogin : ClientPacket
{
    // Настройте эти значения под ваш протокол:
    // Например: если у вас RSA-блок фиксированного размера TestRsaSize,
    // и в нём поля расположены последовательно, то
    // MaxAccountBytes + MaxPasswordBytes <= TestRsaSize - reservedBytes.
    private const int TestRsaSize = 113;
    private const int MaxAccountBytes = 15;   
    private const int MaxPasswordBytes = 15;

    public RequestAuthLogin(string account, string password, int responce)
        : base((byte)LoginInterludeClientPacketTYpe.RequestAuthLogin)
    {
        if(TryCreate(account, password))
        {
            byte[] accountBytes = Encoding.UTF8.GetBytes(account ?? string.Empty);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);


            byte[] testRsa = new byte[TestRsaSize];

            Array.Copy(accountBytes, 0, testRsa, 79, accountBytes.Length);
            Array.Copy(passwordBytes, 0, testRsa, 93, passwordBytes.Length);



            var rsaBlock = LoginClient.Instance.RSACrypt.EncryptRSABlockNoPaddingBoundleCastle(testRsa);


            WriteB(rsaBlock);
            WriteI(responce);
            WriteI(0);
            WriteI(0);
            WriteI(0);
            WriteI(0);
            WriteI(8);
            WriteI(0);

            //BuildPacket();
            BuildPacketExperimental(4);

        }

    }


    private bool TryCreate(string account, string password)
    {
      
        string errorMessage = "";

        byte[] accountBytes = Encoding.UTF8.GetBytes(account ?? string.Empty);
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password ?? string.Empty);

        if (accountBytes.Length > MaxAccountBytes)
        {
            errorMessage = $"Имя аккаунта слишком длинное: {accountBytes.Length} байт (максимум {MaxAccountBytes} байт).";
            // Вызов UI/логирования (по желанию). Замените на ваш способ показа ошибок пользователю:
            Debug.LogWarning(errorMessage);
            // Например, если есть менеджер UI:
            // LoginClient.Instance.ShowError(errorMessage);
            return false;
        }

        if (passwordBytes.Length > MaxPasswordBytes)
        {
            errorMessage = $"Пароль слишком длинный: {passwordBytes.Length} байт (максимум {MaxPasswordBytes} байт).";
            Debug.LogWarning(errorMessage);
            // LoginClient.Instance.ShowError(errorMessage);
            return false;
        }

  

        return true;
    }


}