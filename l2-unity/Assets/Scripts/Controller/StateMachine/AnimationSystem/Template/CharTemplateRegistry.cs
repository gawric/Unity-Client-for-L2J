using Org.BouncyCastle.Utilities.Encoders;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public static class CharTemplateRegistry
{
    private static readonly Dictionary<int, BaseStatModel> Classes = new Dictionary<int, BaseStatModel>()
    {
        {
            (int)ClassId.FIGHTER,
            new BaseStatModel(
                new GenderStats(0.0047f, 0.0047f, 1f), // Male
                new GenderStats(0, 0, 0)  // Female
            )
        },
        {
            (int)ClassId.MAGE,
            new BaseStatModel(
                new GenderStats(0.0033f, 0.0047f, 1.8f), // Male (с бонусом 2H)
                new GenderStats(0.0035f, 0, 0) // Female
            )
        }
    };

    public static float GetRunSpeed(int classId, int sex, float serverValue, bool isTwoHanded)
    {
        
        if (Classes.TryGetValue(classId, out var model))
        {
            var stats = model.GetData(sex); // Берем Male или Female данные

            float speed = serverValue * stats.RunK;

            if (isTwoHanded)
                speed *= stats.TwoHandedBonus;

            return Mathf.Clamp(speed, 0.25f, 0.85f);
        }

        return 0.5f;
    }

    public static float GetWalkSpeed(int classId, int sex, float serverValue, bool isTwoHanded)
    {
        if (Classes.TryGetValue(classId, out var model))
        {
            var stats = model.GetData(sex);
            return Mathf.Clamp(serverValue * stats.WalkK, 0.2f, 0.7f);
        }

        return 0.3f;
    }
}
