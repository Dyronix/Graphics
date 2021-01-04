using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.Rendering.Universal;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(Render2DLighting.Render2DLightSettings), true)]
    internal class Render2DLightPassFeatureEditor : PropertyDrawer
    {
        internal class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static GUIContent callback = new GUIContent("Event", "Choose at which point this render pass is executed in the frame.");

            public static GUIContent renderer2DData = new GUIContent("Data", "The 2D Renderer Data Asset contains the settings that affect the way Light is applied to lit Sprites.");
        }

        private SerializedProperty m_Callback;
        private SerializedProperty m_PassTag;

        // Renderer 2D Data
        private SerializedProperty m_Data;

        private List<SerializedObject> m_properties = new List<SerializedObject>();

        private void Init(SerializedProperty property)
        {
            m_Callback = property.FindPropertyRelative("Event");
            m_PassTag  = property.FindPropertyRelative("passTag");

            // Renderer 2D Data
            m_Data = property.FindPropertyRelative("Data");

            m_properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(rect, label, property);

            if (!m_properties.Contains(property.serializedObject))
            {
                Init(property);
            }

            var passName = property.serializedObject.FindProperty("m_Name").stringValue;
            if (passName != m_PassTag.stringValue)
            {
                m_PassTag.stringValue = passName;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.PropertyField(rect, m_Callback, Styles.callback);
            rect.y += Styles.defaultLineSpace;

            EditorGUI.PropertyField(rect, m_Data, Styles.renderer2DData);
            rect.y += Styles.defaultLineSpace;

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = Styles.defaultLineSpace;

            Init(property);

            height += Styles.defaultLineSpace; // add line for render 2D data

            return height;
        }
    }
}
