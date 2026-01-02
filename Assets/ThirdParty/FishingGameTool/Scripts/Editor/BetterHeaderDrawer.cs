using UnityEditor;
using UnityEngine;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.CustomDrawer
{
    [CustomPropertyDrawer(typeof(BetterHeaderAttribute))]
    public class BetterHeaderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            BetterHeaderAttribute headerAttribute = (BetterHeaderAttribute)attribute;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = headerAttribute._fontSize;

            GUIContent headerLabel = new GUIContent(headerAttribute._headerText);
            Vector2 headerSize = style.CalcSize(headerLabel);

            Rect headerRect = new Rect(position.x, position.y, position.width, headerSize.y);
            EditorGUI.LabelField(headerRect, headerLabel, style);

            Rect lineRect = new Rect(position.x, position.y + headerSize.y + EditorGUIUtility.standardVerticalSpacing, position.width, 1f);
            EditorGUI.DrawRect(lineRect, Color.grey);

            Rect propertyRect = new Rect(position.x, position.y + headerSize.y + EditorGUIUtility.standardVerticalSpacing + 2f, position.width,
                position.height - headerSize.y - EditorGUIUtility.standardVerticalSpacing - 2f);
            EditorGUI.PropertyField(propertyRect, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            BetterHeaderAttribute headerAttribute = (BetterHeaderAttribute)attribute;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = headerAttribute._fontSize;

            GUIContent headerLabel = new GUIContent(headerAttribute._headerText);
            Vector2 headerSize = style.CalcSize(headerLabel);

            float headerHeight = headerSize.y + EditorGUIUtility.standardVerticalSpacing + 2f;
            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);

            return headerHeight + propertyHeight;
        }
    }
}
