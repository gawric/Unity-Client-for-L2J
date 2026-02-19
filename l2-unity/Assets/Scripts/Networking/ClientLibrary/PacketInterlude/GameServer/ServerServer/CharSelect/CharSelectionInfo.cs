using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CharSelectionInfo : ServerPacket
{
    private List<CharSelectInfoPackage> list;
    private int _selectedSlotId;
    public List<CharSelectInfoPackage> Characters { get { return list; } set { list = value; } }
    public int SelectedSlotId { get { return _selectedSlotId; } set { _selectedSlotId = value; } }

    public CharSelectionInfo(byte[] d) : base(d)
    {
        list = new List<CharSelectInfoPackage>();
        Parse();
    }

    public override void Parse()
    {
        int size = ReadI();
        Debug.Log("size " + size);
        for (int i = 0; i < size; i++)
        {
            CharSelectInfoPackage character = new CharSelectInfoPackage();
            character.Slot = i;
            character.Name = ReadOtherS();
            //Debug.Log("Name " + character.Name);
            character.ObjId = ReadI();
            character.Account = ReadOtherS();
            //Debug.Log("Account " + character.Account);
            character.AccountId = ReadI();
            //Debug.Log("AccountId " + character.AccountId);
            character.ClanId = ReadI();
            character.BuildLvl = ReadI();
            character.Sex = ReadI();
            character.Race = ReadI();
            character.ClassId = ReadI();
            character.GsName = ReadI();
            int empty1 = ReadI();//0
            int empty2 = ReadI();//0
            int empty3 = ReadI();//0

            character.Hp = ReadD();
            character.Mp = ReadD();
            character.Sp = ReadI();
            character.Exp = ReadLOther();

            character.Lvl = ReadI();
            character.Karma = ReadI();

            int empty4 = ReadI();//0
            int empty5 = ReadI();//0
            int empty6 = ReadI();//0
            int empty7 = ReadI();//0
            int empty8 = ReadI();//0
            int empty9 = ReadI();//0
            int empty10 = ReadI();//0
            int empty11 = ReadI();//0
            int empty12 = ReadI();//0

            character.PaperDoll.Obj_Under = ReadI();
            character.PaperDoll.Obj_Pear = ReadI();

            character.PaperDoll.Obj_Lear = ReadI();
            character.PaperDoll.Obj_Neck = ReadI();

            character.PaperDoll.Obj_RFinger = ReadI();
            character.PaperDoll.Obj_LFinger = ReadI();

            character.PaperDoll.Obj_Head = ReadI();
            character.PaperDoll.Obj_RHand = ReadI();

            character.PaperDoll.Obj_LHand = ReadI();
            character.PaperDoll.Obj_Gloves = ReadI();

            character.PaperDoll.Obj_Chest = ReadI();
            character.PaperDoll.Obj_Legs = ReadI();

            character.PaperDoll.Obj_Feet = ReadI();
            character.PaperDoll.Obj_Cloak = ReadI();

            character.PaperDoll.Obj_RHand = ReadI();
            character.PaperDoll.Obj_Hair = ReadI();

            character.PaperDoll.Obj_Hair2 = ReadI();

            character.PaperDoll.Item_Under = ReadI();
            character.PaperDoll.Item_Rear = ReadI();

            character.PaperDoll.Item_Lear = ReadI();
            character.PaperDoll.Item_Neck = ReadI();

            character.PaperDoll.Item_RFinger = ReadI();
            character.PaperDoll.Item_LFinger = ReadI();

            character.PaperDoll.Item_Head = ReadI();
            character.PaperDoll.Item_RHand = ReadI();

            character.PaperDoll.Item_LHand = ReadI();
            character.PaperDoll.Item_Gloves = ReadI();

            character.Appreance.Chest = ReadI();
            character.Appreance.Legs = ReadI();

            character.PaperDoll.Item_Feet = ReadI();
            character.PaperDoll.Item_Cloak = ReadI();

            character.PaperDoll.Item_RHand = ReadI();
            character.PaperDoll.Item_Hair = ReadI();
            character.PaperDoll.Item_Hair2 = ReadI();

            character.Appreance.RHand = character.PaperDoll.Item_RHand;

            character.HairStyle = ReadI();
            character.HairColor = ReadI();
            character.Face = ReadI();
            character.MaxHp = ReadD();
            character.MaxMp = ReadD();

            character.DlTimer = ReadI();
            character.ClassId = ReadI();
            //character.ActiveId = ReadI();

            character.Selected = ReadI() == 1;

            if (character.Selected)
            {
                _selectedSlotId = i;
            }

            character.EnchantId = ReadB();
            character.Autgmentation = ReadI();
            //unknown ...set false magic
            //bool IsMage = false;
            //CharacterRace raceDefault;
            //byte sexdefault;
            //0 - M
            //1 - F
            //if (i == 0)
            ///{
                //raceDefault = (CharacterRace)CharacterRace.Dwarf;
            //sexdefault = 0;
            //IsMage = false;
            //}
            //else if (i == 1)
            ///{


            //raceDefault = (CharacterRace)CharacterRace.DarkElf;
            //sexdefault = 0;
            //IsMage = false;
            //}
            //else
            //{
            //raceDefault = (CharacterRace)CharacterRace.DarkElf;
            //sexdefault = (byte)CharacterRaceAnimation.FDarkElf;
            //}

            CharacterRace race = MapClassId.GetCharacterRace(character.ClassId);
            bool IsMage = MapClassId.IsMage(character.ClassId);
            character.CharacterRaceAnimation = CharacterRaceAnimationParser.ParseRace(race, (byte)character.Sex, IsMage);

            list.Add(character);

        }
    }
}
