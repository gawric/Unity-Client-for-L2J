using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VariableItem
{
    public VariableItem(string name , int objId)
    {
        Name = name;
        ObjId = objId;
    }
   public string Name { get; private set; }
   public int ObjId { get; private set; }
}
