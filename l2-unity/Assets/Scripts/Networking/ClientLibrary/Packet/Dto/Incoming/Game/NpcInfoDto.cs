using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class NpcInfoDto : IWireDto
{
    //private EntityIdentity _identity;
    //private NpcStatusInterlude _status;
    //private PlayerStats _stats;
    public EntityIdentity Identity { get; private set; }

    public Appearance Appearance { get; private set; }


    public NpcStatusInterlude Status { get; private set; }
    public Stats Stats { get; private set; }



    public NpcInfoDto()
    {
        Identity = new EntityIdentity();
        Status = new NpcStatusInterlude();
        Stats = new PlayerStats();
        Appearance = new Appearance();
    }

    public void ReadFrom(PacketReader reader)
    {

        //set Default need change 
        Stats.Level = 1;
        Status.SetHp(100);
        Stats.MaxHp = 100;

        //Debug.Log("пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ NPCINFOOO");
        int objectId = reader.ReadI();
        Identity.Id = objectId;
        Identity.NpcId = reader.ReadI() - 1000000; // npctype id (-1000000)

        if(Identity.NpcId == 31775)
        {
            Debug.Log(" object NpcInfo 1 " + objectId);
        }

        Identity.SetHideHp(Identity.NpcId);
        int isAttackable = reader.ReadI();
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();
        Identity.SetL2jPos(x, y, z);
        float heading = reader.ReadI();

        Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
        Identity.Heading = Quaternion.Euler(0, Identity.OrigHeading, 0);

        int empty = reader.ReadI();

        if (Identity.NpcId == 31775)
        {
            Debug.Log(" object NpcInfo  object 2 " + objectId);
        }

        Stats.MAtkSpd = reader.ReadI();
        Stats.PAtkSpd= reader.ReadI();
        //int runSpeed = reader.ReadI();
        Stats.BaseRunSpeed = reader.ReadI();
        Stats.BaseWalkingSpeed = reader.ReadI();
        int swimRunSpd = reader.ReadI();
        int swimWalkSpd = reader.ReadI();
        int flyRunSpd = reader.ReadI();
        int flyWalkSpd = reader.ReadI();
        //Stats.WalkSpeed = reader.ReadI();
        int flyRunSpd2 = reader.ReadI();
        int flyWalkSpd2 = reader.ReadI();
        double moveMultiplier = reader.ReadD();

        Stats.WalkRealSpeed = GetRealSpeed(Stats.BaseWalkingSpeed, (float)moveMultiplier);
        Stats.RunRealSpeed = GetRealSpeed(Stats.BaseRunSpeed, (float)moveMultiplier);

        double atkSpeedMultiplier = reader.ReadD();
        //Stats.PAtkRealSpeed = GetRealSpeed(Stats.PAtkSpd, (float)atkSpeedMultiplier);
        Stats.PAtkRealSpeed = GetRealSpeed(Stats.PAtkSpd, 1);
        double collisionRadius = reader.ReadD();
        double collisionHeight = reader.ReadD();
        int _rhand = reader.ReadI();
        int _chest = reader.ReadI();
        int _lhand = reader.ReadI();
        byte empty2 = reader.ReadB(); // name above char 1=true ... ??
                               // _info.Appearance.Running = isRunning == 1;
        Identity.IsRunning = reader.ReadB() == 1;
        //byte isRunning = reader.ReadB();
        byte isInCombat = reader.ReadB();
        byte sAlikeDead = reader.ReadB();
        byte isSummoned = reader.ReadB();// invisible ?? 0=false 1=true 2=summoned (only works if model has a summon animation)
        Identity.Name = reader.ReadOtherS();
        Identity.Title = reader.ReadOtherS();
        reader.ReadI();// Title color 0=client default
        reader.ReadI(); // pvp flag
        reader.ReadI(); // karma
        int abnormalVisualEffects = reader.ReadI(); //_npc.isInvisible() ?
        int clanId = reader.ReadI();
        int clanCrest = reader.ReadI();
        int allyId = reader.ReadI();
        int allyCrest = reader.ReadI();
        byte insideZone = reader.ReadB(); //(_npc.isInsideZone(ZoneId.WATER) ? 1 : _npc.isFlying() ? 2 : 0); // C2
        byte teamId = reader.ReadB();
        double _collisionRadius = reader.ReadD();
        double _collisionHeight = reader.ReadD();
        Appearance.CollisionHeight = (float)_collisionHeight;
        Appearance.CollisionRadius = (float)_collisionRadius;
        int _enchantEffect = reader.ReadI();
        int isFlying = reader.ReadI();

        if (Identity.NpcId == 31775)
        {
            Debug.Log(" object NpcInfo  object 3 " + objectId);
        }

        //Debug.Log("пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅ NPCINFOOO NPCID " + Identity.NpcId);
    }

    private float GetRealSpeed(int baseSpeed, float speedMultiplier)
    {
        return baseSpeed * speedMultiplier;
    }

}
