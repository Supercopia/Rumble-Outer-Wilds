using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OuterWildsRumble.Atmosphere.Hope;

public class AtmosphereRenderPass : ScriptableRenderPass
{
    AtmosphereBlitData data;
    Shader shader;
    
    public AtmosphereRenderPass(Shader atmosphereShader)
    {
        shader = atmosphereShader;
    }
    
    public AtmosphereRenderPass(IntPtr ptr) : base(ptr) { }

    public AtmosphereRenderPass() 
        : base(ClassInjector.DerivedConstructorPointer<AtmosphereRenderPass>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
    
    [HideFromIl2Cpp]
    public void Setup(AtmosphereBlitData blitData)
    {
        data = blitData;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        MelonLogger.Msg("Running AtmosphereRenderPass.Execute");
        Camera camera = renderingData.cameraData.camera;

        if (camera == null)
            return;

        var cmd = CommandBufferPool.Get("Atmosphere Pass");
        
        foreach (var effect in AtmospherePassManager.ActiveEffects)
        {
            if (effect == null)
                continue;

            // Cull effects outside camera view
            if (!effect.IsVisible(camera))
                continue;

            var mat = effect.GetMaterial(shader);
            if (mat == null)
                continue;

            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, mat);
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}