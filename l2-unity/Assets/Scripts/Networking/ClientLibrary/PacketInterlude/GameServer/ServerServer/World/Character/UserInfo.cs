using UnityEngine;
using static StorageVariable;

public class UserInfo : ServerPacket
{
    private PlayerInfoInterlude _info;
    public PlayerInfoInterlude PlayerInfoInterlude { get { return _info; } }



    public UserInfo(byte[] d , PlayerInfoInterlude info) : base(d)
    {
        _info = info;
        Parse();
    }

    public override void Parse()
    {
        int x = ReadI();
        int y = ReadI();
        int z = ReadI();

        //Vector3 unityPos = VectorUtils.ConvertPosToUnity(new Vector3(x,y,z));
        _info.Identity.SetL2jPos(x, y, z);
        float heading = ReadI();
        _info.Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
        _info.Identity.Heading = Quaternion.Euler(0, _info.Identity.OrigHeading, 0); 
        _info.Identity.Id = ReadI();
     
        _info.Identity.Name = ReadOtherS();
        int reace = ReadI();
        int female = ReadI();
        _info.Appearance.Sex = female;
        _info.Appearance.Race = (int)MapClassId.GetRace(reace);
        _info.Appearance.BaseClass = ReadI();
        _info.Stats.Level = ReadI();
         long exp =  ReadOtherL();
        int ost = (int)exp - (int)_info.Stats.Exp;
       // if (ost > 0) StorageVariable.getInstance().AddS1Items(new VariableItem(ost.ToString(), _info.Identity.Id));
        _info.Stats.Exp = exp;
        _info.Stats.MaxExp  = LevelServer.GetExp(_info.Stats.Level + 1);
        _info.Stats.Str = ReadI();
        _info.Stats.Dex = ReadI();
        _info.Stats.Con = ReadI();
        _info.Stats.Int = ReadI();
        _info.Stats.Wit = ReadI();
        _info.Stats.Men = ReadI();

        _info.Stats.MaxHp = ReadI();
        _info.Status.SetHp(ReadI());
        _info.Stats.MaxMp = ReadI();
        _info.Status.SetMp(ReadI());
        int sp = ReadI();
        int oldSp = (int)_info.Stats.OldSp;
        int ostSp = (int)sp - oldSp;
        //StorageVariable.getInstance().AddS2Items(new VariableItem(ostSp.ToString(), _info.Identity.Id));
        _info.Stats.OldSp = sp;
        _info.Stats.Sp = sp;
        _info.Stats.CurrWeight = ReadI();
        _info.Stats.MaxWeight = ReadI();  //the max weight that the Creature can load.
        int activeWeaponItem = ReadI(); // 20 no weapon, 40 weapon equipped

        /**
 * Returns the objectID associated to the item in the paperdoll slot
 * @param slot : int pointing out the slot
 * @return int designating the objectID
 */
        var paperTest = _info.Appearance.PaperDoll;
        _info.Appearance.PaperDoll.Obj_Under = ReadI();
        _info.Appearance.PaperDoll.Obj_Pear = ReadI();

        _info.Appearance.PaperDoll.Obj_Lear = ReadI();
        _info.Appearance.PaperDoll.Obj_Neck = ReadI();

        _info.Appearance.PaperDoll.Obj_RFinger = ReadI();
        _info.Appearance.PaperDoll.Obj_LFinger = ReadI();

        _info.Appearance.PaperDoll.Obj_Head = ReadI();
        _info.Appearance.PaperDoll.Obj_RHand = ReadI();

        _info.Appearance.PaperDoll.Obj_LHand = ReadI();
        _info.Appearance.PaperDoll.Obj_Gloves = ReadI();

        _info.Appearance.PaperDoll.Obj_Chest = ReadI();
        _info.Appearance.PaperDoll.Obj_Legs = ReadI();

        _info.Appearance.PaperDoll.Obj_Feet = ReadI();
        _info.Appearance.PaperDoll.Obj_Cloak = ReadI();

        _info.Appearance.PaperDoll.Obj_RHand = ReadI();
        _info.Appearance.PaperDoll.Obj_Hair = ReadI();

        _info.Appearance.PaperDoll.Obj_Face = ReadI();


        /**
 * Returns the ID of the item in the paperdoll slot
 * @param slot : int designating the slot
 * @return int designating the ID of the item
 */     
        _info.Appearance.PaperDoll.Item_Under = ReadI();
        _info.Appearance.PaperDoll.Item_Rear = ReadI();

        _info.Appearance.PaperDoll.Item_Lear = ReadI();
        _info.Appearance.PaperDoll.Item_Neck = ReadI();

        _info.Appearance.PaperDoll.Item_RFinger = ReadI();
        _info.Appearance.PaperDoll.Item_LFinger = ReadI();

        _info.Appearance.PaperDoll.Item_Head = ReadI();
        _info.Appearance.PaperDoll.Item_RHand = ReadI();

        _info.Appearance.PaperDoll.Item_LHand = ReadI();
        _info.Appearance.PaperDoll.Item_Gloves = ReadI();
        _info.Appearance.Gloves = _info.Appearance.PaperDoll.Item_Gloves;

        _info.Appearance.PaperDoll.Item_Chest = ReadI();
        _info.Appearance.Chest = _info.Appearance.PaperDoll.Item_Chest;

        _info.Appearance.PaperDoll.Item_Legs = ReadI();
        _info.Appearance.Legs = _info.Appearance.PaperDoll.Item_Legs;

        _info.Appearance.PaperDoll.Item_Feet = ReadI();
        _info.Appearance.Feet = _info.Appearance.PaperDoll.Item_Feet;
        _info.Appearance.PaperDoll.Item_Cloak = ReadI();

        _info.Appearance.PaperDoll.Item_RHand = ReadI();
        _info.Appearance.PaperDoll.Item_Hair = ReadI();
        _info.Appearance.PaperDoll.Item_Face = ReadI();

        _info.Appearance.RHand = _info.Appearance.PaperDoll.Item_RHand;

        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        //buffer.writeInt(_player.getInventory().getPaperdollAugmentationId(Inventory.PAPERDOLL_RHAND));
        int rhandAugmentationId = ReadI();

        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        // buffer.writeInt(_player.getInventory().getPaperdollAugmentationId(Inventory.PAPERDOLL_RHAND));
        int rhandAugmentationId2 = ReadI();
        ReadSh();
        ReadSh();
        ReadSh();
        ReadSh();
        
        _info.Stats.PAtk = ReadI();
        int atackspped = ReadI();
        _info.Stats.BasePAtkSpeed = atackspped;
        //_info.Stats.PAtkSpd = atackspped;
        // Debug.Log("PAAAAAAAAAAAATACK SPEED " + atackspped);
        _info.Stats.PAtkSpd = atackspped;
        _info.Stats.PDef = ReadI();
        _info.Stats.PEvasion = ReadI();
        // int accuracy = ReadI();
        _info.Stats.MAccuracy = ReadI();
        int criticalHit = ReadI();
        _info.Stats.MAtk = ReadI();
        _info.Stats.MAtkSpd = ReadI();
        int pAttackSpd2 = ReadI();
        _info.Stats.MDef = ReadI();
        int pvpFlag = ReadI();
        _info.Stats.Karma = ReadI();
        int runSpeed = ReadI();
        _info.Stats.Speed = runSpeed;
        //_info.Stats.WalkingSpeed = ReadI();
        _info.Stats.BaseWalkingSpeed = ReadI();
        _info.Stats.BaseRunSpeed = runSpeed;
        




        int swimRunSpd = ReadI();
        int swimWalkSpd = ReadI();
        int flyRunSpd = ReadI();
        int flyWalkSpd = ReadI();
        int flyRunSpd2 = ReadI();
        int flyWalkSpd2 = ReadI();
        double moveMultiplier = ReadD();
        double attackSpeedMultiplier = ReadD();

        _info.Stats.WalkRealSpeed = GetRealSpeed(_info.Stats.BaseWalkingSpeed, (float) moveMultiplier);
        _info.Stats.RunRealSpeed = GetRealSpeed(_info.Stats.BaseRunSpeed, (float)moveMultiplier);
        _info.Stats.PAtkRealSpeed = GetRealSpeed(_info.Stats.PAtkSpd, (float)attackSpeedMultiplier);

        Debug.Log("BasePatakSpeed R " + _info.Stats.PAtkRealSpeed);
        Debug.Log("BasePatakSpeed B" + _info.Stats.BasePAtkSpeed);
        Debug.Log("BasePatakSpeed Spd" + attackSpeedMultiplier);


        _info.Appearance.CollisionRadius = (float)ReadD();
        float collision = (float)ReadD();
        _info.Appearance.CollisionHeight = collision;
        int hairStyle = ReadI();
        int hairColor = ReadI();
        int face = ReadI();
        int isGm = ReadI();
        _info.Identity.Title = ReadOtherS();
        //_info.Identity.Title = "My title";
        _info.Identity.ClanId = ReadI();
        int clanCrestId = ReadI();
        int allyId = ReadI();
        int allyCrestId = ReadI();
        // 0x40 leader rights
        // siege flags: attacker - 0x180 sword over name, defender - 0x80 shield, 0xC0 crown (|leader), 0x1C0 flag (|leader)
        int relation = ReadI();
        byte mountType = ReadB();
        byte privateStoreType = ReadB();
        byte hasDwarvenCraft = ReadB();

        _info.Stats.PkKills = ReadI();
        _info.Stats.PvpKills = ReadI();
        int cubics_size = ReadSh();

        for(int i=0; i < cubics_size; i++)
        {
            ReadSh();
        }

        byte isInPartyMatchRoom = ReadB();
        int isInvisible = ReadI();
        byte isInsideZone = ReadB();
        int clanPrivileges = ReadI();
        int recomLeft = ReadSh();
        int recomHave = ReadSh();
        int mountNpcId = ReadI();
        int inventoryLimit = ReadSh();
        int class_id = ReadI();
        int unknow = ReadI();//// special effects? circles around player...
        _info.Stats.MaxCp = ReadI();
        int cp = ReadI();
        _info.Status.Cp = cp;
        byte enchantEffect = ReadB();
        byte teamId = ReadB();
        int clanCrestLargeId = ReadI();
        byte isNoble = ReadB();
        byte isHero = ReadB();
        byte isFishing = ReadB();
        int fishingX = ReadI();
        int fishingY = ReadI();
        int fishingZ = ReadI();
        int colorName = ReadI();
        byte isRunning = ReadB();// changes the Speed display on Status Window
        _info.Appearance.Running = isRunning == 1;
        
        int pledgeClass = ReadI();
        int PledgeType = ReadI();   
        int titleColor = ReadI();
        int isCursedWeaponEquipped = ReadI();
        StorageVariable.getInstance().ResumeShowDelayMessage((int)MessageID.ADD_EXP_SP);
        Debug.Log("USERRRR INFO пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ пїЅпїЅпїЅпїЅпїЅпїЅпїЅпїЅ!!!! ");
    }

    private float GetRealSpeed(int baseSpeed , float speedMultiplier)
    {
       return  baseSpeed * speedMultiplier;
    }

    private float GetRealSpeed(double baseSpeed, float speedMultiplier)
    {
        return (float)baseSpeed * speedMultiplier;
    }


}
