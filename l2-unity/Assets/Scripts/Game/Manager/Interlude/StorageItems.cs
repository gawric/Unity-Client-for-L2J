using System.Collections.Generic;


public class StorageItems
{
    private static StorageItems instance;
    private object _sync = new object();
    private Dictionary <int , ItemInstance> items;
    private Dictionary<int, ItemInstance> equip_items;
    private List<Shortcut> shortCuts;
    private bool showWindow;
    private StorageItems()
    { }

    public static StorageItems getInstance()
    {
        if (instance == null)
        {
            instance = new StorageItems();
        }

        return instance;
    }
    public void AddShortCuts(List<Shortcut> shortCuts)
    {
       // lock (_sync)
       // {
            if (this.shortCuts != null)
            {
                this.shortCuts.Clear();
            }

            this.shortCuts = shortCuts;
       // }
       
    }
    public void AddItems(Dictionary<int, ItemInstance> itemsParce)
    {
        //lock (_sync)
       // {
            items = null;
            items = itemsParce;
       // } 
    }

    public void AddEquipItems(Dictionary<int, ItemInstance> itemsParce)
    {
        //lock (_sync)
        // {
        equip_items = null;
        equip_items = itemsParce;
        // } 
    }

    public void AddShow(bool show)
    {
        showWindow = show;
    }

    public Dictionary<int , ItemInstance>  GetItems()
    {
        //lock (_sync)
       // {
            if (items != null)
            {
                return items;
            }
            return new Dictionary<int , ItemInstance>();
       // }

    }

    public Dictionary<int, ItemInstance> GetEquipItems()
    {
        //lock (_sync)
        // {
        if (equip_items != null)
        {
            return equip_items;
        }
        return new Dictionary<int, ItemInstance>();
        // }

    }

    public List<Shortcut> GetShortCuts()
    {
        lock (_sync)
        {
            if (shortCuts != null)
            {
                return shortCuts;
            }
            return new List<Shortcut>();
        }

    }

    public bool GetShowWindow()
    {
        return showWindow;
    }
}
