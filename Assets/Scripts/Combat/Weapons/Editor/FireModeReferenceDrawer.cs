using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Custom inspector drawer for [SerializeReference] FireModeBase fields.
// Default Unity rendering shows no type picker — this drawer adds a dropdown
// listing all non-abstract FireModeBase subclasses, plus draws the inner fields
// of the chosen concrete type.
[CustomPropertyDrawer(typeof(FireModeBase), true)]
public class FireModeReferenceDrawer : PropertyDrawer
{
    private static List<Type> cachedTypes;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnsureCache();

        Rect headerRect = new Rect(
            position.x,
            position.y,
            position.width,
            EditorGUIUtility.singleLineHeight);
        Rect labelRect = new Rect(
            headerRect.x,
            headerRect.y,
            EditorGUIUtility.labelWidth,
            headerRect.height);
        Rect dropdownRect = new Rect(
            headerRect.x + EditorGUIUtility.labelWidth,
            headerRect.y,
            headerRect.width - EditorGUIUtility.labelWidth,
            headerRect.height);

        EditorGUI.LabelField(labelRect, label);

        object refValue = property.managedReferenceValue;
        string currentName = refValue == null ? "(None)" : refValue.GetType().Name;

        if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(currentName), FocusType.Keyboard))
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("(None)"), refValue == null, () =>
            {
                property.managedReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            });
            menu.AddSeparator(string.Empty);
            foreach (Type t in cachedTypes)
            {
                Type captured = t;
                bool isSelected = refValue != null && refValue.GetType() == t;
                menu.AddItem(new GUIContent(t.Name), isSelected, () =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(captured);
                    property.serializedObject.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        if (refValue != null)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + 2;

            SerializedProperty iter = property.Copy();
            SerializedProperty end = iter.GetEndProperty();
            if (iter.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iter, end))
                {
                    float h = EditorGUI.GetPropertyHeight(iter, true);
                    Rect r = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(r, iter, true);
                    y += h + 2;
                    if (!iter.NextVisible(false)) break;
                }
            }

            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.managedReferenceValue != null)
        {
            SerializedProperty iter = property.Copy();
            SerializedProperty end = iter.GetEndProperty();
            if (iter.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iter, end))
                {
                    height += EditorGUI.GetPropertyHeight(iter, true) + 2;
                    if (!iter.NextVisible(false)) break;
                }
            }
        }
        return height;
    }

    private static void EnsureCache()
    {
        if (cachedTypes != null) return;
        cachedTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t => t.IsSubclassOf(typeof(FireModeBase)) && !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();
    }
}
