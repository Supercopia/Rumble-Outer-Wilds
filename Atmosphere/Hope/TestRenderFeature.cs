using Il2CppInterop.Runtime.Injection;
using UnityEngine.Rendering.Universal;

namespace OuterWildsRumble.Atmosphere.Hope;

public class TestRenderFeature : ScriptableRendererFeature
{
    public TestRenderFeature(IntPtr ptr) : base(ptr) { }
    
    // Used by managed code when creating new instances of this class
    public TestRenderFeature() : base(ClassInjector.DerivedConstructorPointer<TestRenderFeature>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }
}