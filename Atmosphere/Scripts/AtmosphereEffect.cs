using UnityEngine;
using UnityEngine.Rendering;
using MelonLoader;
using OuterWildsRumble; // Added for debug logging

public class AtmosphereEffect : MonoBehaviour
{
	public AtmosphereProfile profile;

	public Transform sun;
	public bool directional = true;

	public float planetRadius = 1000.0f;
	public float cutoffDepth = 50.0f;
	public float atmosphereScale = 0.25f;

	public float AtmosphereSize => (1 + atmosphereScale) * planetRadius;

	public Material material;
	private ComputeShader computeInstance;
	private RenderTexture opticalDepthTexture;

	// Values to check if optical depth texture is up to date
	private int _width, _points;
	private float _size, _scale, _rayFalloff, _mieFalloff, _hAbsorbtion;

	private void OnEnable() 
	{
		MelonLogger.Msg($"[AtmosphereEffect] OnEnable on {gameObject.name}");
		AtmosphereRenderPassTest.RegisterEffect(this);
		MelonLogger.Msg($"[AtmosphereEffect] Registered with render pass for {gameObject.name}");

		// Try to create the material immediately if the shader is already loaded
		try
		{
			GetMaterial(OuterWildsRumble.Main.atmosphereShader);
			if (material != null)
				MelonLogger.Msg($"[AtmosphereEffect] Material created on OnEnable for {gameObject.name} with shader: {material.shader?.name}");
		}
		catch (System.Exception e)
		{
			MelonLogger.Msg($"[AtmosphereEffect] Exception while creating material in OnEnable for {gameObject.name}: {e.Message}");
		}
	}

	private int ticksBeforeCheck = 240;
	private int ticksCounted = 240;

	private void LateUpdate() 
	{
		// If we haven't got a material yet attempt to create one (this will use Main.atmosphereShader if available, else the Shader.Find fallbacks inside GetMaterial)
		if (material == null)
		{
			try
			{
				GetMaterial(OuterWildsRumble.Main.atmosphereShader);
			}
			catch (System.Exception e)
			{
				MelonLogger.Msg($"[AtmosphereEffect] Exception while creating material for {gameObject.name}: {e.Message}");
			}
		}

		// Log component state every few seconds (optional, to avoid spam)
		if ((material == null || sun == null || profile == null))
		{
			ticksCounted++;
			if (ticksCounted >= ticksBeforeCheck)
			{
				if (material == null) MelonLogger.Error($"[AtmosphereEffect] material is null on {gameObject.name}");
				if (sun == null) MelonLogger.Error($"[AtmosphereEffect] sun is null on {gameObject.name}");
				if (profile == null) MelonLogger.Error($"[AtmosphereEffect] profile is null on {gameObject.name}");
				ticksCounted = 0;
			}
			return;
		}

		//MelonLogger.Msg($"[AtmosphereEffect] LateUpdate running for {gameObject.name}");

		profile.SetProperties(material);
		ValidateOpticalDepth();

		// Check if texture is set
		if (opticalDepthTexture == null)
			MelonLogger.Error($"[AtmosphereEffect] opticalDepthTexture is null after ValidateOpticalDepth on {gameObject.name}");
		else
			material.SetTexture("_BakedOpticalDepth", opticalDepthTexture);

		material.SetVector("_PlanetCenter", transform.position);

		if (directional)
		{	
			material.SetVector("_SunParams", -sun.forward);
			material.EnableKeyword("DIRECTIONAL_SUN");
			//MelonLogger.Msg($"[AtmosphereEffect] Directional sun set, forward = {sun.forward}");
		} 
		else
		{
			material.SetVector("_SunParams", sun.position);
			material.DisableKeyword("DIRECTIONAL_SUN");
			//MelonLogger.Msg($"[AtmosphereEffect] Positional sun set, position = {sun.position}");
		}

		material.SetFloat("_AtmosphereRadius", AtmosphereSize);
		material.SetFloat("_PlanetRadius", planetRadius);
		material.SetFloat("_CutoffRadius", planetRadius - cutoffDepth);

		//MelonLogger.Msg($"[AtmosphereEffect] Material properties updated for {gameObject.name}");
	}

	private void OnDisable() 
	{
		MelonLogger.Msg($"[AtmosphereEffect] OnDisable on {gameObject.name}");
		AtmosphereRenderPassTest.RemoveEffect(this);

		if (computeInstance != null) 
		{
			DestroyImmediate(computeInstance);
			MelonLogger.Msg($"[AtmosphereEffect] computeInstance destroyed");
		}

		if (opticalDepthTexture != null)
		{
			opticalDepthTexture.Release();
			DestroyImmediate(opticalDepthTexture);
			MelonLogger.Msg($"[AtmosphereEffect] opticalDepthTexture released and destroyed");
		}
	}

	private void ValidateOpticalDepth()
	{
		if (profile == null)
		{
			MelonLogger.Error($"[AtmosphereEffect] profile is null in ValidateOpticalDepth on {gameObject.name}");
			return;
		}

		bool upToDate = profile.IsUpToDate(ref _width, ref _points, ref _rayFalloff, ref _mieFalloff, ref _hAbsorbtion);
		bool sizeChange = _size != planetRadius || _scale != atmosphereScale;
		bool textureExists = opticalDepthTexture != null && opticalDepthTexture.IsCreated();

		MelonLogger.Msg($"[AtmosphereEffect] ValidateOpticalDepth: upToDate={upToDate}, sizeChange={sizeChange}, textureExists={textureExists}");

		if (!upToDate || sizeChange || !textureExists) 
		{
			MelonLogger.Msg($"[AtmosphereEffect] Re-baking optical depth texture for {gameObject.name}");

			// Ensure we have a compute shader to instantiate
			if (profile.OpticalDepthCompute == null)
			{
				MelonLogger.Error($"[AtmosphereEffect] Cannot bake optical depth: profile.OpticalDepthCompute is null for {gameObject.name}. Skipping bake.");
				// Prevent constant re-tries by updating cached size/scale so sizeChange becomes false
				_size = planetRadius;
				_scale = atmosphereScale;
				return;
			}

			if (computeInstance == null) 
			{
				computeInstance = Instantiate(profile.OpticalDepthCompute);
				MelonLogger.Msg($"[AtmosphereEffect] Created new computeInstance from profile.OpticalDepthCompute");
			}

			MelonLogger.Msg($"[AtmosphereEffect] Calling profile.BakeOpticalDepth with planetRadius={planetRadius}, AtmosphereSize={AtmosphereSize}");
			profile.BakeOpticalDepth(ref opticalDepthTexture, computeInstance, planetRadius, AtmosphereSize);

			if (opticalDepthTexture != null && opticalDepthTexture.IsCreated())
			{
				MelonLogger.Msg($"[AtmosphereEffect] Optical depth texture baked successfully: {opticalDepthTexture.width}x{opticalDepthTexture.height}, format={opticalDepthTexture.format}");
			}
			else
			{
				MelonLogger.Error($"[AtmosphereEffect] Failed to create optical depth texture!");
			}

			_size = planetRadius;
			_scale = atmosphereScale;
		}
	}

	internal Material GetMaterial(Shader atmosphereShader) 
	{
		if (material == null)
		{
			// If the passed shader is null, try to find one by name as a fallback
			if (atmosphereShader == null)
			{
				MelonLogger.Msg($"[AtmosphereEffect] No shader passed to GetMaterial for {gameObject.name}. Attempting Shader.Find fallbacks.");
				// First try a commonly used internal name
				atmosphereShader = Shader.Find("AtmosphereNew");
				if (atmosphereShader != null)
				{
					MelonLogger.Msg($"[AtmosphereEffect] Found shader via Shader.Find: {atmosphereShader.name}");
				}
				else
				{
					// Next, fall back to a safe URP shader so we at least get a visible material
					atmosphereShader = Shader.Find("Universal Render Pipeline/Lit");
					if (atmosphereShader != null)
						MelonLogger.Msg($"[AtmosphereEffect] Falling back to URP shader: {atmosphereShader.name}");
					else
						MelonLogger.Error($"[AtmosphereEffect] No suitable shader found via Shader.Find for {gameObject.name}");
				}
			}

			if (atmosphereShader == null)
			{
				MelonLogger.Error($"[AtmosphereEffect] atmosphereShader is null when creating material for {gameObject.name}");
			}
			else
			{
				MelonLogger.Msg($"[AtmosphereEffect] Creating new material with shader: {atmosphereShader.name}");
			}
			material = new Material(atmosphereShader);
		}
		return material;
	}

	public bool IsVisible(Plane[] cameraPlanes) 
	{
		if (profile == null || sun == null) 
		{
			MelonLogger.Warning($"[AtmosphereEffect] IsVisible check failed because profile or sun is null on {gameObject.name}");
			return false;
		}

		Vector3 pos = transform.position;
		float radius = AtmosphereSize;

		for (int i = 0; i < cameraPlanes.Length - 1; i++) 
		{
			float distance = cameraPlanes[i].GetDistanceToPoint(pos);
			if (distance < 0 && Mathf.Abs(distance) > radius) 
			{
				MelonLogger.Msg($"[AtmosphereEffect] {gameObject.name} culled by plane {i} at distance {distance}");
				return false;
			}
		}
		MelonLogger.Msg($"[AtmosphereEffect] {gameObject.name} is visible to camera");
		return true;
	}

	public float DistToAtmosphere(Vector3 pos) 
	{
		return (pos - transform.position).magnitude - AtmosphereSize;
	}

	private void OnDrawGizmosSelected() 
	{
		if (sun != null) 
		{
			Gizmos.color = Color.green;
			Vector3 sunDir = directional ? -sun.forward : (sun.position - transform.position).normalized;
			Gizmos.DrawRay(transform.position, sunDir * planetRadius);
		}
	}
}