using UnityEngine;

/// <summary>
/// Full info about a player entering our visibility range - sent once per player (like NpcInfo),
/// not on every movement tick. Position/movement updates afterwards come through separate,
/// lightweight packets (CharMoveToLocation, ValidateLocation, MoveWithDelta - already handled
/// elsewhere in this project), so this packet's size isn't a per-frame concern.
///
/// Field order mirrors org.l2jmobius.gameserver.network.serverpackets.CharInfo#writeImpl exactly.
/// Cross-checked against UserInfo.cs (the same packet family, already working for the local
/// player) for the reader helpers (ReadSh for the short-based blocks) and formulas.
/// HP/MP/CP aren't part of this packet in real L2J either - those arrive via StatusUpdate, already
/// wired generically through World._objects.
/// </summary>
public class CharInfo : ServerPacket
{
    public NetworkIdentityInterlude Identity { get; private set; }
    public PlayerStatusInterlude Status { get; private set; }
    public PlayerInterludeStats Stats { get; private set; }
    public PlayerInterludeAppearance Appearance { get; private set; }

    public CharInfo(byte[] d) : base(d)
    {
        Identity = new NetworkIdentityInterlude();
        Status = new PlayerStatusInterlude();
        Stats = new PlayerInterludeStats();
        Appearance = new PlayerInterludeAppearance();
        Parse();
    }

    public override void Parse()
    {
        int x = ReadI();
        int y = ReadI();
        int z = ReadI();
        ReadI(); // vehicleId - vehicles aren't supported

        Identity.Id = ReadI();
        Identity.SetL2jPos(x, y, z);

        Identity.Name = ReadOtherS();
        Appearance.Race = ReadI();
        Appearance.Sex = ReadI();
        Appearance.BaseClass = ReadI();

        ReadI(); // paperdoll under
        ReadI(); // paperdoll head
        Appearance.RHand = ReadI();
        Appearance.LHand = ReadI();
        Appearance.Gloves = ReadI();
        Appearance.Chest = ReadI();
        Appearance.Legs = ReadI();
        Appearance.Feet = ReadI();
        ReadI(); // paperdoll cloak
        Appearance.RHand = ReadI(); // paperdoll rhand, written twice
        ReadI(); // paperdoll hair
        ReadI(); // paperdoll hair2

        // c6 new h's - fixed-size augmentation display block, values not used here
        for (int i = 0; i < 4; i++) ReadSh();
        ReadI(); // rhand augmentation id
        for (int i = 0; i < 12; i++) ReadSh();
        ReadI(); // rhand augmentation id, written twice
        for (int i = 0; i < 4; i++) ReadSh();

        ReadI(); // pvp flag
        ReadI(); // karma
        Stats.MAtkSpd = ReadI();
        Stats.PAtkSpd = ReadI();
        ReadI(); // pvp flag, written twice
        ReadI(); // karma, written twice
        Stats.BaseRunSpeed = ReadI();
        Stats.BaseWalkingSpeed = ReadI();
        ReadI(); // swim run speed
        ReadI(); // swim walk speed
        ReadI(); // fly run speed
        ReadI(); // fly walk speed
        ReadI(); // fly run speed, written twice
        ReadI(); // fly walk speed, written twice

        double moveMultiplier = ReadD();
        double atkSpeedMultiplier = ReadD();
        Stats.WalkRealSpeed = GetRealSpeed(Stats.BaseWalkingSpeed, (float)moveMultiplier);
        Stats.RunRealSpeed = GetRealSpeed(Stats.BaseRunSpeed, (float)moveMultiplier);
        Stats.PAtkRealSpeed = GetRealSpeed(Stats.PAtkSpd, (float)atkSpeedMultiplier);

        Appearance.CollisionRadius = (float)ReadD();
        Appearance.CollisionHeight = (float)ReadD();

        Appearance.HairStyle = ReadI();
        Appearance.HairColor = ReadI();
        Appearance.Face = ReadI();
        Identity.Title = ReadOtherS();

        ReadI(); // clan id
        ReadI(); // clan crest id
        ReadI(); // ally id
        ReadI(); // ally crest id

        ReadI(); // leader rights / siege flags - always 0 for this packet per source comment

        ReadB(); // standing (!sitting) - sitting isn't tracked for other players yet
        Identity.IsRunning = ReadB() == 1;
        ReadB(); // in combat
        ReadB(); // alike dead
        ReadB(); // invisible
        ReadB(); // mount type
        ReadB(); // private store type

        int cubicCount = ReadSh();
        for (int i = 0; i < cubicCount; i++) ReadSh();

        ReadB(); // is in party match room
        ReadI(); // abnormal visual effects
        ReadB(); // recommendations left
        ReadSh(); // recommendations have

        Identity.PlayerClass = ReadI();
        Stats.MaxCp = ReadI();
        Status.Cp = ReadI();

        ReadB(); // enchant effect
        ReadB(); // team id
        ReadI(); // clan crest large id
        ReadB(); // is noble
        ReadB(); // is hero

        ReadB(); // is fishing
        ReadI(); // fish x
        ReadI(); // fish y
        ReadI(); // fish z

        ReadI(); // name color
        Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(ReadI());
        Identity.Heading = Quaternion.Euler(0, Identity.OrigHeading, 0);

        ReadI(); // pledge class
        ReadI(); // pledge type
        ReadI(); // title color
        ReadI(); // cursed weapon level
    }

    private float GetRealSpeed(int baseSpeed, float speedMultiplier)
    {
        return baseSpeed * speedMultiplier;
    }

    // PlayerInterludeStats.PAtkSpd shadows the base Stats.PAtkSpd (int) with a double.
    private float GetRealSpeed(double baseSpeed, float speedMultiplier)
    {
        return (float)baseSpeed * speedMultiplier;
    }
}
