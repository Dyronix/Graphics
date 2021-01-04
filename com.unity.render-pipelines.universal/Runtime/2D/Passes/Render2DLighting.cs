using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class Render2DLighting : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Render2DLightSettings
        {
            public string passTag = "Render2DLightFeature";
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingOpaques;
            public Renderer2DData Data = null;
        }

        public Render2DLightSettings settings = new Render2DLightSettings();

        Render2DLightingPass renderLightPass;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if(renderLightPass == null)
            {
                return;
            }

            renderer.EnqueuePass(renderLightPass);
        }

        public override void Create()
        {
            if(settings.Data == null)
            {
                return;
            }

            renderLightPass = new Render2DLightingPass(settings.Data, settings.Event);
        }
    }
}
