using System.Collections.Generic;
using UnityEngine;

public class AllListImplCombo
{
    //private Dictionary<int, ICombo> dict  = new Dictionary<int, ICombo>();

   


    public static ICombo GetClassCombo(int skill_id)
    {
        if(skill_id == 1177)
        {
            return new Combo_1177();
        }

        return null;

    }
}
