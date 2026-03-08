using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OuterWildsRumble.Atmosphere.Hope;

public class TestRenderPass : ScriptableRenderPass
{
    public TestRenderPass(IntPtr ptr) : base(ptr) { }
    
    public TestRenderPass() 
        : base(ClassInjector.DerivedConstructorPointer<TestRenderPass>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {        
        MelonLogger.Msg("Executing TestRenderPass");
    }
    
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        MelonLogger.Msg("TestRenderPass.OnCameraSetup");
    }
}