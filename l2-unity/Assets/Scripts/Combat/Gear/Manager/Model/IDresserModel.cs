using System.Collections.Generic;
using UnityEngine;
using static ArmorDresserModel;

public interface IDresserModel
{
    public void Update(ArmorPart part, IEnumerable<GameObject>  go, IEnumerable<Armor> armor);
    public void UpdateGo(ArmorPart part, GameObject go);
    public void UpdateData(ArmorPart part, Armor armor);
    public GameObject GetGo(ArmorPart part);
    public GameObject GetGo(ItemSlot slot);
    public Armor GetData(ItemSlot slot);
    public Armor GetData(ArmorPart part);

}
