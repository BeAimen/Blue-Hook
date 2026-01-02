using UnityEditor;
using UnityEngine;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.CustomDrawer
{
    [CustomPropertyDrawer(typeof(AddButtonAttribute))]
    public class AddButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AddButtonAttribute buttonAttribute = (AddButtonAttribute)attribute;

            Rect buttonRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (GUI.Button(buttonRect, buttonAttribute._buttonLabel))
            {
                SerializedProperty targetBoolProperty = property.serializedObject.FindProperty(buttonAttribute._boolProperty);
                if (targetBoolProperty != null && targetBoolProperty.propertyType == SerializedPropertyType.Boolean)
                {
                    targetBoolProperty.boolValue = !targetBoolProperty.boolValue;
                    targetBoolProperty.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
