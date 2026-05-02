using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTemplates
{

    private ClassIdTemplate _templateClassId;
    public int Race { get;  set; }
    public int _classId;
    public int  Base_str { get;  set; }
    public int Base_dex { get;  set; }
    public int Base_con { get;  set; }
    public int Base_int { get;  set; }
    public int Base_wit { get;  set; }
    public int Base_men { get;  set; }

    public void SetClassId(int classId)
    {
        _templateClassId = MapClassId.GetClassId(classId);
        _classId = classId;
    }
    public ClassIdTemplate GetTemplateClassId() {
        return _templateClassId; 
    }


}
