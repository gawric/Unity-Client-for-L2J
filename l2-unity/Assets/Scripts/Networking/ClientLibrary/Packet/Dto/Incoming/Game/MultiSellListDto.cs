using System.Collections.Generic;

public class MultiSellListDto : IWireDto
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
    public MultiSellListDto()
    {
        _listOnlyItem = new List<ItemInstance>();
        _listMultisell = new List<MultiSellData>();
    }

    public void ReadFrom(PacketReader reader)
    {
        
        _listId = reader.ReadI();
        _page = reader.ReadI();
        _finished = reader.ReadI();
        _pageSize = reader.ReadI();
        _size = reader.ReadI();

        for (int i = 0; i < _size; i++)
        {
            int index = reader.ReadI();
            int unk1 = reader.ReadI();
            int unk2 = reader.ReadI();
            _isStackable = reader.ReadB() == 1;
            int sizeProducts = reader.ReadSh();
            int sizeIngredients = reader.ReadSh();

            List<ItemInstance> listProducts = CreateItemList(reader, sizeProducts);
            List<Ingredient> listIngredient = CreateIngredientList(reader, sizeIngredients);

            _listMultisell.Add(new MultiSellData(listProducts, listIngredient));
        }
    }

    private List<ItemInstance> CreateItemList(PacketReader reader, int sizeProducts)
    {
        List < ItemInstance > list = new List <ItemInstance>(sizeProducts);
        for (int pIndex = 0; pIndex < sizeProducts; pIndex++)
        {
            int itemId = reader.ReadSh();
            int bodyPart = reader.ReadI();
            int type2 = reader.ReadSh();
            int itemCount = reader.ReadI();
            int enchantLevel = reader.ReadSh();
            ItemCategory category = ItemsType.ParceCategory(type2);
            ItemSlot itemSlot = ItemsType.ParceSlot(bodyPart);
            //not working. server send 0
            int augmentId = reader.ReadI();
            int manaLeft = reader.ReadI();

            ItemInstance item = new ItemInstance(0, itemId, ItemLocation.Trade, pIndex, itemCount, category, false, itemSlot, enchantLevel , 9999);
            _listOnlyItem.Add(item);
            list.Add(item);
        }

        return list;
    }

    private List<Ingredient> CreateIngredientList(PacketReader reader, int sizeIngredient)
    {
        List<Ingredient> list = new List<Ingredient>(sizeIngredient);

        for (int pIndex = 0; pIndex < sizeIngredient; pIndex++)
        {
            int itemId = reader.ReadSh();
            int type2 = reader.ReadSh();
            int itemCount = reader.ReadI();
            int enchantLevel = reader.ReadSh();

            //not working. server send 0
            int augmentId = reader.ReadI();
            int manaLeft = reader.ReadI();

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
