using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class SkillListDto : IWireDto
{
    public List<SkillInstance> Skills { get; set; }


    public SkillListDto()
    {
        Skills = new List<SkillInstance>();
    }

    public void ReadFrom(PacketReader reader)
    {
        
        int size = reader.ReadI();
        for(int i = 0; i < size; i++)
        {
            bool passive = reader.ReadI() == 1;
            int pLevel = reader.ReadI();
            int pId = reader.ReadI();
            int disabled = (int) reader.ReadB();


            Skills.Add(new SkillInstance(pId, pLevel, passive, disabled == 1));
        }
    }


}
