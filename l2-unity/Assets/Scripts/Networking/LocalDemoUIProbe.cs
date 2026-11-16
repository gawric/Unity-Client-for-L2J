using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

// Read-only diagnostics for an opt-in local test session on the pinned Unity version.
public sealed class LocalDemoUIProbe : MonoBehaviour
{
    const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    bool _dumped;

    void OnEnable()
    {
        Application.logMessageReceived += OnLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
    }

    void OnLog(string message, string stack, LogType type)
    {
        if (_dumped || !message.StartsWith("Assertion failed", StringComparison.Ordinal))
            return;
        _dumped = true;
        try
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("[LocalDemoUI] assertion type=" + type + " documents=" + documents.Length + " renderDataField=" + (typeof(VisualElement).GetField("renderChainData", InstanceMembers) != null));
            foreach (UIDocument document in documents)
                Dump(document.rootVisualElement, document.name);
        }
        catch (Exception error)
        {
            Debug.Log("[LocalDemoUI] inspection failed: " + error.GetType().Name + " " + error.Message);
        }
    }

    static object Read(object owner, string name)
    {
        Type type = owner.GetType();
        FieldInfo field = type.GetField(name, InstanceMembers);
        if (field != null)
            return field.GetValue(owner);
        PropertyInfo property = type.GetProperty(name, InstanceMembers);
        return property != null ? property.GetValue(owner) : null;
    }

    static void Dump(VisualElement element, string path)
    {
        if (element == null)
            return;
        path += "/" + element.GetType().Name + "#" + element.name;
        FieldInfo field = typeof(VisualElement).GetField("renderChainData", InstanceMembers);
        object data = field != null ? field.GetValue(element) : null;
        if (data != null && !Equals(Read(data, "isInChain"), false))
        {
            object clip = Read(data, "clipMethod");
            object transform = Read(data, "transformID");
            object parent = element.parent != null ? field.GetValue(element.parent) : null;
            MethodInfo allocates = data.GetType().GetMethod("AllocatesID", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            bool owns = allocates != null && (bool)allocates.Invoke(null, new[] { transform });
            bool group = Equals(Read(data, "isGroupTransform"), true);
            bool mismatch = parent != null && !owns && !group && !Equals(transform, Read(parent, "transformID"));
            if (Convert.ToString(clip) == "Undetermined" || mismatch)
                Debug.Log("[LocalDemoUI] candidate=" + path + " clip=" + clip + " transformMismatch=" + mismatch + " hints=" + element.usageHints);
        }
        for (int i = 0; i < element.hierarchy.childCount; i++)
            Dump(element.hierarchy[i], path);
    }
}
