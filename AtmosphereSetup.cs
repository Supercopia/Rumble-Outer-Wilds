using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OuterWildsRumble;

public static class AtmosphereSetup
{
    private static bool _added;

    public static void AddAtmosphereFeatureAtRuntime()
    {
        if (_added) return; // Prevent duplicates
        MelonLogger.Msg($"Starting runtime atmosphere injection");
    
        var pipeline = UniversalRenderPipeline.asset;
    
        if (pipeline == null)
        {
            MelonLogger.Error("URP not active. Cannot inject feature.");
            return;
        }
    
        ScriptableRenderer renderer = pipeline.scriptableRenderer;
    
        if (renderer == null)
        {
            MelonLogger.Error("No ScriptableRenderer found.");
            return;
        }
        
        
        var featureList = renderer.rendererFeatures;
    
        if (featureList == null)
        {
            MelonLogger.Error("Could not access rendererFeatures.");
            return;
        }
         
        
        foreach (var feature in featureList)
        {
            if (feature is AtmosphereRendererFeatureTest)
            {
                MelonLogger.Msg("Atmosphere feature already exists.");
                _added = true;
                return;
            }
        }
        
        if (Main.atmosphereShader == null)
        {
            MelonLogger.Error("Atmosphere shader is NULL. Cannot inject feature.");
            return;
        }
        
        //var newFeature = new AtmosphereRendererFeatureTest();
        
        var newFeature = ScriptableObject.CreateInstance<AtmosphereRendererFeatureTest>();
        
        if (newFeature == null)
        {
            MelonLogger.Error("newFeature is NULL.");
            return;
        }
        
        newFeature.name = "AtmosphereRendererFeatureTest";
        newFeature.SetAtmosphereShader(Main.atmosphereShader);
        newFeature.Create();
    
        // inject
        featureList.Add(newFeature);
        newFeature.SetActive(true);
        
        MelonLogger.Msg($"[AtmosphereSetup] Feature added. Total features in list: {featureList.Count}");
        MelonLogger.Msg($"[AtmosphereSetup] Feature active? {newFeature.isActive}");
    
        MelonLogger.Msg($"[AtmosphereSetup] Feature in updated list? {featureList.Contains(newFeature)}");
    
        _added = true;
        
        MethodInfo OnValidate = pipeline.scriptableRenderer.GetType().GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
        OnValidate.Invoke(pipeline.scriptableRenderer, new object[] { });
        
        MelonLogger.Msg($"Finished injection");
    }
    
    public static void InjectAtmosphereFeatureAtRuntime(Shader atmosphereShader)
    {
        if (_added)
        {
            MelonLogger.Msg("Atmosphere feature already injected.");
            return;
        }

        // Get current URP pipeline asset
        var pipeline = UniversalRenderPipeline.asset;
        if (pipeline == null)
        {
            MelonLogger.Error("URP pipeline not active. Cannot inject feature.");
            return;
        }

        ScriptableRenderer renderer = pipeline.scriptableRenderer;
        if (renderer == null)
        {
            MelonLogger.Error("No ScriptableRenderer found.");
            return;
        }

        var featureList = renderer.rendererFeatures;
        if (featureList == null)
        {
            MelonLogger.Error("Could not access rendererFeatures.");
            return;
        }

        if (atmosphereShader == null)
        {
            MelonLogger.Error("Atmosphere shader is NULL. Cannot inject feature.");
            return;
        }

        var pipelineAsset = UniversalRenderPipeline.asset;
        if (pipelineAsset == null)
        {
            MelonLogger.Error("URP asset is null!");
            return;
        }

        var rendererData = pipelineAsset.m_RendererData;
        if (rendererData == null)
        {
            MelonLogger.Error("RendererData is null!");
            return;
        }

        var features = rendererData.rendererFeatures;
        if (features == null || features._items == null || features._items.Count == 0)
        {
            MelonLogger.Error("RendererFeatures or _items is null/empty!");
            return;
        }

        ScriptableRendererFeature baseFeature = features._items.First(); //TODO was null

        if (baseFeature == null)
        {
            MelonLogger.Msg("Base feature is NULL.");
            MelonLogger.Msg("List size: " + features.Count);
            return;
        }

        //baseFeature.name = "AtmosphereFeature_Clone";

        var blitData = new AtmosphereRendererFeatureTest.BlitData();

        var startPass = new AtmosphereRendererFeatureTest.BlitStartRenderPass();
        startPass.Setup(blitData);
        startPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        var atmospherePass = new AtmosphereRendererFeatureTest.AtmosphereRenderPassTest(atmosphereShader);
        atmospherePass.Setup(blitData);
        atmospherePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        var endPass = new AtmosphereRendererFeatureTest.BlitEndRenderPass();
        endPass.Setup(blitData);
        endPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

        renderer.EnqueuePass(startPass);
        renderer.EnqueuePass(atmospherePass);
        renderer.EnqueuePass(endPass);
        
        featureList.Add(baseFeature);
        
        var onValidate = renderer.GetType().GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
        if (onValidate == null)
        {
            MelonLogger.Error("Could not find OnValidate method on ScriptableRenderer.");
            return;
        }
        onValidate?.Invoke(renderer, null);

        _added = true;
        MelonLogger.Msg("Atmosphere feature injected successfully at runtime.");
    }

    public static void Test()
    {
        // Check render pipeline
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null)
        {
            MelonLogger.Error("Current Render Pipeline is null or not UniversalRenderPipelineAsset.");
            return;
        }

        // Get renderer
        var renderer = pipeline.GetRenderer(0);
        if (renderer == null)
        {
            MelonLogger.Error("Renderer at index 0 is null.");
            return;
        }

        // Get property info
        var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures");
        if (property == null)
        {
            MelonLogger.Error("Could not find property 'rendererFeatures' on ScriptableRenderer.");
            return;
        }

        // Get feature list
        var features = property.GetValue(renderer) as List<ScriptableRendererFeature>;
        if (features == null)
        {
            MelonLogger.Error("rendererFeatures list is null or failed to cast.");
            return;
        }

        MelonLogger.Msg($"Renderer features list found. Current count: {features.Count}");

        // Create new feature
        var newFeature = new AtmosphereRendererFeatureTest();
        if (newFeature == null)
        {
            MelonLogger.Error("Failed to create AtmosphereRendererFeatureTest.");
            return;
        }

        // Add feature
        features.Add(newFeature);

        // Validate index
        int index = features.Count - 1;
        if (index < 0 || index >= features.Count)
        {
            MelonLogger.Error("Feature index out of bounds after adding.");
            return;
        }

        var addedFeature = features[index];
        if (addedFeature == null)
        {
            MelonLogger.Error("Added feature is null.");
            return;
        }

        // Activate + create
        addedFeature.SetActive(true);
        addedFeature.Create();

        MelonLogger.Msg("Added render feature. Total features in list: " + features.Count);
    }

    public static void EnablePostProcessingOnAllCameras()
    {
        Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsSortMode.None);
    
        foreach (var cam in cameras)
        {
            var additionalData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (additionalData != null)
                additionalData.renderPostProcessing = true;
        }
    }
    
    private static void UsedOnlyForAOT() { //dummy method to ensure the feature class is included in AOT builds
        ScriptableObject.CreateInstance<AtmosphereRendererFeatureTest>();
    }
}