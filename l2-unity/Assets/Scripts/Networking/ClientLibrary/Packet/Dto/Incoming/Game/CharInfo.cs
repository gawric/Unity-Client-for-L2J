
//using System;
//using static UnityEngine.ProBuilder.AutoUnwrapSettings;

//public class CharInfo : IWireDto
//{
//    public EntityIdentity Identity { get; private set; }
//    public PlayerStatus Status { get; private set; }
//    public Stats Stats { get; private set; }
//    public PlayerAppearance Appearance { get; private set; }
//    public EntityActionInfo EntityActionInfo { get; set; }

//    public CharInfo(byte[] d) : base(d)
//    {
//        Identity = new EntityIdentity();
//        Status = new PlayerStatus();
//        Stats = new Stats();
//        Appearance = new PlayerAppearance();
//        EntityActionInfo = new EntityActionInfo();
//        Parse();
//    }

//    public void ReadFrom(PacketReader reader)
//    {
//        try
//        {
//            Identity.SetPosZ(reader.ReadI() / 52.5f);
//            Identity.SetPosX(reader.ReadI() / 52.5f);
//            Identity.SetPosY(reader.ReadI() / 52.5f);

//            reader.ReadI(); // boat info

//            Identity.Id = reader.ReadI();
//            Identity.Name = reader.ReadS();
//            Appearance.Race = (byte)reader.ReadI();
//            Appearance.Sex = (byte)reader.ReadI();
//            Identity.PlayerClass = (byte)reader.ReadI();

//            reader.ReadI(); //HairAll?
//            reader.ReadI(); //Head
//            Appearance.RHand = reader.ReadI();
//            Appearance.LHand = reader.ReadI();
//            Appearance.Gloves = reader.ReadI();
//            Appearance.Chest = reader.ReadI();
//            Appearance.Legs = reader.ReadI();
//            Appearance.Feet = reader.ReadI();
//            reader.ReadI(); //Cloak
//            Appearance.RHand = reader.ReadI();
//            reader.ReadI(); //Hair
//            reader.ReadI(); //Face

//            reader.ReadI();
//            reader.ReadI();
//            reader.ReadI(); //rhand augmentationid
//            reader.ReadI();
//            reader.ReadI();
//            reader.ReadI();
//            reader.ReadI();
//            reader.ReadI();
//            reader.ReadI();
//            reader.ReadI(); //lhand augmentationid
//            reader.ReadI();
//            reader.ReadI();

//            Identity.PvpFlag = reader.ReadI(); // pvp flag
//            Stats.Karma = reader.ReadI(); // karma

//            Stats.MAtkSpd = reader.ReadI();
//            Stats.PAtkSpd = reader.ReadI();

//            Identity.PvpFlag = reader.ReadI(); // pvp flag
//            Stats.Karma = reader.ReadI(); // karma

//            Stats.Speed = reader.ReadI();
//            Stats.WalkSpeed = reader.ReadI();
//            reader.ReadI(); // swim speed
//            reader.ReadI(); // swim speed
//            reader.ReadI(); //RunSpeed
//            reader.ReadI(); //WalkSpeed
//            reader.ReadI(); //RunSpeed
//            reader.ReadI(); //WalkSpeed

//            Stats.MoveSpeedMultiplier = (float)reader.ReadD();
//            Stats.AttackSpeedMultiplier = (float)reader.ReadD();

//            Appearance.CollisionRadius = (float)reader.ReadD() / 52.5f;
//            Appearance.CollisionHeight = (float)reader.ReadD() / 52.5f;

//            Appearance.HairStyle = (byte)reader.ReadI();
//            Appearance.HairColor = (byte)reader.ReadI();
//            Appearance.Face = (byte)reader.ReadI();

//            Identity.Title = reader.ReadS();

//            reader.ReadI(); //ClanId
//            reader.ReadI(); //ClanCrest
//            reader.ReadI(); //Ally
//            reader.ReadI(); //AllyCrest

//            reader.ReadI();

//            EntityActionInfo.Sitting = reader.ReadB() == 0;
//            EntityActionInfo.Running = reader.ReadB() == 1;
//            EntityActionInfo.InCombat = reader.ReadB() == 1;
//            EntityActionInfo.AlikeDead = reader.ReadB() == 1;
//            EntityActionInfo.Invisible = reader.ReadB() == 1;

//            reader.ReadB(); //MountType
//            reader.ReadB(); //OperateType

//            int cubicCount = reader.ReadH();
//            for (int i = 0; i < cubicCount; i++)
//            {
//                reader.ReadH(); //cubic id
//            }

//            reader.ReadB(); //IsInPartyMatchRoom
//            reader.ReadI(); //AbnormalEffect
//            reader.ReadB(); //Reco left
//            reader.ReadH(); //Reco have

//            Identity.PlayerClass = (byte)reader.ReadI();

//            Stats.MaxCp = reader.ReadI();
//            Status.Cp = reader.ReadI();

//            reader.ReadB(); //EnchantEffect
//            reader.ReadB(); //TeamId (Event?)
//            reader.ReadI(); //Clan Crest LongId

//            reader.ReadB(); //IsNoble
//            reader.ReadB(); //Hero/GM Aura
//            reader.ReadB(); //IsFishing

//            reader.ReadI(); // Fishing Loc X
//            reader.ReadI(); // Fishing Loc Y
//            reader.ReadI(); // Fishing Loc Z

//            Appearance.ServerNameColor = reader.ReadI(); //NameColor

//            Identity.Heading = reader.ReadI();

//            reader.ReadI(); //Pledge class
//            reader.ReadI(); //Pledge type

//            Appearance.ServerTitleColor = reader.ReadI(); //Title Color

//            reader.ReadI(); //Cursed weapon

//            Identity.IsMage = CharacterClassParser.IsMage((CharacterClass)Identity.PlayerClass);

//            Stats.Speed = (int)(Stats.MoveSpeedMultiplier > 0 ? Stats.Speed * Stats.MoveSpeedMultiplier : Stats.Speed);
//            Stats.WalkRealSpeed = (int)(Stats.MoveSpeedMultiplier > 0 ? Stats.WalkSpeed * Stats.MoveSpeedMultiplier : Stats.WalkSpeed);
//            // Stats.PAtkSpd = (int)(Stats.AttackSpeedMultiplier > 0 ? Stats.PAtkSpd * Stats.AttackSpeedMultiplier : Stats.PAtkSpd);

//            Stats.AttackRange = reader.ReadI() / 52.5f;

//            //Debug.LogWarning(ToString());
//        }
//        catch (Exception e)
//        {
//            //Debug.LogError(e);
//        }
//    }

//    public override string ToString()
//    {
//        return $"UserInfoPacket: {{ " +
//               $"Identity: {{ ID: {Identity.Id},, Position: {Identity.Position},\n " +
//               $"  Name: {Identity.Name}\n" +
//               $"  NameColor: {Appearance.ServerNameColor}\n" +
//               $"  Title: {Identity.Title}\n" +
//               $"  TitleColor: {Appearance.ServerTitleColor}\n" +
//               $"  FlagTime: {Identity.PvpFlag}\n" +
//               $"Class: {Identity.PlayerClass}, IsMage: {Identity.IsMage}, Heading: {Identity.Heading} }}, " +
//               $"Status: {{ CP: {Status.Cp} }}, " +
//               $"Stats: {{ Karma: {Stats.Karma}, PAtkSpd: {Stats.PAtkSpd}, MAtkSpd: {Stats.MAtkSpd}, RunSpeed: {Stats.Speed}, " +
//               $"WalkSpeed: {Stats.WalkSpeed}, MoveSpeedMultiplier: {Stats.MoveSpeedMultiplier}, AttackSpeedMultiplier: {Stats.AttackSpeedMultiplier}, " +
//               $"MaxCp: {Stats.MaxCp}, AttackRange: {Stats.AttackRange} }}, " +
//               $"Appearance: {{ Race: {Appearance.Race}, Sex: {Appearance.Sex}, HairStyle: {Appearance.HairStyle}, HairColor: {Appearance.HairColor}, " +
//               $"Face: {Appearance.Face}, CollisionRadius: {Appearance.CollisionRadius}, CollisionHeight: {Appearance.CollisionHeight}, " +
//               $"RHand: {Appearance.RHand}, LHand: {Appearance.LHand}, Gloves: {Appearance.Gloves}, Chest: {Appearance.Chest}, " +
//               $"Legs: {Appearance.Legs}, Feet: {Appearance.Feet} }}, " +
//               $"Running: {EntityActionInfo.Running}, Sitting: {EntityActionInfo.Sitting}, InCombat: {EntityActionInfo.InCombat}, AlikeDead: {EntityActionInfo.AlikeDead}, Invisible: {EntityActionInfo.Invisible} " +
//               $"}}";
//    }
//}
