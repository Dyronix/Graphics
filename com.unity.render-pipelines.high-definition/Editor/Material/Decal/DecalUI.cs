using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// GUI for HDRP Decal materials (does not include ShaderGraphs)
    /// </summary>
    class DecalUI : ShaderGUI
    {
        // Same hack as in HDShaderGUI but for some reason, this editor does not inherit from HDShaderGUI
        bool m_FirstFrame = true;

        [Flags]
        enum Expandable : uint
        {
            SurfaceOptions = 1 << 0,
            SurfaceInputs = 1 << 1,
            Sorting = 1 << 2,
        }

        MaterialUIBlockList uiBlocks = new MaterialUIBlockList
        {
            new DecalSurfaceOptionsUIBlock((MaterialUIBlock.Expandable)Expandable.SurfaceOptions),
            new DecalSurfaceInputsUIBlock((MaterialUIBlock.Expandable)Expandable.SurfaceInputs),
            new DecalSortingInputsUIBlock((MaterialUIBlock.Expandable)Expandable.Sorting),
        };

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            LoadMaterialProperties(props);

            SerializedProperty instancing = materialEditor.serializedObject.FindProperty("m_EnableInstancingVariants");
            instancing.boolValue = true;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                uiBlocks.OnGUI(materialEditor, props);

                var surfaceInputs = uiBlocks.FetchUIBlock< DecalSurfaceInputsUIBlock >();

                // Apply material keywords and pass:
                if (changed.changed || m_FirstFrame)
                {
                    m_FirstFrame = false;

                    normalBlendSrc.floatValue = surfaceInputs.normalBlendSrcValue;
                    maskBlendSrc.floatValue = surfaceInputs.maskBlendSrcValue;
                    maskBlendMode.floatValue = (float)surfaceInputs.maskBlendFlags;
                    smoothnessRemapMin.floatValue = surfaceInputs.smoothnessRemapMinValue;
                    smoothnessRemapMax.floatValue = surfaceInputs.smoothnessRemapMaxValue;
                    AORemapMin.floatValue = surfaceInputs.AORemapMinValue;
                    AORemapMax.floatValue = surfaceInputs.AORemapMaxValue;
                    if (useEmissiveIntensity.floatValue == 1.0f)
                    {
                        emissiveColor.colorValue = emissiveColorLDR.colorValue * emissiveIntensity.floatValue;
                    }
                    else
                    {
                        emissiveColor.colorValue = emissiveColorHDR.colorValue;
                    }

                    foreach (var material in uiBlocks.materials)
                        SetupMaterialKeywordsAndPassInternal(material);
                }
            }
            materialEditor.serializedObject.ApplyModifiedProperties();
        }

        enum BlendSource
        {
            BaseColorMapAlpha,
            MaskMapBlue
        }
        protected const string kBaseColorMap = "_BaseColorMap";

        protected const string kBaseColor = "_BaseColor";

        protected const string kNormalMap = "_NormalMap";

        protected const string kMaskMap = "_MaskMap";

        protected const string kDecalBlend = "_DecalBlend";

        protected MaterialProperty normalBlendSrc = new MaterialProperty();
        protected const string kNormalBlendSrc = "_NormalBlendSrc";

        protected MaterialProperty maskBlendSrc = new MaterialProperty();
        protected const string kMaskBlendSrc = "_MaskBlendSrc";

        protected MaterialProperty maskBlendMode = new MaterialProperty();
        protected const string kMaskBlendMode = "_MaskBlendMode";

        protected const string kMaskmapMetal = "_MaskmapMetal";

        protected const string kMaskmapAO = "_MaskmapAO";

        protected const string kMaskmapSmoothness = "_MaskmapSmoothness";

        protected const string kDecalMeshDepthBias = "_DecalMeshDepthBias";

        protected const string kDrawOrder = "_DrawOrder";

        protected MaterialProperty AORemapMin = new MaterialProperty();
        protected const string kAORemapMin = "_AORemapMin";

        protected MaterialProperty AORemapMax = new MaterialProperty();
        protected const string kAORemapMax = "_AORemapMax";

        protected MaterialProperty smoothnessRemapMin = new MaterialProperty();
        protected const string kSmoothnessRemapMin = "_SmoothnessRemapMin";

        protected MaterialProperty smoothnessRemapMax = new MaterialProperty();
        protected const string kSmoothnessRemapMax = "_SmoothnessRemapMax";

        protected const string kMetallicScale = "_MetallicScale";

        protected const string kMaskMapBlueScale = "_DecalMaskMapBlueScale";

        protected MaterialProperty emissiveColor = new MaterialProperty();
        protected const string kEmissiveColor = "_EmissiveColor";

        protected MaterialProperty emissiveColorMap = new MaterialProperty();
        protected const string kEmissiveColorMap = "_EmissiveColorMap";

        protected MaterialProperty emissiveIntensity = null;
        protected const string kEmissiveIntensity = "_EmissiveIntensity";

        protected const string kEmissiveIntensityUnit = "_EmissiveIntensityUnit";

        protected MaterialProperty useEmissiveIntensity = null;
        protected const string kUseEmissiveIntensity = "_UseEmissiveIntensity";

        protected MaterialProperty emissiveColorLDR = null;
        protected const string kEmissiveColorLDR = "_EmissiveColorLDR";

        protected MaterialProperty emissiveColorHDR = null;
        protected const string kEmissiveColorHDR = "_EmissiveColorHDR";

        void LoadMaterialProperties(MaterialProperty[] properties)
        {
            normalBlendSrc = FindProperty(kNormalBlendSrc, properties);
            maskBlendSrc = FindProperty(kMaskBlendSrc, properties);
            maskBlendMode = FindProperty(kMaskBlendMode, properties);
            AORemapMin = FindProperty(kAORemapMin, properties);
            AORemapMax = FindProperty(kAORemapMax, properties);
            smoothnessRemapMin = FindProperty(kSmoothnessRemapMin, properties);
            smoothnessRemapMax = FindProperty(kSmoothnessRemapMax, properties);
            emissiveColor = FindProperty(kEmissiveColor, properties);
            emissiveColorMap = FindProperty(kEmissiveColorMap, properties);
            useEmissiveIntensity = FindProperty(kUseEmissiveIntensity, properties);
            emissiveIntensity = FindProperty(kEmissiveIntensity, properties);
            emissiveColorLDR = FindProperty(kEmissiveColorLDR, properties);
            emissiveColorHDR = FindProperty(kEmissiveColorHDR, properties);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupCommonDecalMaterialKeywordsAndPass(Material material)
        {
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_ALBEDO", material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_NORMAL", material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f);
            CoreUtils.SetKeyword(material, "_MATERIAL_AFFECTS_MASKMAP", material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f);

            // Albedo : RT0 RGB, A - sRGB
            // Normal : RT1 RGB, A
            // Smoothness: RT2 B, A
            // Metal: RT2 R, RT3 R
            // AO: RT2 G, RT3 G
            // Note RT3 is only RG
            ColorWriteMask mask0 = 0, mask1 = 0, mask2 = 0, mask3 = 0;

            if (material.HasProperty(kAffectAlbedo) && material.GetFloat(kAffectAlbedo) == 1.0f)
                mask0 |= ColorWriteMask.All;
            if (material.HasProperty(kAffectNormal) && material.GetFloat(kAffectNormal) == 1.0f)
                mask1 |= ColorWriteMask.All;
            if (material.HasProperty(kAffectMetal) && material.GetFloat(kAffectMetal) == 1.0f)
                mask2 |= mask3 |= ColorWriteMask.Red;
            if (material.HasProperty(kAffectAO) && material.GetFloat(kAffectAO) == 1.0f)
                mask2 |= mask3 |= ColorWriteMask.Green;
            if (material.HasProperty(kAffectSmoothness) && material.GetFloat(kAffectSmoothness) == 1.0f)
                mask2 |= ColorWriteMask.Blue | ColorWriteMask.Alpha;

            material.SetInt(HDShaderIDs._DecalColorMask0, (int)mask0);
            material.SetInt(HDShaderIDs._DecalColorMask1, (int)mask1);
            material.SetInt(HDShaderIDs._DecalColorMask2, (int)mask2);
            material.SetInt(HDShaderIDs._DecalColorMask3, (int)mask3);

            // First reset the pass (in case new shader graph add or remove a pass)
            material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, true);
            material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, true);

            // Then disable pass is they aren't needed
            if (material.FindPass(HDShaderPassNames.s_DBufferProjectorStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferProjectorStr, ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0);
            if (material.FindPass(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalProjectorForwardEmissiveStr, material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f);
            if (material.FindPass(HDShaderPassNames.s_DBufferMeshStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DBufferMeshStr, ((int)mask0 + (int)mask1 + (int)mask2 + (int)mask3) != 0);
            if (material.FindPass(HDShaderPassNames.s_DecalMeshForwardEmissiveStr) != -1)
                material.SetShaderPassEnabled(HDShaderPassNames.s_DecalMeshForwardEmissiveStr, material.HasProperty(kAffectEmission) && material.GetFloat(kAffectEmission) == 1.0f);

            // Set stencil state
            material.SetInt(kDecalStencilWriteMask, (int)StencilUsage.Decals);
            material.SetInt(kDecalStencilRef, (int)StencilUsage.Decals);
        }

        // All Setup Keyword functions must be static. It allow to create script to automatically update the shaders with a script if code change
        static public void SetupMaterialKeywordsAndPass(Material material)
        {
            // Setup color mask properties
            SetupCommonDecalMaterialKeywordsAndPass(material);

            CoreUtils.SetKeyword(material, "_COLORMAP", material.GetTexture(kBaseColorMap));
            CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture(kNormalMap));
            CoreUtils.SetKeyword(material, "_MASKMAP", material.GetTexture(kMaskMap));
            CoreUtils.SetKeyword(material, "_EMISSIVEMAP", material.GetTexture(kEmissiveColorMap));
        }

        //protected override void SetupMaterialKeywordsAndPassInternal(Material material)
        void SetupMaterialKeywordsAndPassInternal(Material material)
        {
            SetupMaterialKeywordsAndPass(material);
        }
    }
}
