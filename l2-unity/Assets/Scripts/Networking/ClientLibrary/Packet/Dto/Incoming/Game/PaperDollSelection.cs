using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperDollSelection
{
    private int ObjId_PAPERDOLL_UNDER;
    private int ObjId_PAPERDOLL_REAR;
    private int ObjId_PAPERDOLL_LEAR;
    private int ObjId_PAPERDOLL_NECK;
    private int ObjId_PAPERDOLL_RFINGER;
    private int ObjId_PAPERDOLL_LFINGER;
    private int ObjId_PAPERDOLL_HEAD;
    private int ObjId_PAPERDOLL_RHAND;
    private int ObjId_PAPERDOLL_LHAND;
    private int ObjId_PAPERDOLL_GLOVES;
    private int ObjId_PAPERDOLL_CHEST;
    private int ObjId_PAPERDOLL_LEGS;
    private int ObjId_PAPERDOLL_FEET;
    private int ObjId_PAPERDOLL_CLOAK;
    private int ObjId_PAPERDOLL_RHAND_2;
    private int ObjId_PAPERDOLL_HAIR;
    private int ObjId_PAPERDOLL_HAIR2;
    private int ObjId_PAPERDOLL_Face;

    private int ItemId_PAPERDOLL_UNDER;
    private int ItemId_PAPERDOLL_REAR;
    private int ItemId_PAPERDOLL_LEAR;
    private int ItemId_PAPERDOLL_NECK;
    private int ItemId_PAPERDOLL_RFINGER;
    private int ItemId_PAPERDOLL_LFINGER;
    private int ItemId_PAPERDOLL_HEAD;
    private int ItemId_PAPERDOLL_RHAND;
    private int ItemId_PAPERDOLL_LHAND;
    private int ItemId_PAPERDOLL_GLOVES;
    private int ItemId_PAPERDOLL_CHEST;
    private int ItemId_PAPERDOLL_LEGS;
    private int ItemId_PAPERDOLL_FEET;
    private int ItemId_PAPERDOLL_CLOAK;
    private int ItemId_PAPERDOLL_RHAND_2;
    private int ItemId_PAPERDOLL_HAIR;
    private int ItemId_PAPERDOLL_HAIR2;
    private int ItemId_PAPERDOLL_Face;

    public int Obj_Under { get => ObjId_PAPERDOLL_UNDER; set => ObjId_PAPERDOLL_UNDER = value; }
    public int Obj_Pear { get => ObjId_PAPERDOLL_REAR; set => ObjId_PAPERDOLL_REAR = value; }
    public int Obj_Lear { get => ObjId_PAPERDOLL_LEAR; set => ObjId_PAPERDOLL_LEAR = value; }
    public int Obj_Neck { get => ObjId_PAPERDOLL_NECK; set => ObjId_PAPERDOLL_NECK = value; }
    public int Obj_RFinger { get => ObjId_PAPERDOLL_RFINGER; set => ObjId_PAPERDOLL_RFINGER = value; }
    public int Obj_LFinger { get => ObjId_PAPERDOLL_LFINGER; set => ObjId_PAPERDOLL_LFINGER = value; }
    public int Obj_Head { get => ObjId_PAPERDOLL_HEAD; set => ObjId_PAPERDOLL_HEAD = value; }
    public int Obj_RHand { get => ObjId_PAPERDOLL_RHAND; set => ObjId_PAPERDOLL_RHAND = value; }
    public int Obj_LHand { get => ObjId_PAPERDOLL_LHAND; set => ObjId_PAPERDOLL_LHAND = value; }
    public int Obj_Gloves { get => ObjId_PAPERDOLL_GLOVES; set => ObjId_PAPERDOLL_GLOVES = value; }
    public int Obj_Chest { get => ObjId_PAPERDOLL_CHEST; set => ObjId_PAPERDOLL_CHEST = value; }
    public int Obj_Legs { get => ObjId_PAPERDOLL_LEGS; set => ObjId_PAPERDOLL_LEGS = value; }
    public int Obj_Feet { get => ObjId_PAPERDOLL_FEET; set => ObjId_PAPERDOLL_FEET = value; }
    public int Obj_Cloak { get => ObjId_PAPERDOLL_CLOAK; set => ObjId_PAPERDOLL_CLOAK = value; }

    public int Obj_Rhand2 { get => ObjId_PAPERDOLL_RHAND_2; set => ObjId_PAPERDOLL_RHAND_2 = value; }
    public int Obj_Hair { get => ObjId_PAPERDOLL_HAIR; set => ObjId_PAPERDOLL_HAIR = value; }
    public int Obj_Hair2 { get => ObjId_PAPERDOLL_HAIR2; set => ObjId_PAPERDOLL_HAIR2 = value; }
    public int Obj_Face { get => ObjId_PAPERDOLL_Face; set => ObjId_PAPERDOLL_Face = value; }


    public int Item_Under { get => ItemId_PAPERDOLL_UNDER; set => ItemId_PAPERDOLL_UNDER = value; }
    public int Item_Rear { get => ItemId_PAPERDOLL_REAR; set => ItemId_PAPERDOLL_REAR = value; }
    public int Item_Lear { get => ItemId_PAPERDOLL_LEAR; set => ItemId_PAPERDOLL_LEAR = value; }
    public int Item_Neck { get => ItemId_PAPERDOLL_NECK; set => ItemId_PAPERDOLL_NECK = value; }
    public int Item_RFinger { get => ItemId_PAPERDOLL_RFINGER; set => ItemId_PAPERDOLL_RFINGER = value; }
    public int Item_LFinger { get => ItemId_PAPERDOLL_LFINGER; set => ItemId_PAPERDOLL_LFINGER = value; }
    public int Item_Head { get => ItemId_PAPERDOLL_HEAD; set => ItemId_PAPERDOLL_HEAD = value; }
    public int Item_RHand { get => ItemId_PAPERDOLL_RHAND; set => ItemId_PAPERDOLL_RHAND = value; }
    public int Item_LHand { get => ItemId_PAPERDOLL_LHAND; set => ItemId_PAPERDOLL_LHAND = value; }
    public int Item_Gloves { get => ItemId_PAPERDOLL_GLOVES; set => ItemId_PAPERDOLL_GLOVES = value; }
    public int Item_Chest { get => ItemId_PAPERDOLL_CHEST; set => ItemId_PAPERDOLL_CHEST = value; }
    public int Item_Legs { get => ItemId_PAPERDOLL_LEGS; set => ItemId_PAPERDOLL_LEGS = value; }
    public int Item_Feet { get => ItemId_PAPERDOLL_FEET; set => ItemId_PAPERDOLL_FEET = value; }
    public int Item_Cloak { get => ItemId_PAPERDOLL_CLOAK; set => ItemId_PAPERDOLL_CLOAK = value; }

    public int Item_Rhand2 { get => ItemId_PAPERDOLL_RHAND_2; set => ItemId_PAPERDOLL_RHAND_2 = value; }
    public int Item_Hair { get => ItemId_PAPERDOLL_HAIR; set => ItemId_PAPERDOLL_HAIR = value; }
    public int Item_Hair2 { get => ItemId_PAPERDOLL_HAIR2; set => ItemId_PAPERDOLL_HAIR2 = value; }
    public int Item_Face { get => ItemId_PAPERDOLL_Face; set => ItemId_PAPERDOLL_Face = value; }

}
