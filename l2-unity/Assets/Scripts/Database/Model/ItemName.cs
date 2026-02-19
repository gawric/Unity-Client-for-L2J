using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

[System.Serializable]
public class ItemName {
    [SerializeField] private int _id;
    [SerializeField] private string _name;
    [SerializeField] private string _description;
    [SerializeField] private string _defaultAction;
    [SerializeField] private bool _tradeable;
    [SerializeField] private bool _destructible;
    [SerializeField] private bool _droppable;
    [SerializeField] private bool _sellable;
    private ItemSets[] _setsThings;

    public int Id { get { return _id; } set { _id = value; } }
    public string Name { get { return _name; } set { _name = value; } }
    public string Description { get { return _description; } set { _description = value; } }
    public string DefaultAction { get { return _defaultAction; } set { _defaultAction = value; } }
    public bool Tradeable { get { return _tradeable; } set { _tradeable = value; } }
    public bool Destructible { get { return _destructible; } set { _destructible = value; } }
    public bool Droppable { get { return _droppable; } set { _droppable = value; } }
    public bool Sellable { get { return _sellable; } set { _sellable = value; } }

    private List<ItemName> items = new List<ItemName>();
    public ItemName[] GetSetsName()
    {
        items.Clear();

        if (_setsThings == null) return null;

        foreach (ItemSets setItem in _setsThings)
        {
            if (setItem != null & setItem.GetArrayId().Length > 0)
            {
                int[] array = setItem.GetArrayId();

                for (int i = 0; i < array.Length; i++)
                {
                    int id = array[i];
                    items.Add(ItemNameTable.Instance.GetItemName(id));
                }
            }
        }

        return items.ToArray();
    }

    public ItemSets[] GetSetsEffect()
    {
        return _setsThings;
    }

    public void SetSets(ItemSets[] sets)
    {
        if(sets != null)
        {
            _setsThings = sets;
        }
        
    }
}

public class ItemSets
{

    private int[] _id;
    private string _description;
    public ItemSets(int[] id , string description)
    {
        _id = id;
        _description = description;
    }

    public int[] GetArrayId()
    {
        return _id;
    }

    public string GetDescription()
    {
        return _description;
    }

}
