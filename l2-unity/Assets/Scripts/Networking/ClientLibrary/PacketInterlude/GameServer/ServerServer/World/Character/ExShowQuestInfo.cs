using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class ExShowQuestInfo : ServerPacket
{
    public ExShowQuestInfo(byte[] d) : base(d)
    {
        Parse();
    }
    public override void Parse()
    {
     
    }

}