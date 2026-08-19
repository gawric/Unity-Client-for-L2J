using System.Collections.Generic;
using System.Linq;

[OutgoingCommandPacket(typeof(CharacterCreateCommand))]
public sealed class CharacterCreate : OutgoingWirePacket<CharacterCreateDto>
{
    protected override byte Opcode => (byte)GameClientPacketType.CharacterCreate;
    protected override OutgoingBuildKind BuildKind => OutgoingBuildKind.GameOverwriteOpcode;

    public static CharacterCreate FromUi(List<PlayerTemplates> templates, string className, string sex, string hairColor, string hairStyle, string face, string raceName, string name)
    {
        ClassIdTemplate classId = MapClassId.GetClassIdByName(className, raceName);
        PlayerTemplates template = templates.First(x => x._classId == classId.GetClassId());
        return new CharacterCreate(
            name,
            ConvertType.GetIntSex(sex),
            ConvertType.GetType(hairStyle),
            ConvertType.GetType(hairColor),
            ConvertType.GetType(face),
            template);
    }

    public CharacterCreate(CharacterCreateCommand command)
    {
        CharacterCreate created = FromUi(
            command.Templates, command.ClassName, command.Sex, command.HairColor,
            command.HairStyle, command.Face, command.RaceName, command.Name);
        Dto = created.Dto;
    }

    public CharacterCreate(string name, int female, int hairStyle, int hairColor, int face, PlayerTemplates template)
    {
        Dto.Name = name;
        Dto.Race = template.Race;
        Dto.Female = female;
        Dto.ClassId = template._classId;
        Dto.BaseInt = template.Base_int;
        Dto.BaseStr = template.Base_str;
        Dto.BaseCon = template.Base_con;
        Dto.BaseMen = template.Base_men;
        Dto.BaseDex = template.Base_dex;
        Dto.BaseWit = template.Base_wit;
        Dto.HairStyle = hairStyle;
        Dto.HairColor = hairColor;
        Dto.Face = face;
    }
}
