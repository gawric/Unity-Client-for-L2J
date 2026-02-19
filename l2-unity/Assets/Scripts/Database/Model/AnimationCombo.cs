using UnityEngine;

public class AnimationCombo
{
    private string tag;
    private string[] anim_name;
    private float anim_leght;
    private string unk0;

    public AnimationCombo(string tag , string[] anim_name  , string unk0)
    {
        this.tag = tag;
        this.anim_name = anim_name;
        this.unk0 = unk0;
    }

    public string[] GetAnimCycle()
    {
        return anim_name;
    }

    public string GetAnimToIndex(int index)
    {
        return anim_name[index];
    }

}



