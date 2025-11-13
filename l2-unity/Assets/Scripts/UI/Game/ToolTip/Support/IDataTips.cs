using UnityEngine;

public interface IDataTips
{
    string GetName(bool hideCount = false);
    string GetDiscription();

    string GetPrice();

    string GetItemDiscription();

    ItemName[] GetSets();

    ItemSets[] GetSetsEffect();
    string GetIcon();

    Texture2D GetGradeTexture();

    int GetEnchant();
}
