using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemMessageDat
{
    private int _id;
    private string _message;
    private string _original;
    private int _group;
    private string _color;

    public int Id { get { return _id; } set { _id = value; } }
    public string Message { get { return _message; } set { _message = value; } }

    public string OriginalMessage { get { return _original; } set { _original = value; } }
    public int Group { get { return _group; } set { _group = value; } }
    public string Color { get { return _color; } set { _color = value; } }

    public string AddSkillName(string skilName)
    {
        string messageSkill = _original;
        if (messageSkill.IndexOf("$s1.") == -1)
        {
            return  messageSkill + " " + skilName;
        }
        else
        {
            return  messageSkill.Replace("$s1.", skilName).Trim();
        }
    }
}
