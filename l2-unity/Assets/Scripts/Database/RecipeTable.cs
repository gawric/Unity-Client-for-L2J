using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecipeTable
{
    private static RecipeTable _instance;
    private static Dictionary<int, RecipeData> _recipesData;

    private int _indexName = 0;
    private int _indexid_mk = 1;
    private int _indexRecipeId = 2;
    private int _indexlevel = 3;
    private int _indexItemId = 4;
    private int _indexCount = 5;
    private int _indexMpCost = 6;
    private int _indexSuccesRate = 7;
    public static RecipeTable Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new RecipeTable();
                _recipesData = new Dictionary<int, RecipeData>();
            }

            return _instance;
        }
    }


    public RecipeData GetRecipeData(int recipeId)
    {
        if (_recipesData.ContainsKey(recipeId))
        {
            return _recipesData[recipeId];
        }
        return null;
    }






    public void Initialize()
    {
        try
        {
            string dataPathE = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Recipe-c_interlude.txt");
            ReadRecipeInterlude(dataPathE);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize QuestNameTable: {ex.Message}");
        }
    }

    public void ReadRecipeInterlude(string dataPath)
    {

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            int index = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (index != 0)
                {
                    string[] ids = line.Split('\t');
                    int id = Int32.Parse(ids[_indexid_mk]);
                    RecipeData recipeData = CreateModel(ids, id);
                    if (!_recipesData.ContainsKey(recipeData.IDMk))
                    {
                        _recipesData.Add(recipeData.IDMk , recipeData);
                    }
                }
                index++;
            }

        }

    }

    private RecipeData CreateModel(string[] ids, int idMk)
    {
        RecipeData recipe = new RecipeData();

        string aliase = DatUtils.CleanupStringOldData(ids[_indexName]);
        recipe.Alias = aliase;
        recipe.IDMk = idMk;
        recipe.RecipeId = Int32.Parse(ids[_indexRecipeId]); ;
        recipe.Level = Int32.Parse(ids[_indexlevel]);
        recipe.ItemId = Int32.Parse(ids[_indexItemId]);
        recipe.Count = Int32.Parse(ids[_indexCount]);
        recipe.MpCost = Int32.Parse(ids[_indexMpCost]);
        recipe.SuccessRate = Int32.Parse(ids[_indexSuccesRate]);

        // Set materials with bounds checking
        SetMaterialSafely(0, recipe, ids);
        SetMaterialSafely(1, recipe, ids);
        SetMaterialSafely(2, recipe, ids);
        SetMaterialSafely(3, recipe, ids);
        SetMaterialSafely(4, recipe, ids);
        SetMaterialSafely(5, recipe, ids);
        SetMaterialSafely(6, recipe, ids);
        SetMaterialSafely(7, recipe, ids);
        SetMaterialSafely(8, recipe, ids);


        return recipe;
    }

    // Helper method to safely set materials
    void SetMaterialSafely(int materialIndex, RecipeData recipe, string[] ids)
    {
        int idIndex = 9 + (materialIndex * 2);
        int countIndex = idIndex + 1;

        if (idIndex + 1 < ids.Length)
        {
            int materialId = SafeIntParse(ids, idIndex);
            int materialCount = SafeIntParse(ids, countIndex);
            recipe.SetMaterials(materialId, materialCount, materialIndex);
        }
    }


    private int SafeIntParse(string[] array, int index)
    {
        if (index < 0 || index >= array.Length)
        {
            Debug.LogWarning($"Index {index} is out of bounds for array of length {array.Length}");
            return 0;
        }

        if (int.TryParse(array[index], out int result))
        {
            return result;
        }
        else
        {
            Debug.LogWarning($"Failed to parse value '{array[index]}' to integer at index {index}");
            return 0;
        }


    }
}
