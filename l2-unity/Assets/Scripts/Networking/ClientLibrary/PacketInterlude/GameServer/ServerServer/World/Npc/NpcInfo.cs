using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class NpcInfo : ServerPacket
{
    //private NetworkIdentityInterlude _identity;
    //private NpcStatusInterlude _status;
    //private PlayerInterludeStats _stats;
    public NetworkIdentityInterlude Identity { get; private set; }

    public Appearance Appearance { get; private set; }


    public NpcStatusInterlude Status { get; private set; }
    public Stats Stats { get; private set; }



    public NpcInfo(byte[] d) : base(d)
    {
        Identity = new NetworkIdentityInterlude();
        Status = new NpcStatusInterlude();
        Stats = new PlayerInterludeStats();
        Appearance = new Appearance();
        Parse();
    }

    public override void Parse()
    {

        //set Default need change 
        Stats.Level = 1;
        Status.SetHp(100);
        Stats.MaxHp = 100;

        //Debug.Log("�������� ��������� ������ NPCINFOOO");
        int objectId = ReadI();
        Identity.Id = objectId;
        Identity.NpcId = ReadI() - 1000000; // npctype id (-1000000)

        if(Identity.NpcId == 31775)
        {
            Debug.Log(" object NpcInfo 1 " + objectId);
        }

        Identity.SetHideHp(Identity.NpcId);
        int isAttackable = ReadI();
        int x = ReadI();
        int y = ReadI();
        int z = ReadI();
        Identity.SetL2jPos(x, y, z);
        float heading = ReadI();

        Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
        Identity.Heading = Quaternion.Euler(0, Identity.OrigHeading, 0);

        int empty = ReadI();

        if (Identity.NpcId == 31775)
        {
            Debug.Log(" object NpcInfo  object 2 " + objectId);
        }

        Stats.MAtkSpd = ReadI();
        Stats.PAtkSpd= ReadI();
        //int runSpeed = ReadI();
        Stats.BaseRunSpeed = ReadI();
        Stats.BaseWalkingSpeed = ReadI();
        int swimRunSpd = ReadI();
        int swimWalkSpd = ReadI();
        int flyRunSpd = ReadI();
        int flyWalkSpd = ReadI();
        //Stats.WalkSpeed = ReadI();
        int flyRunSpd2 = ReadI();
        int flyWalkSpd2 = ReadI();
        double moveMultiplier = ReadD();

        Stats.WalkRealSpeed = GetRealSpeed(Stats.BaseWalkingSpeed, (float)moveMultiplier);
        Stats.RunRealSpeed = GetRealSpeed(Stats.BaseRunSpeed, (float)moveMultiplier);

        double atkSpeedMultiplier = ReadD();
        //Stats.PAtkRealSpeed = GetRealSpeed(Stats.PAtkSpd, (float)atkSpeedMultiplier);
        Stats.PAtkRealSpeed = GetRealSpeed(Stats.PAtkSpd, 1);
        double collisionRadius = ReadD();
        double collisionHeight = ReadD();
        int _rhand = ReadI();
        int _chest = ReadI();
        int _lhand = ReadI();
        byte empty2 = ReadB(); // name above char 1=true ... ??
                               // _info.Appearance.Running = isRunning == 1;
        Identity.IsRunning = ReadB() == 1;
        //byte isRunning = ReadB();
        byte isInCombat = ReadB();
        byte sAlikeDead = ReadB();
        byte isSummoned = ReadB();// invisible ?? 0=false 1=true 2=summoned (only works if model has a summon animation)
        Identity.Name = ReadOtherS();
        Identity.Title = ReadOtherS();
        ReadI();// Title color 0=client default
        ReadI(); // pvp flag
        ReadI(); // karma
        int abnormalVisualEffects = ReadI(); //_npc.isInvisible() ?
        int clanId = ReadI();
        int clanCrest = ReadI();
        int allyId = ReadI();
        int allyCrest = ReadI();
        byte insideZone = ReadB(); //(_npc.isInsideZone(ZoneId.WATER) ? 1 : _npc.isFlying() ? 2 : 0); // C2
        byte teamId = ReadB();
        double _collisionRadius = ReadD();
        double _collisionHeight = ReadD();
        Appearance.CollisionHeight = (float)_collisionHeight;
        Appearance.CollisionRadius = (float)_collisionRadius;
        int _enchantEffect = ReadI();
        int isFlying = ReadI();

        if (Identity.NpcId == 31775)
        {
            Debug.Log(" object NpcInfo  object 3 " + objectId);
        }

        //Debug.Log("��������� ��������� ������ NPCINFOOO NPCID " + Identity.NpcId);
    }

    private float GetRealSpeed(int baseSpeed, float speedMultiplier)
    {
        return baseSpeed * speedMultiplier;
    }

}
