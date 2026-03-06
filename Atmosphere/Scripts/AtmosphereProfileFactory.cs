namespace OuterWildsRumble.Atmosphere.Scripts;

using UnityEngine;

public static class AtmosphereProfileFactory
{
    public static AtmosphereProfile CreateDefaultAtmosphereProfile(ComputeShader opticalDepthCompute)
    {
        if (opticalDepthCompute == null)
        {
            throw new System.ArgumentNullException(nameof(opticalDepthCompute), "ComputeShader cannot be null.");
        }

        // Create a new ScriptableObject instance
        AtmosphereProfile profile = ScriptableObject.CreateInstance<AtmosphereProfile>();

        // Assign default values
        profile.LUTSize = AtmosphereProfile.TextureSizes._256;
        profile.opticalDepthPoints = 15;
        profile.inScatteringPoints = 25;
        profile.sunIntensity = 20f;

        profile.rayleighScatter = new AtmosphereProfile.ScatterWavelengths
        {
            red = 0.556f,
            green = 0.7f,
            blue = 0.84f,
            power = 2f
        };
        profile.rayleighDensityFalloff = 15f;

        profile.mieScatter = new AtmosphereProfile.ScatterWavelengths
        {
            red = 1.0f,
            green = 0.95f,
            blue = 0.8f,
            power = 0.1f
        };
        profile.mieDensityFalloff = 15f;
        profile.mieG = 0.97f;

        profile.heightAbsorbtion = 0f;
        profile.absorbtionColor = Color.black;
        profile.ambientColor = Color.black;

        // Assign the required ComputeShader
        profile.OpticalDepthCompute = opticalDepthCompute;

        return profile;
    }
}