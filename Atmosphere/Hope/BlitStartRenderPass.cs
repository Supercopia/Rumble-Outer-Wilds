using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OuterWildsRumble.Atmosphere.Hope;

public class BlitStartRenderPass : ScriptableRenderPass
{
    AtmosphereBlitData data;

    [HideFromIl2Cpp]
    public void Setup(AtmosphereBlitData blitData)
    {
        data = blitData;
    }
    
    public BlitStartRenderPass(IntPtr ptr) : base(ptr) { }

    public BlitStartRenderPass() 
        : base(ClassInjector.DerivedConstructorPointer<BlitStartRenderPass>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        MelonLogger.Msg("Executing BlitStartRenderPass");
        var cmd = CommandBufferPool.Get("AtmosphereStart");

        var renderer = renderingData.cameraData.renderer;
        var target = renderer.cameraColorTargetHandle;

        var desc = renderingData.cameraData.cameraTargetDescriptor;

        data.Init(desc);
        data.ExecuteBlitColor(cmd, target);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}