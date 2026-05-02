using System.Collections.Generic;
using UnityEngine;

public static class RegisteredBillboards
{
    static readonly List<IRegisteredBillboard> s_items = new List<IRegisteredBillboard>();

    public static void Register(IRegisteredBillboard item)
    {
        if (item == null) return;
        if (!s_items.Contains(item)) s_items.Add(item);
    }

    public static void Unregister(IRegisteredBillboard item)
    {
        if (item == null) return;
        s_items.Remove(item);
    }

    public static void InvokeAll(Camera camera)
    {
        // Итерируем по копии или по индексу, чтобы не ломалось при динамической регистрации/удалении
        for (int i = 0; i < s_items.Count; i++)
        {
            s_items[i].OnCameraPreRender(camera);
        }
    }
}