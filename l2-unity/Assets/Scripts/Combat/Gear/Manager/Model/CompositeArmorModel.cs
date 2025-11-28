using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class CompositeArmorModel : ArmorDresserModel , IDresserModel
{
    public CompositeArmorModel() : base()
    {

    }

    public override void Update(ArmorPart part, IEnumerable<GameObject> go, IEnumerable<Armor> armor)
    {

        base.Update(part , go  , armor);
    }


    //This class always handles fullPlate Slot so ArmorPart are objects inside FullPlate
    //ArmorPart - FullPlate
    //
    public override void UpdateGo(ArmorPart insidePart, GameObject go)
    {
        base.UpdateGo(insidePart , go);
    }

    public override void UpdateData(ArmorPart insidePart, Armor armor)
    {
        base.UpdateData(insidePart , armor);

    }

    public override Armor GetData(ItemSlot slot)
    {
        return base.GetData(slot);
    }

    public Armor GetData(ArmorPart part)
    {
        return base.GetData(part == ArmorPart.FullArmor ? ArmorPart.Torso : part);
    }


}
