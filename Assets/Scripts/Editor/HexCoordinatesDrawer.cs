using HexMap;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(type: typeof(HexCoordinates))]
public class HexCoordinatesDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position, SerializedProperty property, GUIContent label
    )
    {
        var coordinates = new HexCoordinates(
            x: property.FindPropertyRelative(relativePropertyPath: "x").intValue,
            z: property.FindPropertyRelative(relativePropertyPath: "z").intValue
        );

        position = EditorGUI.PrefixLabel(totalPosition: position, label: label);
        GUI.Label(position: position, text: coordinates.ToString());
    }
}