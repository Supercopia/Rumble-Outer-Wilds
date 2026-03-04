using UnityEngine;



public class AtmosphereProfile : ScriptableObject
{
	public enum TextureSizes 
	{
		_32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512
	}

	
	[Serializable]
	public struct ScatterWavelengths
	{
		public float red;
		public float green;
		public float blue;

		public float power; // Minimum value is -1 because negative values around that range can give a nice black-light effect. Any more than that and it'll flashbang you.

		// Scattering wavelengths are inversely proportional to channel^4.
		public readonly Vector3 Wavelengths => new Vector3(
			Mathf.Pow(red, 4), 
			Mathf.Pow(green, 4), 
			Mathf.Pow(blue, 4)
		) * power;
	}


	public TextureSizes LUTSize = TextureSizes._256;
	private ComputeShader opticalDepthCompute; 
	public int opticalDepthPoints = 15;

	public ComputeShader OpticalDepthCompute
	{
		get { return opticalDepthCompute; }
		set { opticalDepthCompute = value; }
	}


	public int inScatteringPoints = 25;
	public float sunIntensity = 20;


	public ScatterWavelengths rayleighScatter = new ScatterWavelengths { red = 0.556f, green = 0.7f, blue = 0.84f, power = 2f };
	public float rayleighDensityFalloff = 15;


	public ScatterWavelengths mieScatter = new ScatterWavelengths { red = 1.0f, green = 0.95f, blue = 0.8f, power = 0.1f };
	public float mieDensityFalloff = 15;
	public float mieG = 0.97f;


	public float heightAbsorbtion = 0;
	public Color absorbtionColor = Color.black;


	public Color ambientColor = Color.black;



	internal void SetProperties(Material material) 
	{
		material.SetInteger("_NumInScatteringPoints", inScatteringPoints);
		material.SetInteger("_NumOpticalDepthPoints", opticalDepthPoints);

		material.SetVector("_RayleighScattering", rayleighScatter.Wavelengths);

		material.SetVector("_MieScattering", mieScatter.Wavelengths);
		material.SetVector("_AbsorbtionBeta", absorbtionColor);
		material.SetVector("_AmbientBeta", ambientColor);

		material.SetFloat("_MieG", mieG);
		
		material.SetFloat("_RayleighFalloff", rayleighDensityFalloff);
		material.SetFloat("_MieFalloff", mieDensityFalloff);
		material.SetFloat("_HeightAbsorbtion", heightAbsorbtion);

		material.SetFloat("_Intensity", sunIntensity);
	} 


	private void SetComputeProperties(ComputeShader shader, float planetRadius, float atmosphereRadius)
	{
		shader.SetInt("_TextureSize", (int)LUTSize);
		shader.SetInt("_NumOutScatteringSteps", opticalDepthPoints);

		shader.SetFloat("_PlanetRadius", planetRadius); 
		shader.SetFloat("_AtmosphereRadius", atmosphereRadius);

		shader.SetFloat("_RayleighFalloff", rayleighDensityFalloff);
		shader.SetFloat("_MieFalloff", mieDensityFalloff);
		shader.SetFloat("_HeightAbsorbtion", heightAbsorbtion);
	}


	internal bool IsUpToDate(ref int LUTSize, ref int opticalPoints, ref float rayleighFalloff, ref float mieFalloff, ref float absorbtion) 
	{
		bool upToDate = (int)this.LUTSize == LUTSize && 
			opticalDepthPoints == opticalPoints && 
			rayleighDensityFalloff == rayleighFalloff && 
			mieDensityFalloff == mieFalloff && 
			heightAbsorbtion == absorbtion;

		LUTSize = (int)this.LUTSize;
		opticalPoints = opticalDepthPoints;
		rayleighFalloff = rayleighDensityFalloff;
		mieFalloff = mieDensityFalloff;
		absorbtion = heightAbsorbtion;

		return upToDate;
	}


	internal void BakeOpticalDepth(ref RenderTexture opticalDepthTexture, ComputeShader shader, float planetRadius, float atmosphereRadius) 
	{ 
		if (shader == null) 
		{
			throw new Exception("Compute Shader not provided");
		}

		if (opticalDepthTexture == null || !opticalDepthTexture.IsCreated()) 
		{
			CreateRenderTexture(ref opticalDepthTexture, (int)LUTSize, (int)LUTSize, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);	

			shader.SetTexture(0, "_Result", opticalDepthTexture);

			SetComputeProperties(shader, planetRadius, atmosphereRadius);

			shader.GetKernelThreadGroupSizes(0, out uint x, out uint y, out _);

			int numGroupsX = Mathf.CeilToInt((int)LUTSize / (float)x);
			int numGroupsY = Mathf.CeilToInt((int)LUTSize / (float)y);
			shader.Dispatch(0, numGroupsX, numGroupsY, 1);
		}
	}


	private static void CreateRenderTexture(ref RenderTexture texture, int width, int height, FilterMode filterMode = FilterMode.Bilinear, RenderTextureFormat format = RenderTextureFormat.ARGB32) 
	{
		if (texture == null || !texture.IsCreated() || texture.width != width || texture.height != height || texture.format != format) 
		{
			if (texture != null)
			{
				texture.Release();
			}

            texture = new RenderTexture(width, height, 0)
            {
                format = format,
                enableRandomWrite = true,
                autoGenerateMips = false
            };

            texture.Create();
		}
		
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = filterMode;
	}
}
