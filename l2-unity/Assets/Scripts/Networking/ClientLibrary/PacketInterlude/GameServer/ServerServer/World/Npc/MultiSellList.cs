using System.Collections.Generic;

public class MultiSellList : ServerPacket
{
    private List<MultiSellData> _listMultisell;
    private List<ItemInstance> _listOnlyItem;

    private int _listId;
    private int _page;
    private int _pageSize;
    private int _finished;
    private bool _isStackable;
    private int _size;

    public int GetListId() { return _listId;  }
    public List<MultiSellData> GetMultiSell() { return _listMultisell; }
    public List<ItemInstance> GetOnlyItems() { return _listOnlyItem; }
    public MultiSellList(byte[] d) : base(d)
    {
        _listOnlyItem = new List<ItemInstance>();
        _listMultisell = new List<MultiSellData>();
        Parse();
    }

    public override void Parse()
    {
        
        _listId = ReadI();
        _page = ReadI();
        _finished = ReadI();
        _pageSize = ReadI();
        _size = ReadI();

        for (int i = 0; i < _size; i++)
        {
            int index = ReadI();
            int unk1 = ReadI();
            int unk2 = ReadI();
            _isStackable = ReadB() == 1;
            int sizeProducts = ReadSh();
            int sizeIngredients = ReadSh();

            List<ItemInstance> listProducts = CreateItemList(sizeProducts);
            List<Ingredient> listIngredient = CreateIngredientList(sizeIngredients);

            _listMultisell.Add(new MultiSellData(listProducts, listIngredient));
        }
    }

    private List<ItemInstance> CreateItemList(int sizeProducts)
    {
        List < ItemInstance > list = new List <ItemInstance>(sizeProducts);
        for (int pIndex = 0; pIndex < sizeProducts; pIndex++)
        {
            int itemId = ReadSh();
            int bodyPart = ReadI();
            int type2 = ReadSh();
            int itemCount = ReadI();
            int enchantLevel = ReadSh();
            ItemCategory category = ItemsType.ParceCategory(type2);
            ItemSlot itemSlot = ItemsType.ParceSlot(bodyPart);
            //not working. server send 0
            int augmentId = ReadI();
            int manaLeft = ReadI();

            ItemInstance item = new ItemInstance(0, itemId, ItemLocation.Trade, pIndex, itemCount, category, false, itemSlot, enchantLevel , 9999);
            _listOnlyItem.Add(item);
            list.Add(item);
        }

        return list;
    }

    private List<Ingredient> CreateIngredientList(int sizeIngredient)
    {
        List<Ingredient> list = new List<Ingredient>(sizeIngredient);

        for (int pIndex = 0; pIndex < sizeIngredient; pIndex++)
        {
            int itemId = ReadSh();
            int type2 = ReadSh();
            int itemCount = ReadI();
            int enchantLevel = ReadSh();

            //not working. server send 0
            int augmentId = ReadI();
            int manaLeft = ReadI();

            Ingredient product = new Ingredient(itemId, type2, itemCount, enchantLevel);
            list.Add(product);
        }

        return list;
    }

}

public class MultiSellData
{
    private List<Ingredient> _ingredients;
    private List<ItemInstance> _itemInstance;
    public MultiSellData(List<ItemInstance> itemInstance , List<Ingredient> ingredients)
    {
        _ingredients = ingredients;
        _itemInstance = itemInstance;
    }
    public List<Ingredient> IngredientList { get => _ingredients; }
    public List<ItemInstance> ProductList { get => _itemInstance; }
}

public class Ingredient
{
    private int _itemId;
    private ItemInstance _itemInstance;
    private int _type2;
    private int _itemCount;
    private int _enchantLevel;

    public Ingredient(int itemId ,  int type2 , int itemCount , int enchantLevel)
    {
        _itemId = itemId;
        _type2 = type2;
        _itemCount = itemCount;
        _enchantLevel = enchantLevel;
        ItemCategory category = ItemsType.ParceCategory(type2);

        _itemInstance = new ItemInstance(0, itemId, ItemLocation.Trade, 0, itemCount, category, false, ItemSlot.none, _enchantLevel, 9999);
    }

    public ItemInstance GetItemInstance() { return _itemInstance; }
}
