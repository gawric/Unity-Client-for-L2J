using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RecipeItemMakeInfo : ServerPacket
{
    private int _recipeId;
    private int _isDwarven; // 0 = dwarven, 1 = common
    private int _currentMp;
    private int _maxMp;
    private int _status;
    private RecipeData _data;
    private List<ItemInstance> _requiredItems;
    public int RecipeId => _recipeId;

    public RecipeData RecipeData => _data;

    public List<ItemInstance>  RequiredItems => _requiredItems;
    public int IsDwarvenRecipe => _isDwarven;
    public int CurrentMp => _currentMp;
    public int MaxMp => _maxMp;
    public int CraftStatus => _status;

    public RecipeItemMakeInfo(byte[] data) : base(data)
    {
        _requiredItems = new List<ItemInstance>();
        Parse();
    }

    public override void Parse()
    {
        try
        {
            
            _recipeId = ReadI();
            _isDwarven = ReadI();
            _currentMp = ReadI();
            _maxMp = ReadI();
            _status = ReadI();

            _data = RecipeTable.Instance.GetRecipeData(_recipeId);
            if(_data != null)
            {
                RecipeMaterials[] materials = _data.Materials;

                for(int i = 0; i < materials.Length; i++)
                {
                    RecipeMaterials material = materials[i];

                    if(material != null && material.MaterialsMCnt > 0)
                    {
                        var item = new ItemInstance(-1, material.MaterialsMId, ItemLocation.Void, i, material.MaterialsMCnt, ItemCategory.RequiredItem, false, ItemSlot.none, 0, -1);
                        item.SetItemRequiredType(true, material.MaterialsMCnt);
                        _requiredItems.Add(item);
                    }

                }
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"[RecipeItemMakeInfo] Parse error: {ex.Message}");
            _recipeId = -1;
            _isDwarven = -1;
            _currentMp = 0;
            _maxMp = 0;
            _status = -1;
        }
    }

    public override string ToString()
    {
        return $"[RecipeItemMakeInfo] RecipeId: {_recipeId}, Type: {(IsDwarvenRecipe)}, " +
               $"MP: {_currentMp}/{_maxMp}, Status: {_status}";
    }
}