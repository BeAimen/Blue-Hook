using FishingGameTool.CustomAttribute;
using UnityEditor;
using UnityEngine;

namespace FishingGameTool.CustomDrawer
{
    [CustomPropertyDrawer(typeof(InfoBoxAttribute))]
    public class InfoBoxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InfoBoxAttribute infoAttribute = attribute as InfoBoxAttribute;
            float infoHeight = GetInfoHeight(infoAttribute);

            Rect propertyPosition = new Rect(position.x, position.y + infoHeight, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(propertyPosition, property, label);

            if (!string.IsNullOrEmpty(infoAttribute?._infoText))
            {
                Rect infoRect = new Rect(position.x, position.y, position.width, infoHeight - EditorGUIUtility.standardVerticalSpacing);
                EditorGUI.HelpBox(infoRect, infoAttribute._infoText, MessageType.Info);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InfoBoxAttribute infoAttribute = attribute as InfoBoxAttribute;
            float infoHeight = GetInfoHeight(infoAttribute);

            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            return propertyHeight + infoHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        private float GetInfoHeight(InfoBoxAttribute infoAttribute)
        {
            if (string.IsNullOrEmpty(infoAttribute?._infoText))
                return 0f;

            GUIStyle style = EditorStyles.helpBox;
            GUIContent content = new GUIContent(infoAttribute._infoText);
            float infoHeight = style.CalcHeight(content, EditorGUIUtility.currentViewWidth);

            float padding = EditorGUIUtility.singleLineHeight * 0.5f;
            return infoHeight + padding;
        }
    }
}
