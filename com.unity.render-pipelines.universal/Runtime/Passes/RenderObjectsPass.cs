using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Experimental.Rendering.Universal
{
    [MovedFrom("UnityEngine.Experimental.Rendering.LWRP")] public class RenderObjectsPass : ScriptableRenderPass
    {
        RenderQueueType renderQueueType;
        FilteringSettings m_FilteringSettings;
        RenderObjects.CustomCameraSettings m_CameraSettings;
        string m_ProfilerTag;
        ProfilingSampler m_ProfilingSampler;

        public Material overrideMaterial
        {
            get;
            set;
        }

        public int overrideMaterialPassIndex { get; set; }

        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        private Material m_DefaultOverrideMaterial;
        private Material m_ActiveOverrideMaterial;
        private int m_DefaultOverrideMaterialPass;
        private int m_ActiveOverrideMaterialPass;

        public void SetDetphState(bool writeEnabled, CompareFunction function = CompareFunction.Less)
        {
            m_RenderStateBlock.mask |= RenderStateMask.Depth;
            m_RenderStateBlock.depthState = new DepthState(writeEnabled, function);
        }

        public void SetStencilState(int reference, CompareFunction compareFunction, StencilOp passOp, StencilOp failOp, StencilOp zFailOp)
        {
            StencilState stencilState = StencilState.defaultValue;
            stencilState.enabled = true;
            stencilState.SetCompareFunction(compareFunction);
            stencilState.SetPassOperation(passOp);
            stencilState.SetFailOperation(failOp);
            stencilState.SetZFailOperation(zFailOp);

            m_RenderStateBlock.mask |= RenderStateMask.Stencil;
            m_RenderStateBlock.stencilReference = reference;
            m_RenderStateBlock.stencilState = stencilState;

            m_ActiveStateBlock = m_RenderStateBlock;
        }

        RenderStateBlock m_RenderStateBlock;
        RenderStateBlock m_DefaultStateBlock;
        RenderStateBlock m_ActiveStateBlock;

        public RenderObjectsPass(string profilerTag, RenderPassEvent renderPassEvent, string[] shaderTags, RenderQueueType renderQueueType, int layerMask, RenderObjects.CustomCameraSettings cameraSettings)
        {
            m_ProfilerTag = profilerTag;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            this.renderPassEvent = renderPassEvent;
            this.renderQueueType = renderQueueType;
            this.overrideMaterial = null;
            this.overrideMaterialPassIndex = 0;
            RenderQueueRange renderQueueRange = (renderQueueType == RenderQueueType.Transparent)
                ? RenderQueueRange.transparent
                : RenderQueueRange.opaque;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);

            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_DefaultStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_ActiveStateBlock = m_DefaultStateBlock;

            m_CameraSettings = cameraSettings;

            m_DefaultOverrideMaterial = null;
            m_ActiveOverrideMaterial = m_DefaultOverrideMaterial;
            m_DefaultOverrideMaterialPass = 0;
            m_ActiveOverrideMaterialPass = m_DefaultOverrideMaterialPass; 
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
            {
                m_ActiveStateBlock = m_DefaultStateBlock;
                m_ActiveOverrideMaterial = m_DefaultOverrideMaterial;
                m_ActiveOverrideMaterialPass = m_DefaultOverrideMaterialPass;
            }
            else
            {
                m_ActiveStateBlock = m_RenderStateBlock;
                m_ActiveOverrideMaterial = overrideMaterial;
                m_ActiveOverrideMaterialPass = overrideMaterialPassIndex;
            }

            SortingCriteria sortingCriteria = (renderQueueType == RenderQueueType.Transparent)
                ? SortingCriteria.CommonTransparent
                : renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
            drawingSettings.overrideMaterial = m_ActiveOverrideMaterial;
            drawingSettings.overrideMaterialPassIndex = m_ActiveOverrideMaterialPass;

            ref CameraData cameraData = ref renderingData.cameraData;

            Camera camera = cameraData.camera;

            // In case of camera stacking we need to take the viewport rect from base camera
            Rect pixelRect = renderingData.cameraData.pixelRect;
            float cameraAspect = (float) pixelRect.width / (float) pixelRect.height;
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                if (m_CameraSettings.overrideCamera && cameraData.isStereoEnabled)
                    Debug.LogWarning("RenderObjects pass is configured to override camera matrices. While rendering in stereo camera matrices cannot be overriden.");

                if (m_CameraSettings.overrideCamera && !cameraData.isStereoEnabled)
                {
                    Matrix4x4 projectionMatrix = Matrix4x4.Perspective(m_CameraSettings.cameraFieldOfView, cameraAspect, camera.nearClipPlane, camera.farClipPlane);
                    projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, cameraData.IsCameraProjectionMatrixFlipped());

                    Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
                    Vector4 cameraTranslation = viewMatrix.GetColumn(3);
                    viewMatrix.SetColumn(3, cameraTranslation + m_CameraSettings.offset);

                    RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings,
                    ref m_ActiveStateBlock);

                if (m_CameraSettings.overrideCamera && m_CameraSettings.restoreCamera && !cameraData.isStereoEnabled)
                {
                    RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), false);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
