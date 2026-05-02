using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SwordSetup))]
public class SwordEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Отрисовка стандартных полей (чтобы видеть ссылки на точки)
        DrawDefaultInspector();

        SwordSetup script = (SwordSetup)target;

        GUILayout.Space(10); // Отступ

        if (GUILayout.Button("Добавить/Обновить точки меча", GUILayout.Height(30)))
        {
            // Позволяет отменить действие через Ctrl+Z
            Undo.RegisterCreatedObjectUndo(script.gameObject, "Setup Sword Points");

            script.SetupPoints();

            // Помечаем объект как "измененный", чтобы изменения сохранились в сцене/префабе
            EditorUtility.SetDirty(script);
        }
    }
}