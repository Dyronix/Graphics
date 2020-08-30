using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEditor.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Renderer2DData), true)]
    internal class Renderer2DDataEditor : ScriptableRendererDataEditor
    {
        ////-----------------------------------------------------------------------------------------
        //Sorting
        //    Transparency Sort Mode
        //    Transparency Sort Axis
        //
        ////-----------------------------------------------------------------------------------------
        //Rendering
        //    HRD Emulation Scale
        //
        //    Default Material Type
        //    Custom Material
        //
        ////-----------------------------------------------------------------------------------------
        //Blending
        //    Light Blend Styles
        //        Default
        //        Blend Style 1
        //        Blend Style 2
        //        Blend Style 3
        //        Blend Style 4
        //
        ////-----------------------------------------------------------------------------------------
        //Overrides
        //    Use Depth/Stencil Buffer
        //
        ////-----------------------------------------------------------------------------------------
        //Misc
        //    Post Processing Data
        ////-----------------------------------------------------------------------------------------
        //Render Passes
        //    Render Pass
        //    Render Pass Map
        //

        private class Styles
        {
            //-----------------------------------------------------------------------------------------
            // Sorting
            public static readonly GUIContent transparencySortMode      = EditorGUIUtility.TrTextContent("Transparency Sort Mode", "Default sorting mode used for transparent objects");
            public static readonly GUIContent transparencySortAxis      = EditorGUIUtility.TrTextContent("Transparency Sort Axis", "Axis used for custom axis sorting mode");

            //-----------------------------------------------------------------------------------------
            // Rendering
            public static readonly GUIContent transparentMask           = EditorGUIUtility.TrTextContent("Transparent Layer Mask", "Controls which transparent layers this renderer draws.");
            public static readonly GUIContent hdrEmulationScale         = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");

            public static readonly GUIContent defaultMaterialType       = EditorGUIUtility.TrTextContent("Default Material Type", "Material to use when adding new objects to a scene");
            public static readonly GUIContent defaultCustomMaterial     = EditorGUIUtility.TrTextContent("Default Custom Material", "Material to use when adding new objects to a scene");

            //-----------------------------------------------------------------------------------------
            // Blending
            public static readonly GUIContent lightBlendStyles          = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent name                      = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel        = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent renderTextureScale        = EditorGUIUtility.TrTextContent("Render Texture Scale", "The resolution of the lighting buffer relative to the screen resolution. 1.0 means full screen size.");
            public static readonly GUIContent blendMode                 = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent customBlendFactors        = EditorGUIUtility.TrTextContent("Custom Blend Factors");
            public static readonly GUIContent blendFactorMultiplicative = EditorGUIUtility.TrTextContent("Multiplicative");
            public static readonly GUIContent blendFactorAdditive       = EditorGUIUtility.TrTextContent("Additive");

            //-----------------------------------------------------------------------------------------
            // Overrides
            public static readonly GUIContent useDepthStencilBuffer = EditorGUIUtility.TrTextContent("Use Depth/Stencil Buffer", "Uncheck this when you are certain you don't use any feature that requires the depth/stencil buffer (e.g. Sprite Mask). Not using the depth/stencil buffer may improve performance, especially on mobile platforms.");

            //-----------------------------------------------------------------------------------------
            // Misc
            public static readonly GUIContent postProcessData = EditorGUIUtility.TrTextContent("Post-processing Data", "Resources (textures, shaders, etc.) required by post-processing effects.");
        }

        private struct LightBlendStyleProps
        {
            public SerializedProperty name;
            public SerializedProperty maskTextureChannel;
            public SerializedProperty renderTextureScale;
            public SerializedProperty blendMode;
            public SerializedProperty blendFactorMultiplicative;
            public SerializedProperty blendFactorAdditive;
        }

        // Sorting
        private SerializedProperty m_TransparencySortMode;
        private SerializedProperty m_TransparencySortAxis;

        // Rendering
        private SerializedProperty m_TransparentLayerMask;
        private SerializedProperty m_HDREmulationScale;
        private SerializedProperty m_DefaultMaterialType;
        private SerializedProperty m_DefaultCustomMaterial;

        // Blending
        private SerializedProperty m_LightBlendStyles;
        private LightBlendStyleProps[] m_LightBlendStylePropsArray;

        // Overrides
        private SerializedProperty m_UseDepthStencilBuffer;

        // Misc
        private SerializedProperty m_PostProcessData;

        //-----------------------------------------------------------------------------------------
        // Fields
        private Analytics.Renderer2DAnalytics m_Analytics;
        private Renderer2DData m_Renderer2DData;
        private bool m_WasModified;

        #region Unity Messages

        //-----------------------------------------------------------------------------------------
        private void OnEnable()
        {
            m_Analytics                     = Analytics.Renderer2DAnalytics.instance;
            m_Renderer2DData                = (Renderer2DData)serializedObject.targetObject;
            m_WasModified                   = false;

            InitializeSortingProperties();
            InitializeRenderingProperties();
            InitializeBlendingProperties();
            InitializeOverrideProperties();
            InitializeMiscellaneousProperties();
        }
        //-----------------------------------------------------------------------------------------
        private void OnDestroy()
        {
            SendModifiedAnalytics(m_Analytics);
        }

        //-----------------------------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            //-----------------------------------------------------------------------------------------
            // Initialize
            serializedObject.Update();

            //-----------------------------------------------------------------------------------------
            // Sorting
            EditorGUILayout.LabelField("Sorting", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_TransparencySortMode, Styles.transparencySortMode);
            if(m_TransparencySortMode.intValue == (int)TransparencySortMode.CustomAxis)
                EditorGUILayout.PropertyField(m_TransparencySortAxis, Styles.transparencySortAxis);
            EditorGUI.indentLevel--;

            //-----------------------------------------------------------------------------------------
            // Rendering
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.transparentMask);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && m_HDREmulationScale.floatValue < 1.0f)
                m_HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.PropertyField(m_DefaultMaterialType, Styles.defaultMaterialType);
            if (m_DefaultMaterialType.intValue == (int)Renderer2DData.Renderer2DDefaultMaterialType.Custom)
                EditorGUILayout.PropertyField(m_DefaultCustomMaterial, Styles.defaultCustomMaterial);
            EditorGUI.indentLevel--;

            //-----------------------------------------------------------------------------------------
            // Blending
            EditorGUILayout.LabelField("Blending", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();

            int num_blend_styles = m_LightBlendStyles.arraySize;
            for (int i = 0; i < num_blend_styles; ++i)
            {
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                SerializedProperty blend_style_prop = m_LightBlendStyles.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginHorizontal();

                blend_style_prop.isExpanded = EditorGUILayout.Foldout(blend_style_prop.isExpanded, props.name.stringValue, true);

                EditorGUILayout.EndHorizontal();

                if (blend_style_prop.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(props.name, Styles.name);
                    EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                    EditorGUILayout.PropertyField(props.renderTextureScale, Styles.renderTextureScale);
                    EditorGUILayout.PropertyField(props.blendMode, Styles.blendMode);

                    if (props.blendMode.intValue == (int)Light2DBlendStyle.BlendMode.Custom)
                    {
                        EditorGUILayout.BeginHorizontal();

                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField(Styles.customBlendFactors, GUILayout.MaxWidth(200.0f));
                        EditorGUI.indentLevel--;

                        int old_indent_level = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;

                        EditorGUIUtility.labelWidth = 80.0f;
                        EditorGUILayout.PropertyField(props.blendFactorMultiplicative, Styles.blendFactorMultiplicative, GUILayout.MinWidth(110.0f));

                        GUILayout.Space(10.0f);

                        EditorGUIUtility.labelWidth = 50.0f;
                        EditorGUILayout.PropertyField(props.blendFactorAdditive, Styles.blendFactorAdditive, GUILayout.MinWidth(90.0f));

                        EditorGUIUtility.labelWidth = 0.0f;
                        EditorGUI.indentLevel = old_indent_level;
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            EditorGUI.EndChangeCheck();

            //-----------------------------------------------------------------------------------------
            // Overrides
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_UseDepthStencilBuffer, Styles.useDepthStencilBuffer);
            EditorGUI.indentLevel--;

            //-----------------------------------------------------------------------------------------
            // Misc
            EditorGUILayout.LabelField("Miscellaneous", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.postProcessData);
            EditorGUI.indentLevel--;

            //-----------------------------------------------------------------------------------------
            // Apply changes
            m_WasModified |= serializedObject.hasModifiedProperties;
            if (m_WasModified)
                serializedObject.ApplyModifiedProperties();

            //-----------------------------------------------------------------------------------------
            // Base implementation
            base.OnInspectorGUI();
        }

        #endregion

        //-----------------------------------------------------------------------------------------
        private void InitializeSortingProperties()
        {
            m_TransparencySortMode = serializedObject.FindProperty("m_TransparencySortMode");
            m_TransparencySortAxis = serializedObject.FindProperty("m_TransparencySortAxis");
        }
        //-----------------------------------------------------------------------------------------
        private void InitializeRenderingProperties()
        {
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_HDREmulationScale = serializedObject.FindProperty("m_HDREmulationScale");
            m_DefaultMaterialType = serializedObject.FindProperty("m_DefaultMaterialType");
            m_DefaultCustomMaterial = serializedObject.FindProperty("m_DefaultCustomMaterial");
        }
        //-----------------------------------------------------------------------------------------
        private void InitializeBlendingProperties()
        {
            m_LightBlendStyles = serializedObject.FindProperty("m_LightBlendStyles");
            m_LightBlendStylePropsArray = new LightBlendStyleProps[m_LightBlendStyles.arraySize];

            for (int i = 0; i < m_LightBlendStylePropsArray.Length; ++i)
            {
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                SerializedProperty blend_style_prop = m_LightBlendStyles.GetArrayElementAtIndex(i);

                props.name = blend_style_prop.FindPropertyRelative("name");
                props.maskTextureChannel = blend_style_prop.FindPropertyRelative("maskTextureChannel");
                props.renderTextureScale = blend_style_prop.FindPropertyRelative("renderTextureScale");
                props.blendMode = blend_style_prop.FindPropertyRelative("blendMode");
                props.blendFactorMultiplicative = blend_style_prop.FindPropertyRelative("customBlendFactors.multiplicative");
                props.blendFactorAdditive = blend_style_prop.FindPropertyRelative("customBlendFactors.additive");

                if (props.blendFactorMultiplicative == null)
                {
                    props.blendFactorMultiplicative = blend_style_prop.FindPropertyRelative("customBlendFactors.modulate");
                }
                if (props.blendFactorAdditive == null)
                {
                    props.blendFactorAdditive = blend_style_prop.FindPropertyRelative("customBlendFactors.additve");
                }
            }
        }
        //-----------------------------------------------------------------------------------------
        private void InitializeOverrideProperties()
        {
            m_UseDepthStencilBuffer = serializedObject.FindProperty("m_UseDepthStencilBuffer");
        }
        //-----------------------------------------------------------------------------------------
        private void InitializeMiscellaneousProperties()
        {
            m_PostProcessData = serializedObject.FindProperty("m_PostProcessData");
        }

        //-----------------------------------------------------------------------------------------
        private void SendModifiedAnalytics(Analytics.IAnalytics analytics)
        {
            if (m_WasModified)
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.instance_id = m_Renderer2DData.GetInstanceID();
                modifiedData.was_create_event = false;
                modifiedData.blending_layers_count = 0;
                modifiedData.blending_modes_used = 0;
                analytics.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            }
        }
    }
}
