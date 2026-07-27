using System.Collections.Generic;

public class PartyMemberData
{
    public int ObjectId;
    public string Name;
    public int CurCp;
    public int MaxCp;
    public int CurHp;
    public int MaxHp;
    public int CurMp;
    public int MaxMp;
    public int Level;
    public int ClassId;
    public readonly List<PartyBuffInfo> Buffs = new List<PartyBuffInfo>();

    public void ApplySnapshot(PartyMemberSnapshot snapshot)
    {
        ObjectId = snapshot.ObjectId;
        Name = snapshot.Name;
        CurCp = snapshot.CurCp;
        MaxCp = snapshot.MaxCp;
        CurHp = snapshot.CurHp;
        MaxHp = snapshot.MaxHp;
        CurMp = snapshot.CurMp;
        MaxMp = snapshot.MaxMp;
        Level = snapshot.Level;
        ClassId = snapshot.ClassId;
    }
}
