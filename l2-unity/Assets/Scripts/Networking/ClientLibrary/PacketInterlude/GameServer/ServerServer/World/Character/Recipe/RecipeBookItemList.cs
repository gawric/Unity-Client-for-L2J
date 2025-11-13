using System.Collections.Generic;

public class RecipeBookItemList : ServerPacket
{
    private int _isDwarven;
    private int _maxMp;
    private List<ItemInstance> _listItemInstance;
    private List<RecipeInstance> _listRecipes;

    // Getters
    public int IsDwarven => _isDwarven;
    public int MaxMp => _maxMp;
    public List<RecipeInstance> ListRecipes => _listRecipes;
    public List<ItemInstance> ListItemInstance => _listItemInstance;

    public RecipeBookItemList(byte[] d) : base(d)
    {
        _listItemInstance = new List<ItemInstance>();
        _listRecipes = new List<RecipeInstance>();
        Parse();
    }
    public override void Parse()
    {

        _isDwarven = ReadI();
        _maxMp = ReadI();
        int size = ReadI();

        for(int i = 0; i < size; i++)
        {
            int recipeId = ReadI();
            int position = ReadI();

            RecipeData data = RecipeTable.Instance.GetRecipeData(recipeId);

            if(data != null)
            {
                var recipe = new RecipeInstance(data, position);
                _listItemInstance.Add(recipe.GetItemInstance());
                _listRecipes.Add(recipe);
            }

        }
    }
}

public class RecipeInstance :ItemInstance
{
    private RecipeData _data;
    private int _position;
    private ItemInstance _recipeInstance;
    public RecipeInstance(RecipeData data , int position): 
        base(-1, data.ItemId, ItemLocation.Void, position, 1, ItemCategory.Item, false, ItemSlot.none, 0, -1)
    {
        _data = data;
        _position = position;
        _recipeInstance = new ItemInstance(-1, data.RecipeId, ItemLocation.Void, position, 1, ItemCategory.Item, false, ItemSlot.none, 0, -1);
    }

    public ItemInstance GetRecipeItemInstance()
    {
        return _recipeInstance;
    }

    public ItemInstance GetItemInstance()
    {
        return this;
    }

    public int GetPosition()
    {
        return _position;
    }

    public int GetIDMK()
    {
        return _data.IDMk;
    }
}
