using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Shader shader;
        [Range(1, 10)] public int scale = 1;
        public Color color = Color.white;
        [Range(0, 10)] public float depthThreshold = 1.5f;
        [Range(0, 1)] public float depthNormalThreshold = 0.5f;
        [Range(0, 10)] public float depthNormalThresholdScale = 7f;
        [Range(0, 1)] public float normalThreshold = 0.4f;
    }

    public Settings settings = new Settings();
    OutlinePass pass;

    public override void Create()
    {
        pass = new OutlinePass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.shader == null) return;
        renderer.EnqueuePass(pass);
    }
}

public class OutlinePass : ScriptableRenderPass
{
    OutlineRendererFeature.Settings s;
    Material mat;
    RTHandle tempRT;

    static readonly int scaleProp = Shader.PropertyToID("_Scale");
    static readonly int colorProp = Shader.PropertyToID("_Color");
    static readonly int depthThreshProp = Shader.PropertyToID("_DepthThreshold");
    static readonly int depthNormThreshProp = Shader.PropertyToID("_DepthNormalThreshold");
    static readonly int depthNormThreshScaleProp = Shader.PropertyToID("_DepthNormalThresholdScale");
    static readonly int normalThreshProp = Shader.PropertyToID("_NormalThreshold");
    static readonly int clipToViewProp = Shader.PropertyToID("_ClipToView");

    public OutlinePass(OutlineRendererFeature.Settings settings)
    {
        s = settings;
        renderPassEvent = s.renderPassEvent;
        mat = CoreUtils.CreateEngineMaterial(s.shader);
        requiresIntermediateTexture = true;
        ConfigureInput(ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Depth);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();
        var cameraData = frameData.Get<UniversalCameraData>();

        if (resourceData.isActiveTargetBackBuffer) return;

        var desc = cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        desc.msaaSamples = 1;

        var cam = cameraData.camera;
        mat.SetFloat(scaleProp, s.scale);
        mat.SetColor(colorProp, s.color);
        mat.SetFloat(depthThreshProp, s.depthThreshold);
        mat.SetFloat(depthNormThreshProp, s.depthNormalThreshold);
        mat.SetFloat(depthNormThreshScaleProp, s.depthNormalThresholdScale);
        mat.SetFloat(normalThreshProp, s.normalThreshold);
        Matrix4x4 clipToView = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true).inverse;
        mat.SetMatrix(clipToViewProp, clipToView);

        TextureHandle src = resourceData.activeColorTexture;

        TextureHandle dst = renderGraph.CreateTexture(new TextureDesc(desc)
        {
            name = "_OutlineTemp",
            clearBuffer = false
        });

        RenderGraphUtils.BlitMaterialParameters para = new(src, dst, mat, 0);
        renderGraph.AddBlitPass(para, passName: "Outline");

        resourceData.cameraColor = dst;
    }
}
