using UnityEngine;

/// <summary>
/// aCis Interlude CharInfo (0x03):
/// aCis_gameserver/.../serverpackets/CharInfo.java writeImpl()
/// </summary>
public class CharInfoDto : IWireDto
{
    public EntityIdentity Identity { get; private set; }
    public PlayerStatus Status { get; private set; }
    public PlayerStats Stats { get; private set; }
    public PlayerAppearance Appearance { get; private set; }
    public bool AlikeDead { get; private set; }

    public CharInfoDto()
    {
        Identity = new EntityIdentity();
        Status = new PlayerStatus();
        Stats = new PlayerStats();
        Appearance = new PlayerAppearance();
    }

    public void ReadFrom(PacketReader reader)
    {
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        Identity.SetL2jPos(x, y, z);

        reader.ReadI();

        Identity.Id = reader.ReadI();
        Identity.Name = reader.ReadOtherS();

        int race = reader.ReadI();
        Appearance.Race = (int)MapClassId.GetRace(race);
        Appearance.Sex = reader.ReadI();

        int classId = reader.ReadI();
        Appearance.BaseClass = classId;
        Identity.PlayerClass = classId;

        reader.ReadI();
        reader.ReadI();
        int rHand1 = reader.ReadI();
        Appearance.RHand = rHand1;
        Appearance.LHand = reader.ReadI();
        Appearance.Gloves = reader.ReadI();
        Appearance.Chest = reader.ReadI();
        Appearance.Legs = reader.ReadI();
        Appearance.Feet = reader.ReadI();
        int paperBackOrHair = reader.ReadI();
        int rHand2 = reader.ReadI();
        Appearance.RHand = rHand2;
        reader.ReadI();
        reader.ReadI();

        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadI();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadI();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();
        reader.ReadH();

        reader.ReadI();
        Stats.Karma = reader.ReadI();
        Stats.MAtkSpd = reader.ReadI();
        Stats.PAtkSpd = reader.ReadI();
        reader.ReadI();
        reader.ReadI();

        Stats.BaseRunSpeed = reader.ReadI();
        Stats.BaseWalkingSpeed = reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();

        float moveMultiplier = (float)reader.ReadD();
        reader.ReadD();

        Stats.Speed = Stats.BaseRunSpeed;
        Stats.WalkRealSpeed = GetRealSpeed(Stats.BaseWalkingSpeed, moveMultiplier);
        Stats.RunRealSpeed = GetRealSpeed(Stats.BaseRunSpeed, moveMultiplier);

        Appearance.CollisionRadius = (float)reader.ReadD();
        Appearance.CollisionHeight = (float)reader.ReadD();

        Appearance.HairStyle = ClampByte(reader.ReadI());
        Appearance.HairColor = ClampByte(reader.ReadI());
        Appearance.Face = ClampByte(reader.ReadI());

        Identity.Title = reader.ReadOtherS();
        Identity.ClanId = reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();

        reader.ReadB();
        bool running = reader.ReadB() == 1;
        Identity.IsRunning = running;
        Appearance.Running = running;
        reader.ReadB();
        AlikeDead = reader.ReadB() == 1;
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();

        int cubicCount = reader.ReadH();
        for (int i = 0; i < cubicCount; i++)
            reader.ReadH();

        reader.ReadB();
        reader.ReadI();
        reader.ReadB();
        reader.ReadH();

        Identity.PlayerClass = reader.ReadI();

        Stats.MaxCp = reader.ReadI();
        Status.Cp = reader.ReadI();

        reader.ReadB();
        reader.ReadB();
        reader.ReadI();
        reader.ReadB();
        reader.ReadB();
        reader.ReadB();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();

        float heading = reader.ReadI();
        Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
        Identity.Heading = Quaternion.Euler(0, Identity.OrigHeading, 0);

        reader.ReadI();
        reader.ReadI();
        reader.ReadI();
        reader.ReadI();

        ApplyStubHpFromAlikeDead();

        Debug.Log("[EntityAction:User] nick=" + Identity.Name +
            " CharInfo parse id=" + Identity.Id +
            " alikeDead=" + AlikeDead +
            " stubHp=" + Status.GetHp() +
            " maxHp=" + Stats.MaxHp +
            " cp=" + Status.Cp +
            " maxCp=" + Stats.MaxCp +
            " " + GearFlowLog.Paperdoll(Appearance) +
            " rHand1=" + rHand1 + " rHand2=" + rHand2 + " backOrHair=" + paperBackOrHair);

        if (Appearance.BaseClass != (int)BaseClass.Fighter && Appearance.BaseClass != (int)BaseClass.MMagic)
        {
            Appearance.BaseClass = CharacterClassParser.IsMage((CharacterClass)Identity.PlayerClass)
                ? (int)BaseClass.MMagic
                : (int)BaseClass.Fighter;
        }
    }

    void ApplyStubHpFromAlikeDead()
    {
        if (Stats.MaxHp <= 0)
            Stats.MaxHp = 100;

        if (AlikeDead)
            Status.SetHp(0);
        else
            Status.SetHp(Stats.MaxHp);
    }

    static float GetRealSpeed(int baseSpeed, float speedMultiplier)
    {
        if (speedMultiplier <= 0f)
            return baseSpeed;
        return baseSpeed * speedMultiplier;
    }

    static int ClampByte(int value)
    {
        if (value < 0)
            return 0;
        if (value > 255)
            return 255;
        return value;
    }
}
