using MelonLoader;
using OuterWildsRumble.Components.SupernovaUtils;
using UnityEngine;
using System.Collections;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SupernovaSun : MonoBehaviour
{
    public SupernovaSun(IntPtr ptr) : base(ptr) {}// TODO collapse into itself and big kaboom :3

    public int secondsToFullRed = 40;
    public float blueTransitionTime = 2f;
    public Vector3 maxExtraScale = new Vector3(0.3f, 0.3f, 0.3f);

    public Light sunLight;
    public Color sunlightOriginal;
    public Color sunlightRed = new Color(0.8f, 0.116f, 0);
    public Color sunlightBlue = Color.cyan;

    private Renderer sunRenderer;
    private Material sunMaterial;
    private Renderer haloRenderer;
    private Material haloMaterial;

    private float timer;
    private float blueTimer;
    private bool blueTriggered;
    private bool settingsInitialized;

    private SunShaderUtils.SunCoreSettings startCore;
    private SunShaderUtils.SunCoreSettings redCore;
    private SunShaderUtils.SunCoreSettings superCore;

    private SunShaderUtils.SunHaloSettings startHalo;
    private SunShaderUtils.SunHaloSettings redHalo;
    private SunShaderUtils.SunHaloSettings superHalo;

    private Vector3 initialScale;

    void Start()
    {
        sunRenderer = GetComponent<Renderer>();
        if (sunRenderer == null) return;
        sunMaterial = sunRenderer.material;

        if (transform.childCount > 0)
        {
            haloRenderer = transform.GetChild(0).GetComponent<Renderer>();
            haloMaterial = haloRenderer != null ? haloRenderer.material : null;
        }

        initialScale = transform.localScale;

        if (sunLight != null)
            sunlightOriginal = sunLight.color;

        MelonCoroutines.Start(InitializeAfterFrame());
    }

    private IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        if (sunMaterial != null)
        {
            startCore = SunShaderUtils.ReadCore(sunMaterial);

            redCore = startCore;
            redCore.Color1 = new Color(0.7f, 0.133f, 0);
            redCore.Color2 = new Color(0.8f, 0.116f, 0);
            redCore.SunBright = 1.8f;

            superCore = startCore;
            superCore.SunBright *= 2f;
            superCore.Color2 = Color.cyan;
            superCore.Color1 = Color.darkCyan;
        }

        if (haloMaterial != null)
        {
            startHalo = SunShaderUtils.ReadHalo(haloMaterial);

            redHalo = startHalo;
            redHalo.HaloRing1Color = new Color(1f, 0, 0);
            redHalo.HaloRing2Color = new Color(0.749f, 0.012f, 0);

            superHalo = startHalo;
            superHalo.HaloRing2Color = Color.cyan;
        }

        settingsInitialized = true;
    }

    public void TriggerSupernova()
    {
        if (!blueTriggered)
        {
            blueTriggered = true;
            blueTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (!settingsInitialized) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / secondsToFullRed);
        float redT = Mathf.SmoothStep(0f, 1f, t * 1.2f);

        float superT = 0f;
        if (blueTriggered)
        {
            blueTimer += Time.deltaTime;
            superT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blueTimer / blueTransitionTime));
        }

        transform.localScale = Vector3.Lerp(initialScale, initialScale + maxExtraScale, redT);

        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(startCore, redCore, superCore, redT, superT));
        SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(startHalo, redHalo, superHalo, redT, superT));
        
        sunLight.color = Color.Lerp(
            Color.Lerp(sunlightOriginal, sunlightRed, redT),
            sunlightBlue,
            superT
        );
    }

    private SunShaderUtils.SunCoreSettings LerpCore(
        SunShaderUtils.SunCoreSettings start,
        SunShaderUtils.SunCoreSettings red,
        SunShaderUtils.SunCoreSettings super,
        float redT, float superT)
    {
        return new SunShaderUtils.SunCoreSettings
        {
            SunBright = Mathf.Lerp(start.SunBright, super.SunBright, superT),
            SunSpeed = start.SunSpeed,
            Color1 = Color.Lerp(Color.Lerp(start.Color1, red.Color1, redT), super.Color1, superT),
            Color2 = Color.Lerp(Color.Lerp(start.Color2, red.Color2, redT), super.Color2, superT),
            Color3 = start.Color3,
            Color4 = start.Color4,
        };
    }

    private SunShaderUtils.SunHaloSettings LerpHalo(
        SunShaderUtils.SunHaloSettings start,
        SunShaderUtils.SunHaloSettings red,
        SunShaderUtils.SunHaloSettings super,
        float redT, float superT)
    {
        return new SunShaderUtils.SunHaloSettings
        {
            HaloRing1 = start.HaloRing1,
            HaloRing1Color = Color.Lerp(Color.Lerp(start.HaloRing1Color, red.HaloRing1Color, redT), super.HaloRing1Color, superT),
            HaloRing1Size = start.HaloRing1Size,
            HaloRing1Intensity = start.HaloRing1Intensity,
            HaloRing1Strength = start.HaloRing1Strength,

            HaloRing2 = start.HaloRing2,
            HaloRing2Str = start.HaloRing2Str,
            HaloRing2Thickness = start.HaloRing2Thickness,
            HaloRing2Color = Color.Lerp(Color.Lerp(start.HaloRing2Color, red.HaloRing2Color, redT), super.HaloRing2Color, superT),
            HaloRing2Size = start.HaloRing2Size,
            HaloRing2Intensity = start.HaloRing2Intensity,
            HaloRing2Width = start.HaloRing2Width,
        };
    }
}