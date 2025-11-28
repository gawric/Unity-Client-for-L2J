using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractCache : AbstractGetCache
{

    protected T[] LoadAllArmorResources<T>(List<string> allModels, Func<string, T> loadResourceFunc)
    {
        if (allModels.Count == 0)
        {
            return Array.Empty<T>();
        }

        T[] resources = new T[allModels.Count];

        for (int i = 0; i < allModels.Count; i++)
        {
            string model = allModels[i];

            if (!string.IsNullOrEmpty(model))
            {
                resources[i] = loadResourceFunc(model);
            }
        }

        return resources;
    }


    protected GameObject[] LoadAllArmorModels(List<string> allModels)
    {
        return LoadAllArmorResources(allModels, LoadArmorModel);
    }

    protected Material[] LoadAllArmorMaterials(List<string> allModels)
    {
        return LoadAllArmorResources(allModels, LoadArmorMaterial);
    }

    protected GameObject LoadArmorModel(string model)
    {

        string[] folderFile = model.Split(".");

        if (folderFile.Length < 2) return null;


        string modelPath = $"Data/Animations/{folderFile[0]}/{folderFile[1]}";

        GameObject armorPiece = (GameObject)Resources.Load(modelPath);
        if (armorPiece == null)
        {
            Debug.LogWarning($"Can't find armor model at {modelPath}");
        }
        else
        {
            Debug.Log($"Successfully loaded armor model at {modelPath}");
        }

        return armorPiece;
    }

    protected Material LoadArmorMaterial(string texture)
    {
        string[] folderFile = texture.Split(".");

        if (folderFile.Length < 2) return null;



        string materialPath = $"Data/SysTextures/{folderFile[0]}/Materials/{folderFile[1]}";

        Material material = (Material)Resources.Load(materialPath);
        if (material == null)
        {
            // Debug.LogWarning($"Can't find armor model at {materialPath}");
        }
        else
        {
            // Debug.Log($"Successfully loaded armor model at {materialPath}");
        }

        return material;
    }

    protected GameObject LoadNpc(string meshname)
    {
        string[] folderFile = meshname.Split(".");

        if (folderFile.Length < 2)
        {
            return null;
        }

        string path = $"Data/Animations/{folderFile[0]}/{folderFile[1]}/{folderFile[1]}";

        return Resources.Load<GameObject>(path);
    }

    protected GameObject LoadWeaponModel(string model)
    {
        string[] folderFile = model.Split(".");

        if (folderFile.Length < 2) return null;

        string modelPath = $"Data/Animations/{folderFile[0]}/{folderFile[1]}";

        GameObject weapon = (GameObject)Resources.Load(modelPath);
        if (weapon == null)
        {
            //Debug.LogWarning($"Can't find weapon model at {modelPath}");
        }
        else
        {
            //Debug.Log($"Successfully loaded weapon {model} model.");
        }

        return weapon;
    }



}
