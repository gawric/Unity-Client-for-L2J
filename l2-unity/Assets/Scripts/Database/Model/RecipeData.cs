using UnityEngine;

public class RecipeData 
{
    private string _alias;
    private int _idMk;
    private int _recipeId;
    private int _itemId;
    private int _level;
    private int _count;
    private int _mp_cost;
    private int _success_rate;
    private RecipeMaterials[] _materials;


    public void SetMaterials(int materials_m_id , int materials_m_count , int indexArray)
    {
        if(_materials == null) _materials = new RecipeMaterials[10];
        var data = new RecipeMaterials(materials_m_id, materials_m_count);
        _materials[indexArray] = data;
    }
    public int IDMk
    {
        get { return _idMk; }
        set { _idMk = value; }
    }

    // Property for alias
    public string Alias
    {
        get { return _alias; }
        set { _alias = value; }
    }

    // Property for recipeId
    public int RecipeId
    {
        get { return _recipeId; }
        set { _recipeId = value; }
    }

    // Property for itemId
    public int ItemId
    {
        get { return _itemId; }
        set { _itemId = value; }
    }

    // Property for level
    public int Level
    {
        get { return _level; }
        set { _level = value; }
    }

    // Property for count
    public int Count
    {
        get { return _count; }
        set { _count = value; }
    }

    // Property for mp_cost
    public int MpCost
    {
        get { return _mp_cost; }
        set { _mp_cost = value; }
    }

    // Property for success_rate
    public int SuccessRate
    {
        get { return _success_rate; }
        set { _success_rate = value; }
    }

    // Property for materials array
    public RecipeMaterials[] Materials
    {
        get { return _materials; }
        set { _materials = value; }
    }
}
