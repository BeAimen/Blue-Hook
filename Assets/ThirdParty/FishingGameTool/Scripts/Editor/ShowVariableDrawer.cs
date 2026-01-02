using UnityEditor;
using UnityEngine;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.CustomDrawer
{
    [CustomPropertyDrawer(typeof(ShowVariableAttribute))]
    public class ShowVariableDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowVariableAttribute showVariableAttribute = (ShowVariableAttribute)attribute;

            if (GetConditionalAttributeResult(showVariableAttribute, property))
                return EditorGUI.GetPropertyHeight(property, label);
            else
                return -EditorGUIUtility.standardVerticalSpacing;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowVariableAttribute showVariableAttribute = (ShowVariableAttribute)attribute;

            if (GetConditionalAttributeResult(showVariableAttribute, property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public bool GetConditionalAttributeResult(ShowVariableAttribute attribute, SerializedProperty property)
        {
            bool enabled = true;

            string[] boolPropertyPathArray = property.propertyPath.Split('.');
            boolPropertyPathArray[boolPropertyPathArray.Length - 1] = attribute._boolProperty;
            string boolPropertyPath = string.Join(".", boolPropertyPathArray);

            SerializedProperty propertyValue = property.serializedObject.FindProperty(boolPropertyPath);

            if (propertyValue != null)
                enabled = propertyValue.boolValue;
            else
                Debug.LogWarning("Conditional Attribute not found!");

            return enabled;
        }
    }
}
