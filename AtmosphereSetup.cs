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
        
        

        // Get private field m_RendererFeatures
        var featureList = renderer.m_RendererFeatures;

        if (featureList == null)
        {
            MelonLogger.Error("Could not access m_RendererFeatures.");
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

        // Create feature
        // Instead of using the generic method, use the Type overload:
        
        var newFeature = (AtmosphereRendererFeatureTest)ScriptableObject.CreateInstance(
            Il2CppType.Of<AtmosphereRendererFeatureTest>()
        );
        // Set up the feature (name, shader, etc.), then call Create() and add to the renderer.
        newFeature.name = "AtmosphereRendererFeatureTest";
        newFeature.SetAtmosphereShader(Main.atmosphereShader);
        newFeature.Create();

        // Inject
        featureList.Add(newFeature);
        newFeature.SetActive(true);
        
        MelonLogger.Msg($"[AtmosphereSetup] Feature added. Total features in list: {featureList.Count}");
        MelonLogger.Msg($"[AtmosphereSetup] Feature active? {newFeature.isActive}");

        var updatedList = featureList;
        MelonLogger.Msg($"[AtmosphereSetup] Feature in updated list? {updatedList.Contains(newFeature)}");

        _added = true;
        
        MelonLogger.Msg($"Finished injection");
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