using MelonLoader;
using OuterWildsRumble.Atmosphere.Hope;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class AtmospherePassManager
{
    static AtmosphereBlitData sharedData;

    static BlitStartRenderPass startPass;
    static AtmosphereRenderPass pass;
    static DepthStackRenderPass depthStackPass;
    static BlitEndRenderPass endPass;
    
    static TestRenderPass testPass;

    
    public static readonly List<AtmosphereEffect> ActiveEffects = new();

    public static void Init(Shader shader)
    {
        MelonLogger.Msg("Setting up shader");

        MelonLogger.Msg("Setting up AtmosphereBlitData");
        sharedData = new AtmosphereBlitData();

        MelonLogger.Msg("Setting up BlitStartRenderPass");
        startPass = new BlitStartRenderPass();
        MelonLogger.Msg("Setting up AtmosphereRenderPass");
        pass = new AtmosphereRenderPass(shader);
        
        testPass = new TestRenderPass();

        if (OuterWildsRumble.Main.copyDepthMaterial == null) 
        {
           MelonLogger.Error("CopyDepth material could not be found! Make sure Hidden/CopyDepth shader is located somewhere in your project and included in 'Always Included Shaders'");
           return;
        }
        
        depthStackPass = new DepthStackRenderPass(OuterWildsRumble.Main.copyDepthMaterial);
        MelonLogger.Msg("Setting up BlitEndRenderPass");
        endPass = new BlitEndRenderPass();

        startPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox; // AfterRenderingSkybox
        pass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox; // AfterRenderingSkybox
        endPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox; // AfterRenderingSkybox
        depthStackPass.renderPassEvent = RenderPassEvent.AfterRendering; // AfterRendering
        
        
        testPass.renderPassEvent = RenderPassEvent.AfterRendering; // AfterRendering
        
        RenderPipelineManager.beginCameraRendering += new Action<ScriptableRenderContext, Camera>(OnBeginCameraRendering);
    }

    public static void Enqueue(ScriptableRenderer renderer)
    {
        if (renderer == null) return;

        // 1. Safety Check
        if (sharedData == null || startPass == null || pass == null || endPass == null || depthStackPass == null)
        {
            MelonLogger.Warning("Enqueue skipped: AtmospherePassManager not initialized");
            return;
        }

        try
        {
            sharedData.Reset();
            startPass.Setup(sharedData);
            pass.Setup(sharedData);
            endPass.Setup(sharedData);
            
            renderer.EnqueuePass(startPass);
            renderer.EnqueuePass(pass);
            renderer.EnqueuePass(depthStackPass);
            renderer.EnqueuePass(endPass);
            renderer.EnqueuePass(testPass);
            
            // Debug
            // foreach (var p in renderer.activeRenderPassQueue)
            // {
            //     if (p.renderPassEvent == startPass.renderPassEvent)
            //         MelonLogger.Msg($"[Active] Pass: {p.GetType().Name} @ {p.renderPassEvent}");
            // }
            
            
            MelonLogger.Warning($"Enqueue executed successfully current queue count: {renderer.activeRenderPassQueue.Count}");
        }
        catch (System.Exception e)
        {
            MelonLogger.Warning($"Enqueue failed safely: {e.Message}");
        }
    }

    static void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
    {
        // 1. Filter for valid cameras only
        if (cam == null || cam.cameraType != CameraType.Game) return;
    
        // Optional: Filter specifically for the Main Camera if needed
        // if (cam.name != "Main Camera" && cam.name != "PlayerCamera") return;

        var cameraData = cam.GetUniversalAdditionalCameraData();
        if (cameraData == null) return;

        var renderer = cameraData.scriptableRenderer;
        if (renderer != null)
        {
            Enqueue(renderer);
        }
    }
    
    
    static Shader AddAlwaysIncludedShader(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) 
        {
            return null;
        }
     
#if UNITY_EDITOR
        var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        var serializedObject = new SerializedObject(graphicsSettingsObj);
        var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");
        bool hasShader = false;

        for (int i = 0; i < arrayProp.arraySize; ++i)
        {
            var arrayElem = arrayProp.GetArrayElementAtIndex(i);
            if (shader == arrayElem.objectReferenceValue)
            {
                hasShader = true;
                break;
            }
        }
     
        if (!hasShader)
        {
            int arrayIndex = arrayProp.arraySize;
            arrayProp.InsertArrayElementAtIndex(arrayIndex);
            var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
            arrayElem.objectReferenceValue = shader;
     
            serializedObject.ApplyModifiedProperties();
     
            AssetDatabase.SaveAssets();
        }
#endif

        return shader;
    }
    
}