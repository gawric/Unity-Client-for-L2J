using UnityEngine;

public class UserInfoDto : IWireDto
{
    private PlayerInfoInterlude _info;
    public PlayerInfoInterlude PlayerInfoInterlude { get { return _info; } }



    public UserInfoDto()
    {
    }

    public UserInfoDto(PlayerInfoInterlude info)
    {
        _info = info;
    }

    public void ReadFrom(PacketReader reader)
    {
        int x = reader.ReadI();
        int y = reader.ReadI();
        int z = reader.ReadI();

        //Vector3 unityPos = VectorUtils.ConvertPosToUnity(new Vector3(x,y,z));
        _info.Identity.SetL2jPos(x, y, z);
        float heading = reader.ReadI();
        _info.Identity.OrigHeading = VectorUtils.HeadingToUnityQuaternionForNpc(heading);
        _info.Identity.Heading = Quaternion.Euler(0, _info.Identity.OrigHeading, 0); 
        _info.Identity.Id = reader.ReadI();
     
        _info.Identity.Name = reader.ReadOtherS();
        int reace = reader.ReadI();
        int female = reader.ReadI();
        _info.Appearance.Sex = female;
        _info.Appearance.Race = (int)MapClassId.GetRace(reace);
        _info.Appearance.BaseClass = reader.ReadI();
        _info.Stats.Level = reader.ReadI();
         long exp =  reader.ReadLOther();
        int ost = (int)exp - (int)_info.Stats.Exp;
       // if (ost > 0) StorageVariable.getInstance().AddS1Items(new VariableItem(ost.ToString(), _info.Identity.Id));
        _info.Stats.Exp = exp;
        _info.Stats.MaxExp  = LevelServer.GetExp(_info.Stats.Level + 1);
        _info.Stats.Str = reader.ReadI();
        _info.Stats.Dex = reader.ReadI();
        _info.Stats.Con = reader.ReadI();
        _info.Stats.Int = reader.ReadI();
        _info.Stats.Wit = reader.ReadI();
        _info.Stats.Men = reader.ReadI();

        _info.Stats.MaxHp = reader.ReadI();
        _info.Status.SetHp(reader.ReadI());
        _info.Stats.MaxMp = reader.ReadI();
        _info.Status.SetMp(reader.ReadI());
        int sp = reader.ReadI();
        int oldSp = (int)_info.Stats.OldSp;
        int ostSp = (int)sp - oldSp;
        //StorageVariable.getInstance().AddS2Items(new VariableItem(ostSp.ToString(), _info.Identity.Id));
        _info.Stats.OldSp = sp;
        _info.Stats.Sp = sp;
        _info.Stats.CurrWeight = reader.ReadI();
        _info.Stats.MaxWeight = reader.ReadI();  //the max weight that the Creature can load.
        int activeWeaponItem = reader.ReadI(); // 20 no weapon, 40 weapon equipped

        /**
 * Returns the objectID associated to the item in the paperdoll slot
 * @param slot : int pointing out the slot
 * @return int designating the objectID
 */
        var paperTest = _info.Appearance.PaperDoll;
        _info.Appearance.PaperDoll.Obj_Under = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_Pear = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_Lear = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_Neck = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_RFinger = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_LFinger = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_Head = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_RHand = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_LHand = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_Gloves = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_Chest = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_Legs = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_Feet = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_Cloak = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_RHand = reader.ReadI();
        _info.Appearance.PaperDoll.Obj_Hair = reader.ReadI();

        _info.Appearance.PaperDoll.Obj_Face = reader.ReadI();


        /**
 * Returns the ID of the item in the paperdoll slot
 * @param slot : int designating the slot
 * @return int designating the ID of the item
 */     
        _info.Appearance.PaperDoll.Item_Under = reader.ReadI();
        _info.Appearance.PaperDoll.Item_Rear = reader.ReadI();

        _info.Appearance.PaperDoll.Item_Lear = reader.ReadI();
        _info.Appearance.PaperDoll.Item_Neck = reader.ReadI();

        _info.Appearance.PaperDoll.Item_RFinger = reader.ReadI();
        _info.Appearance.PaperDoll.Item_LFinger = reader.ReadI();

        _info.Appearance.PaperDoll.Item_Head = reader.ReadI();
        _info.Appearance.PaperDoll.Item_RHand = reader.ReadI();

        _info.Appearance.PaperDoll.Item_LHand = reader.ReadI();
        _info.Appearance.PaperDoll.Item_Gloves = reader.ReadI();
        _info.Appearance.Gloves = _info.Appearance.PaperDoll.Item_Gloves;

        _info.Appearance.PaperDoll.Item_Chest = reader.ReadI();
        _info.Appearance.Chest = _info.Appearance.PaperDoll.Item_Chest;

        _info.Appearance.PaperDoll.Item_Legs = reader.ReadI();
        _info.Appearance.Legs = _info.Appearance.PaperDoll.Item_Legs;

        _info.Appearance.PaperDoll.Item_Feet = reader.ReadI();
        _info.Appearance.Feet = _info.Appearance.PaperDoll.Item_Feet;
        _info.Appearance.PaperDoll.Item_Cloak = reader.ReadI();

        _info.Appearance.PaperDoll.Item_RHand = reader.ReadI();
        _info.Appearance.PaperDoll.Item_Hair = reader.ReadI();
        _info.Appearance.PaperDoll.Item_Face = reader.ReadI();

        _info.Appearance.RHand = _info.Appearance.PaperDoll.Item_RHand;
        _info.Appearance.LHand = _info.Appearance.PaperDoll.Item_LHand;

        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        //buffer.writeInt(_player.getInventory().getPaperdollAugmentationId(Inventory.PAPERDOLL_RHAND));
        int rhandAugmentationId = reader.ReadI();

        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        // buffer.writeInt(_player.getInventory().getPaperdollAugmentationId(Inventory.PAPERDOLL_RHAND));
        int rhandAugmentationId2 = reader.ReadI();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        reader.ReadSh();
        
        _info.Stats.PAtk = reader.ReadI();
        int atackspped = reader.ReadI();
        _info.Stats.BasePAtkSpeed = atackspped;
        //_info.Stats.PAtkSpd = atackspped;
        // Debug.Log("PAAAAAAAAAAAATACK SPEED " + atackspped);
        _info.Stats.PAtkSpd = atackspped;
        _info.Stats.PDef = reader.ReadI();
        _info.Stats.PEvasion = reader.ReadI();
        // int accuracy = reader.ReadI();
        _info.Stats.MAccuracy = reader.ReadI();
        int criticalHit = reader.ReadI();
        _info.Stats.MAtk = reader.ReadI();
        _info.Stats.MAtkSpd = reader.ReadI();
        int pAttackSpd2 = reader.ReadI();
        _info.Stats.MDef = reader.ReadI();
        int pvpFlag = reader.ReadI();
        _info.Stats.Karma = reader.ReadI();
        int runSpeed = reader.ReadI();
        _info.Stats.Speed = runSpeed;
        //_info.Stats.WalkingSpeed = reader.ReadI();
        _info.Stats.BaseWalkingSpeed = reader.ReadI();
        _info.Stats.BaseRunSpeed = runSpeed;
        




        int swimRunSpd = reader.ReadI();
        int swimWalkSpd = reader.ReadI();
        int flyRunSpd = reader.ReadI();
        int flyWalkSpd = reader.ReadI();
        int flyRunSpd2 = reader.ReadI();
        int flyWalkSpd2 = reader.ReadI();
        double moveMultiplier = reader.ReadD();
        double attackSpeedMultiplier = reader.ReadD();

        _info.Stats.WalkRealSpeed = GetRealSpeed(_info.Stats.BaseWalkingSpeed, (float) moveMultiplier);
        _info.Stats.RunRealSpeed = GetRealSpeed(_info.Stats.BaseRunSpeed, (float)moveMultiplier);
        _info.Stats.PAtkRealSpeed = GetRealSpeed(_info.Stats.PAtkSpd, (float)attackSpeedMultiplier);

        Debug.Log("BasePatakSpeed R " + _info.Stats.PAtkRealSpeed);
        Debug.Log("BasePatakSpeed B" + _info.Stats.BasePAtkSpeed);
        Debug.Log("BasePatakSpeed Spd" + attackSpeedMultiplier);


        _info.Appearance.CollisionRadius = (float)reader.ReadD();
        float collision = (float)reader.ReadD();
        _info.Appearance.CollisionHeight = collision;
        int hairStyle = reader.ReadI();
        int hairColor = reader.ReadI();
        int face = reader.ReadI();
        int isGm = reader.ReadI();
        _info.Identity.Title = reader.ReadOtherS();
        //_info.Identity.Title = "My title";
        _info.Identity.ClanId = reader.ReadI();
        int clanCrestId = reader.ReadI();
        int allyId = reader.ReadI();
        int allyCrestId = reader.ReadI();
        // 0x40 leader rights
        // siege flags: attacker - 0x180 sword over name, defender - 0x80 shield, 0xC0 crown (|leader), 0x1C0 flag (|leader)
        int relation = reader.ReadI();
        byte mountType = reader.ReadB();
        byte privateStoreType = reader.ReadB();
        byte hasDwarvenCraft = reader.ReadB();

        _info.Stats.PkKills = reader.ReadI();
        _info.Stats.PvpKills = reader.ReadI();
        int cubics_size = reader.ReadSh();

        for(int i=0; i < cubics_size; i++)
        {
            reader.ReadSh();
        }

        byte isInPartyMatchRoom = reader.ReadB();
        int isInvisible = reader.ReadI();
        byte isInsideZone = reader.ReadB();
        int clanPrivileges = reader.ReadI();
        int recomLeft = reader.ReadSh();
        int recomHave = reader.ReadSh();
        int mountNpcId = reader.ReadI();
        int inventoryLimit = reader.ReadSh();
        int class_id = reader.ReadI();
        int unknow = reader.ReadI();//// special effects? circles around player...
        _info.Stats.MaxCp = reader.ReadI();
        int cp = reader.ReadI();
        _info.Status.Cp = cp;
        byte enchantEffect = reader.ReadB();
        byte teamId = reader.ReadB();
        int clanCrestLargeId = reader.ReadI();
        byte isNoble = reader.ReadB();
        byte isHero = reader.ReadB();
        byte isFishing = reader.ReadB();
        int fishingX = reader.ReadI();
        int fishingY = reader.ReadI();
        int fishingZ = reader.ReadI();
        int colorName = reader.ReadI();
        byte isRunning = reader.ReadB();// changes the Speed display on Status Window
        _info.Appearance.Running = isRunning == 1;
        
        int pledgeClass = reader.ReadI();
        int PledgeType = reader.ReadI();   
        int titleColor = reader.ReadI();
        int isCursedWeaponEquipped = reader.ReadI();
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
