using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatorPacketsGameLobby 
{
    public static ProtocolVersion CreateProtocolVersion(int version)
    {
        return new ProtocolVersion(version);
    }

    public static NewCharacter CreateNewCharacter()
    {
        return new NewCharacter();
    }

    public static CharacterCreate CreateCharacter(List<PlayerTemplates> _playerTemplates  , string _className , string sex , string hairColor , string hairStyle , string face , string raceName , string name )
    {
        ClassIdTemplate temp = MapClassId.GetClassIdByName(_className , raceName);

        var female = ConvertType.GetIntSex(sex);
        var IhairStyle = ConvertType.GetType(hairStyle);
        var IhairColor = ConvertType.GetType(hairColor);
        var iFace = ConvertType.GetType(face);

        PlayerTemplates template =  GetByClassId(temp.GetClassId(), _playerTemplates);

        return new CharacterCreate(name, female, IhairStyle, IhairColor, iFace , template);
    }

    public static AuthLogin CreateAuthLogin(string account, int playKey1, int playKey2, int loginKey1, int loginKey2)
    {
        return new AuthLogin(account, playKey1, playKey2, loginKey1, loginKey2);
    }

    public static CharacterSelect CharacterSelect(int slot)
    {
        return new CharacterSelect(slot);
    }

    public static CharacterDelete RequestCharacterDelete(int slot)
    {
        return new CharacterDelete(slot);
    }

    private static PlayerTemplates GetByClassId(int classId , List<PlayerTemplates> _playerTemplates)
    {
       return  _playerTemplates.First(x => x._classId == classId);
    }

    public static EnterWorld CreateEnterWorld()
    {
        return new EnterWorld();
    }




}
