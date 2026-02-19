using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LoginFailReason: byte
{
    REASON_NO_MESSAGE = 0x00,
    REASON_SYSTEM_ERROR_LOGIN_LATER = 0x01,
    REASON_USER_OR_PASS_WRONG = 0x02,
    REASON_ACCESS_FAILED_TRY_AGAIN_LATER = 0x04,
    REASON_ACCOUNT_INFO_INCORRECT_CONTACT_SUPPORT = 0x05,
    REASON_NOT_AUTHED= 0x06,
    REASON_ACCOUNT_IN_USE = 0x07,

    
}
