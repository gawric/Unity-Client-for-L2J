using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CharSelectionInfoDto : IWireDto
{
    private List<CharSelectInfoPackage> list;
    private int _selectedSlotId;
    public List<CharSelectInfoPackage> Characters { get { return list; } set { list = value; } }
    public int SelectedSlotId { get { return _selectedSlotId; } set { _selectedSlotId = value; } }

    public CharSelectionInfoDto()
    {
        list = new List<CharSelectInfoPackage>();
    }

    public void ReadFrom(PacketReader reader)
    {
        int size = reader.ReadI();
        Debug.Log("size " + size);
        for (int i = 0; i < size; i++)
        {
            CharSelectInfoPackage character = new CharSelectInfoPackage();
            character.Slot = i;
            character.Name = reader.ReadOtherS();
            //Debug.Log("Name " + character.Name);
            character.ObjId = reader.ReadI();
            character.Account = reader.ReadOtherS();
            //Debug.Log("Account " + character.Account);
            character.AccountId = reader.ReadI();
            //Debug.Log("AccountId " + character.AccountId);
            character.ClanId = reader.ReadI();
            character.BuildLvl = reader.ReadI();
            character.Sex = reader.ReadI();
            character.Race = reader.ReadI();
            character.ClassId = reader.ReadI();
            character.GsName = reader.ReadI();
            int empty1 = reader.ReadI();//0
            int empty2 = reader.ReadI();//0
            int empty3 = reader.ReadI();//0

            character.Hp = reader.ReadD();
            character.Mp = reader.ReadD();
            character.Sp = reader.ReadI();
            character.Exp = reader.ReadLOther();

            character.Lvl = reader.ReadI();
            character.Karma = reader.ReadI();

            int empty4 = reader.ReadI();//0
            int empty5 = reader.ReadI();//0
            int empty6 = reader.ReadI();//0
            int empty7 = reader.ReadI();//0
            int empty8 = reader.ReadI();//0
            int empty9 = reader.ReadI();//0
            int empty10 = reader.ReadI();//0
            int empty11 = reader.ReadI();//0
            int empty12 = reader.ReadI();//0

            character.PaperDoll.Obj_Under = reader.ReadI();
            character.PaperDoll.Obj_Pear = reader.ReadI();

            character.PaperDoll.Obj_Lear = reader.ReadI();
            character.PaperDoll.Obj_Neck = reader.ReadI();

            character.PaperDoll.Obj_RFinger = reader.ReadI();
            character.PaperDoll.Obj_LFinger = reader.ReadI();

            character.PaperDoll.Obj_Head = reader.ReadI();
            character.PaperDoll.Obj_RHand = reader.ReadI();

            character.PaperDoll.Obj_LHand = reader.ReadI();
            character.PaperDoll.Obj_Gloves = reader.ReadI();

            character.PaperDoll.Obj_Chest = reader.ReadI();
            character.PaperDoll.Obj_Legs = reader.ReadI();

            character.PaperDoll.Obj_Feet = reader.ReadI();
            character.PaperDoll.Obj_Cloak = reader.ReadI();

            character.PaperDoll.Obj_RHand = reader.ReadI();
            character.PaperDoll.Obj_Hair = reader.ReadI();

            character.PaperDoll.Obj_Hair2 = reader.ReadI();

            character.PaperDoll.Item_Under = reader.ReadI();
            character.PaperDoll.Item_Rear = reader.ReadI();

            character.PaperDoll.Item_Lear = reader.ReadI();
            character.PaperDoll.Item_Neck = reader.ReadI();

            character.PaperDoll.Item_RFinger = reader.ReadI();
            character.PaperDoll.Item_LFinger = reader.ReadI();

            character.PaperDoll.Item_Head = reader.ReadI();
            character.PaperDoll.Item_RHand = reader.ReadI();

            character.PaperDoll.Item_LHand = reader.ReadI();
            character.PaperDoll.Item_Gloves = reader.ReadI();

            character.Appreance.Chest = reader.ReadI();
            character.Appreance.Legs = reader.ReadI();

            character.PaperDoll.Item_Feet = reader.ReadI();
            character.PaperDoll.Item_Cloak = reader.ReadI();

            character.PaperDoll.Item_RHand = reader.ReadI();
            character.PaperDoll.Item_Hair = reader.ReadI();
            character.PaperDoll.Item_Hair2 = reader.ReadI();

            character.Appreance.RHand = character.PaperDoll.Item_RHand;

            character.HairStyle = reader.ReadI();
            character.HairColor = reader.ReadI();
            character.Face = reader.ReadI();
            character.MaxHp = reader.ReadD();
            character.MaxMp = reader.ReadD();

            character.DlTimer = reader.ReadI();
            character.ClassId = reader.ReadI();
            //character.ActiveId = reader.ReadI();

            character.Selected = reader.ReadI() == 1;

            if (character.Selected)
            {
                _selectedSlotId = i;
            }

            character.EnchantId = reader.ReadB();
            character.Autgmentation = reader.ReadI();
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
