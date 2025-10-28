public class ErrorType
{
    public static readonly int REASON_CREATION_FAILED = 0x00; // "Your character creation has failed."
    public static readonly int REASON_TOO_MANY_CHARACTERS = 0x01; // "You cannot create another character. Please delete the existing character and try again." Removes all settings that were selected (race, class, etc).
    public static readonly int REASON_NAME_ALREADY_EXISTS = 0x02; // "This name already exists."
    public static readonly int REASON_16_ENG_CHARS = 0x03; // "Your title cannot exceed 16 characters in length. Please try again."
    public static readonly int REASON_INCORRECT_NAME = 0x04; // "Incorrect name. Please try again."
    public static readonly int REASON_CREATE_NOT_ALLOWED = 0x05; // "Characters cannot be created from this server."
    public static readonly int REASON_CHOOSE_ANOTHER_SVR = 0x06; // "Unable to create character. You are unable to create a new character on the selected server. A restriction is in place which restricts users from creating characters on different servers where no previous character exists. Please
                                                              // choose another server."

    public static string  GetErrorText(int id)
    {

        if (id == REASON_CREATION_FAILED)
        {
            return "Your character creation has failed.";
        }
        else if(id == REASON_TOO_MANY_CHARACTERS)
        {
            return "You cannot create another character. Please delete the existing character and try again. Removes all settings that were selected(race, class, etc)";
        }
        else if (id == REASON_NAME_ALREADY_EXISTS)
        {
            return "This name already exists.";
        }
        else if (id == REASON_16_ENG_CHARS)
        {
            return "Your title cannot exceed 16 characters in length. Please try again.";
        }
        else if (id == REASON_INCORRECT_NAME)
        {
            return "Incorrect name. Please try again.";
        }
        else if (id == REASON_CREATE_NOT_ALLOWED)
        {
            return "Characters cannot be created from this server.";
        }
        else if (id == REASON_CHOOSE_ANOTHER_SVR)
        {
            return "Unable to create character. You are unable to create a new character on the selected server.";
        }

        return "Unknown error";
    }
}
