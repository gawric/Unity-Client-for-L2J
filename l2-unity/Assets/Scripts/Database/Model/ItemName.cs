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
    private int[] _setsThings;

    public int Id { get { return _id; } set { _id = value; } }
    public string Name { get { return _name; } set { _name = value; } }
    public string Description { get { return _description; } set { _description = value; } }
    public string DefaultAction { get { return _defaultAction; } set { _defaultAction = value; } }
    public bool Tradeable { get { return _tradeable; } set { _tradeable = value; } }
    public bool Destructible { get { return _destructible; } set { _destructible = value; } }
    public bool Droppable { get { return _droppable; } set { _droppable = value; } }
    public bool Sellable { get { return _sellable; } set { _sellable = value; } }
    public ItemName[] GetSetsName()
    {
        ItemName[] items = null;

        if (_setsThings != null)
        {
            items = new ItemName[_setsThings.Length];
            for (int i = 0; i < _setsThings.Length; i++)
            {
                int id = _setsThings[i];
                items[i] = ItemNameTable.Instance.GetItemName(id);
            }
        }

        return items;
    }

    public void SetSets(int[] sets)
    {
        if(sets != null)
        {
            _setsThings = sets;
        }
        
    }
}
