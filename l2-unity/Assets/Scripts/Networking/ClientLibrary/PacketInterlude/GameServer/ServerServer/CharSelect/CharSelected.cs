using UnityEngine;


public class CharSelected : ServerPacket
{

    private PlayerInfoInterlude _info;
    public PlayerInfoInterlude PlayeInfo { get { return _info; } }
    public CharSelected(byte[] d) : base(d)
    {
        _info = new PlayerInfoInterlude();
        _info.Identity = new NetworkIdentityInterlude();
        _info.Status = new PlayerStatusInterlude();
        _info.Stats = new PlayerInterludeStats();
        _info.Appearance = new PlayerInterludeAppearance();


        Parse();
    }

    public override void Parse()
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



        _info.Identity.Name = ReadOtherS();
        Debug.Log("CharSelected: Name " + _info.Identity.Name);
        _info.Identity.Id = ReadI();
        _info.Identity.Title = ReadOtherS();
        int sessionId = ReadI();
        int clan_id = ReadI();
        int empty = ReadI();


        //java server data
        int sex = ReadI();
        int race = ReadI();

        //set default (will need to be completed)
        // _info.Appearance.Race = selectChar.Race;
        _info.Appearance.Sex = selectChar.Sex;
        _info.Appearance.Race = (int)MapClassId.GetRace(selectChar.Race);


        _info.Identity.PlayerClass = ReadI(); //classId
        int unknow = ReadI();// active ??
        int x = ReadI();
        int y =  ReadI();
        int z = ReadI();
        _info.Identity.SetL2jPos(x, y, z);

        _info.Status.SetHp(ReadD());
        _info.Status.SetMp(ReadD());
        _info.Status.Cp = ReadI();
        _info.Stats.Exp = ReadOtherL();
        _info.Stats.Level = ReadI();
        _info.Stats.MaxMp = selectChar.MaxMp;
        _info.Stats.MaxHp = selectChar.MaxHp;
        _info.Stats.MaxExp = LevelServer.GetExp(_info.Stats.Level + 1);

        _info.Stats.Karma = ReadI();
        _info.Stats.PkKills = ReadI();

        _info.Stats.Int = ReadI();
        _info.Stats.Str = ReadI();
        _info.Stats.Con = ReadI();
        _info.Stats.Men = ReadI();
        _info.Stats.Dex = ReadI();
        _info.Stats.Wit = ReadI();

        for (int i = 0; i < 30; i++)
        {
            ReadI();
        }
        int empty1 = ReadI();
        int empty2 = ReadI();
        _info.Identity.ResetTick = ReadI(); // "reset" on 24th hour
        
        int empty3 = ReadI();
        int classId2 = ReadI();

        int empty31 = ReadI();
        int empty4 = ReadI();
        int empty5 = ReadI();
        int empty6= ReadI();
        int empty7 = ReadI();
        int empty8 = ReadI();
        int empty9 = ReadI();
        int empty10 = ReadI();
        int empty11 = ReadI();
        int empty12 = ReadI();
        int empty13 = ReadI();
        int empty14 = ReadI();

        Debug.Log("");
    }
}
