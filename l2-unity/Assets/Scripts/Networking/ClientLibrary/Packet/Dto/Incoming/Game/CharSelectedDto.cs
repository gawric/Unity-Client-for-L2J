using UnityEngine;


public class CharSelectedDto : IWireDto
{

    private PlayerInfoInterlude _info;
    public PlayerInfoInterlude PlayeInfo { get { return _info; } }
    public CharSelectedDto()
    {
        _info = new PlayerInfoInterlude();
        _info.Identity = new EntityIdentity();
        _info.Status = new PlayerStatus();
        _info.Stats = new PlayerStats();
        _info.Appearance = new PlayerAppearance();
    }

    public void ReadFrom(PacketReader reader)
    {
        CharSelectInfoPackage selectChar = CharSelectWindow.Instance.GetSelectChar();
        //_info.Identity.Heading = 0;
        _info.Identity.Owned = true;
        _info.Stats.Speed = 50;
        _info.Stats.AttackRange = 5;
        _info.Stats.PCritical = 5;
        _info.Stats.MCritical = 5;
        _info.Stats.Sp = 0;
        _info.Appearance.HairStyle = selectChar.HairStyle;
        _info.Appearance.HairColor = selectChar.HairColor;
        // Stats end not found Appearance



        _info.Identity.Name = reader.ReadOtherS();
        Debug.Log("CharSelected: Name " + _info.Identity.Name);
        _info.Identity.Id = reader.ReadI();
        _info.Identity.Title = reader.ReadOtherS();
        int sessionId = reader.ReadI();
        int clan_id = reader.ReadI();
        int empty = reader.ReadI();


        //java server data
        int sex = reader.ReadI();
        int race = reader.ReadI();

        //set default (will need to be completed)
        // _info.Appearance.Race = selectChar.Race;
        _info.Appearance.Sex = selectChar.Sex;
        _info.Appearance.Race = (int)MapClassId.GetRace(selectChar.Race);


        _info.Identity.PlayerClass = reader.ReadI(); //classId
        int unknow = reader.ReadI();// active ??
        int x = reader.ReadI();
        int y =  reader.ReadI();
        int z = reader.ReadI();
        _info.Identity.SetL2jPos(x, y, z);

        _info.Status.SetHp(reader.ReadD());
        _info.Status.SetMp(reader.ReadD());
        _info.Status.Cp = reader.ReadI();
        _info.Stats.Exp = reader.ReadLOther();
        _info.Stats.Level = reader.ReadI();
        _info.Stats.MaxMp = (int)selectChar.MaxMp;
        _info.Stats.MaxHp = (int)selectChar.MaxHp;
        _info.Stats.MaxExp = LevelServer.GetExp(_info.Stats.Level + 1);

        _info.Stats.Karma = reader.ReadI();
        _info.Stats.PkKills = reader.ReadI();

        _info.Stats.Int = reader.ReadI();
        _info.Stats.Str = reader.ReadI();
        _info.Stats.Con = reader.ReadI();
        _info.Stats.Men = reader.ReadI();
        _info.Stats.Dex = reader.ReadI();
        _info.Stats.Wit = reader.ReadI();

        for (int i = 0; i < 30; i++)
        {
            reader.ReadI();
        }
        int empty1 = reader.ReadI();
        int empty2 = reader.ReadI();
        _info.Identity.ResetTick = reader.ReadI(); // "reset" on 24th hour
        
        int empty3 = reader.ReadI();
        int classId2 = reader.ReadI();

        int empty31 = reader.ReadI();
        int empty4 = reader.ReadI();
        int empty5 = reader.ReadI();
        int empty6= reader.ReadI();
        int empty7 = reader.ReadI();
        int empty8 = reader.ReadI();
        int empty9 = reader.ReadI();
        int empty10 = reader.ReadI();
        int empty11 = reader.ReadI();
        int empty12 = reader.ReadI();
        int empty13 = reader.ReadI();
        int empty14 = reader.ReadI();

        Debug.Log("");
    }
}
