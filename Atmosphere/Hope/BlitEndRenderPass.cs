using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OuterWildsRumble.Atmosphere.Hope;


public class BlitEndRenderPass : ScriptableRenderPass
{
    AtmosphereBlitData data;
    [HideFromIl2Cpp]
    public void Setup(AtmosphereBlitData blitData)
    {
        data = blitData;
    }
    
    public BlitEndRenderPass(IntPtr ptr) : base(ptr) { }

    public BlitEndRenderPass() 
        : base(ClassInjector.DerivedConstructorPointer<BlitEndRenderPass>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        MelonLogger.Msg("Running BlitEndRenderPass.Execute");
        var cmd = CommandBufferPool.Get("AtmosphereEnd");

        var renderer = renderingData.cameraData.renderer;
        var target = renderer.cameraColorTargetHandle;

        data.ExecuteBlitBackToColor(cmd, target);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}