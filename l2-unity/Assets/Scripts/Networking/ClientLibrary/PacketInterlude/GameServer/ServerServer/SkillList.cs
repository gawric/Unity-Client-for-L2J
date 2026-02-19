using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class SkillList : ServerPacket
{
    public List<SkillInstance> Skills { get; set; }


    public SkillList(byte[] d) : base(d)
    {
        Skills = new List<SkillInstance>();
        Parse();
    }

    public override void Parse()
    {
        
        int size = ReadI();
        for(int i = 0; i < size; i++)
        {
            bool passive = ReadI() == 1;
            int pLevel = ReadI();
            int pId = ReadI();
            int disabled = (int) ReadB();


            Skills.Add(new SkillInstance(pId, pLevel, passive, disabled == 1));
        }
    }


}
