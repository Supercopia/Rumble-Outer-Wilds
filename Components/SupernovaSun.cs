using System.Collections;
using System.Collections.Generic;
using System.IO;
using AudioSchtuff;
using MelonLoader;
using OuterWildsRumble.Components.SupernovaUtils;
using RumbleModdingAPI.RMAPI;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SupernovaSun : MonoBehaviour
{
    private const string endTimesSoundName = "OW_EndTimes.wav";
    private const string supernovaCollapseSoundName = "Sun_supernova_collapse.wav";
    private const string supernovaExplosionSoundName = "Sun_supernova_explosion.wav";
    private const string supernovaWallSoundName = "Sun_supernova_wall.wav";
    
    private const float wallBaseVolume = 0.1f;
    private float wallRampRange = 0f;
    private AudioManager.ClipData wallClip;

    private float extraDistance = 25f;          // how far beyond the surface the wall sound reaches max volume
    private float originalLightIntensity;

    // ---------- Player & expansion control ----------
    public Transform playerTransform;           // set externally via SetPlayerTransform()
    private bool hasReachedPlayer = false;
    private bool isFadingOut = false;
    public float expansionSpeedWorldUnitsPerSec = 24f;   // world units per second during wall phase

    // ---------- Required targets ----------
    // The sun must engulf ALL of these (in addition to the player) before stopping.
    // Populated in Start() from Main.solarSystem. Add or remove entries there as needed.
    private struct RequiredTarget
    {
        public Transform transform;
        public float radius;    
        public bool engulfed;
    }
    private RequiredTarget requiredTarget;

    // ---------- Timing & scale settings ----------
    public int secondsToFullRed = 60 * 1;
    public float waitAfterRed = 60 + 32f;
    public float collapseDuration = 9.5f;
    public float explosionDuration = 3.7f;
    public float wallDuration = 32f;            // kept for compatibility (no longer used)
    public Vector3 collapseScale = new(0.08f, 0.08f, 0.08f);
    public Vector3 explosionTargetScale = new(2.5f, 2.5f, 2.5f);
    // How much bigger than the original the sun should be at peak red — set per-axis.
    public Vector3 redGrowthScale = new(0.05f, 0.05f, 0.05f);
    //public Vector3 explosionMaxScale = new(15f, 15f, 15f);

    // ---------- Light & colour references ----------
    public Light sunLight;
    public Color sunlightOriginal;
    public Color sunlightRed = new(0.73f, 0.116f, 0);
    public Color sunlightWhite = Color.white;
    public Color sunlightBlue = Color.cyan;

    // ---------- Internal state ----------
    private enum Phase { Red, RedFullWait, Collapse, Explosion, Wall, Done }
    private Phase currentPhase = Phase.Red;
    private float phaseTimer = 0f;

    private Renderer sunRenderer;
    private Material sunMaterial;
    private Renderer haloRenderer;
    private Material haloMaterial;

    private SunShaderUtils.SunCoreSettings startCore, redCore, whiteCore, superCore;
    private SunShaderUtils.SunHaloSettings startHalo, redHalo, whiteHalo, superHalo;

    private Vector3 initialScale;

    // ---------- Swallowing system ----------
    private struct BodyToSwallow
    {
        public Transform transform;
        public float radius;
    }
    private List<BodyToSwallow> bodiesToSwallow = new ();

    
    private float sunBaseRadius;
    private float sunRadiusPerUnitScale;
    

    public SupernovaSun(IntPtr ptr) : base(ptr) { }

    public void SetBodiesToSwallow(List<Transform> transforms)
    {
        bodiesToSwallow.Clear();
        foreach (Transform t in transforms)
        {
            if (t == null) continue;
            float radius = CalculateWorldRadius(t);
            bodiesToSwallow.Add(new BodyToSwallow { transform = t, radius = radius });
        }
    }

    // ---------- Unity lifecycle ----------
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
        {
            sunlightOriginal = sunLight.color;
            originalLightIntensity = sunLight.intensity;
        }
            

        MelonCoroutines.Start(InitializeAfterFrame());

        sunBaseRadius = transform.GetChild(0).GetComponent<Renderer>().bounds.extents.magnitude;
        sunBaseRadius = Mathf.Max(
            sunRenderer.bounds.extents.x,
            sunRenderer.bounds.extents.y,
            sunRenderer.bounds.extents.z
        );
        sunRadiusPerUnitScale = sunBaseRadius / initialScale.x;

        // Register required targets — radius is calculated once here, never again.
        RequiredTarget CreateRequiredTarget(GameObject go)
        {
            return new RequiredTarget
            {
                transform = go.transform,
                radius = CalculateWorldRadius(go.transform),
                engulfed = false,
            };
        }

        requiredTarget = CreateRequiredTarget(Main.solarSystem.WhiteHole);
    }
    
    public IEnumerator FindPlayerAndSetup()
    {
        while (Calls.Players.GetLocalPlayer() == null || 
               Calls.Players.GetLocalPlayer().Controller == null || 
               Calls.Players.GetLocalPlayer().Controller.PlayerVisuals == null)
        {
            // Abort if the scene changes while we are waiting
            if (this == null) yield break; 
        
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);
    
        if (this == null) yield break;

        playerTransform = Calls.Players.GetLocalPlayer().Controller.PlayerVisuals.transform.GetChild(1);
    }

    void FixedUpdate()
    {
        if (!sunMaterial || !haloMaterial) return;

        phaseTimer += Time.deltaTime;

        switch (currentPhase)
        {
            case Phase.Red:
                UpdateRedPhase();
                break;
            case Phase.RedFullWait:
                UpdateRedFullWaitPhase();
                break;
            case Phase.Collapse:
                UpdateCollapsePhase();
                break;
            case Phase.Explosion:
                UpdateExplosionPhase();
                break;
            case Phase.Wall:
                UpdateWallPhase();
                break;
        }
    }

    // ---------- Phase update methods ----------
    private void UpdateRedPhase()
    {
        float t = Mathf.Clamp01(phaseTimer / secondsToFullRed);
        
        float sizeT = Mathf.SmoothStep(0f, 1f, t);
        transform.localScale = Vector3.Lerp(initialScale, initialScale + redGrowthScale, sizeT);

        // COLOR: Use an exponential "Ease-In" curve. 
        // Mathf.Pow(t, 2f) or Mathf.Pow(t, 3f) will make the sun stay its original 
        // color for much longer, only aggressively turning red near the end.
        float colorT = Mathf.Pow(t, 3f); 

        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(startCore, redCore, colorT));
        SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(startHalo, redHalo, colorT));
        sunLight.color = Color.Lerp(sunlightOriginal, sunlightRed, colorT);

        if (t >= 1f)
            OnRedFull();
    }

    private void UpdateRedFullWaitPhase()
    {
        SunShaderUtils.ApplyCore(sunMaterial, redCore);
        SunShaderUtils.ApplyHalo(haloMaterial, redHalo);
        sunLight.color = sunlightRed;
        transform.localScale = initialScale + redGrowthScale;

        GameObject inteloper = Main.solarSystem.Interloper;

        if (inteloper.activeSelf && Vector3.Distance(inteloper.transform.position,transform.position) < 3.2f)
        {
            inteloper.SetActive(false);
        }
    }

    private void UpdateCollapsePhase()
    {
        float t = Mathf.Clamp01(phaseTimer / collapseDuration);
        t = Mathf.SmoothStep(0f, 1f, t);

        transform.localScale = Vector3.Lerp(initialScale + redGrowthScale, collapseScale, t);

        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(redCore, whiteCore, t));
        SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(redHalo, whiteHalo, t));
        sunLight.color = Color.Lerp(sunlightRed, sunlightWhite, t);
    }

    private void UpdateExplosionPhase()
    {
        float t = Mathf.Clamp01(phaseTimer / explosionDuration);
        transform.localScale = Vector3.Lerp(collapseScale, explosionTargetScale, t);

        SunShaderUtils.ApplyCore(sunMaterial, LerpCore(whiteCore, superCore, t));
        SunShaderUtils.ApplyHalo(haloMaterial, LerpHalo(whiteHalo, superHalo, t));
        sunLight.color = Color.Lerp(sunlightWhite, sunlightBlue, t);

        // Keep updating wall-sound volume during explosion so the ramp is seamless.
        UpdateWallClipVolume();
    }

    // ---------- Modified wall phase with continuous expansion & dynamic volume ----------
    private void UpdateWallPhase()
    {
        // Keep core/halo super state
        SunShaderUtils.ApplyCore(sunMaterial, superCore);
        SunShaderUtils.ApplyHalo(haloMaterial, superHalo);
        sunLight.color = sunlightBlue;

        float currentRadius = sunRadiusPerUnitScale * transform.localScale.x;
        Vector3 sunPos = transform.position;

        // Swallow other bodies
        for (int i = bodiesToSwallow.Count - 1; i >= 0; i--)
        {
            BodyToSwallow body = bodiesToSwallow[i];
            Transform tr = body.transform;
            if (!tr.gameObject.activeSelf) continue;
            float dist = Vector3.Distance(tr.position, sunPos);
            if (currentRadius >= dist + body.radius + extraDistance)
            {
                tr.gameObject.SetActive(false);
            }
        }

        if (isFadingOut) return;

        // Single target logic
        float farthestRequired = 0f;
        
        if (requiredTarget.engulfed || !requiredTarget.transform.gameObject.activeSelf)
        {
            requiredTarget.engulfed = true;
        }
        else
        {
            // 2. Calculate distance logic for the single target
            float dist = Vector3.Distance(sunPos, requiredTarget.transform.position);
            float needed = dist + requiredTarget.radius + extraDistance;

            if (currentRadius >= needed)
            {
                requiredTarget.engulfed = true;
            }
            else
            {
                farthestRequired = needed;
            }
        }

        // Check player
        float distToPlayer = Vector3.Distance(sunPos, playerTransform.position);
        if (!hasReachedPlayer)
        {
            if (currentRadius >= distToPlayer + extraDistance)
                hasReachedPlayer = true;
            else
                farthestRequired = Mathf.Max(farthestRequired, distToPlayer + extraDistance);
        }
        UpdateWallClipVolume();
        
        if (requiredTarget.engulfed)
        {
            if (Main.solarSystem.DarkBramble.activeSelf)
                Main.solarSystem.DarkBramble.SetActive(false);
            MelonCoroutines.Start(FadeOutAndDisable());
        }
        else
        {
            float newWorldRadius = currentRadius + (expansionSpeedWorldUnitsPerSec * Time.deltaTime);
            if (newWorldRadius > farthestRequired)
                newWorldRadius = farthestRequired;

            float newScaleX = newWorldRadius / sunRadiusPerUnitScale;
            transform.localScale = new Vector3(newScaleX, newScaleX, newScaleX);
        }
    }

    // ---------- Shared volume helper ----------
    // Called each frame during both Explosion and Wall phases so the
    // volume ramps smoothly from wallBaseVolume (inaudible-ish) up to 1.0
    // based purely on how close the expanding wall is to the player.
    private void UpdateWallClipVolume()
    {
        if (wallClip == null) return;

        float currentRadius = sunRadiusPerUnitScale * transform.localScale.x;
        float distToPlayer  = Vector3.Distance(transform.position, playerTransform.position);
        float surfaceDistance = distToPlayer - currentRadius;

        float volume;
        if (surfaceDistance <= 0f)
            volume = 1f;
        else
            volume = Mathf.Lerp(wallBaseVolume, 1f, 1f - Mathf.Clamp01(surfaceDistance / wallRampRange));

        wallClip.Reader.Volume = volume;
    }

    // ---------- Coroutines & helpers ----------
    private IEnumerator InitializeAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        if (sunMaterial != null)
        {
            startCore = SunShaderUtils.ReadCore(sunMaterial);
            redCore = startCore;
            redCore.Color1 = new Color(0.65f, 0.189f, 0);
            redCore.Color2 = new Color(0.7f, 0.156f, 0.0f);
            redCore.SunBright = 1.8f;

            whiteCore = startCore;
            whiteCore.Color1 = Color.white;
            whiteCore.Color2 = Color.white;
            whiteCore.Color3 = Color.white;
            whiteCore.Color4 = Color.white;
            whiteCore.SunBright = 2.5f;

            superCore = startCore;
            superCore.SunBright = 1.8f;
            superCore.Color2 = Color.white;
            superCore.Color1 = Color.cyan;
            superCore.Color3 = Color.mediumBlue;
            superCore.Color4 = Color.deepSkyBlue;
        }

        if (haloMaterial != null)
        {
            startHalo = SunShaderUtils.ReadHalo(haloMaterial);
            redHalo = startHalo;
            redHalo.HaloRing1Color = new Color(0.8f, 0.2f, 0.01f);
            redHalo.HaloRing2Color = new Color(0.749f, 0.112f, 0);

            whiteHalo = startHalo;
            whiteHalo.HaloRing1Color = Color.white;
            whiteHalo.HaloRing2Color = Color.white;

            superHalo = startHalo;
            superHalo.HaloRing1Color = Color.darkCyan;
            superHalo.HaloRing2Color = Color.cyan;
        }
    }

    private void OnRedFull()
    {
        if (currentPhase != Phase.Red) return;
        Main.solarSystem.SunStation.gameObject.SetActive(false); //TODO swallow interloper
        
        AudioManager.PlaySoundIfFileExists(Path.Combine(Main.folderPath, endTimesSoundName), 0.2f);
        currentPhase = Phase.RedFullWait;
        phaseTimer = 0f;
        MelonCoroutines.Start(HandlePostRedSequence());
    }

    private IEnumerator HandlePostRedSequence()
    {
        yield return new WaitForSeconds(waitAfterRed);

        currentPhase = Phase.Collapse;
        phaseTimer = 0f;
        AudioManager.PlaySoundIfFileExists(Path.Combine(Main.folderPath, supernovaCollapseSoundName));
    
        Transform collapseObject = gameObject.transform.GetChild(1);
        Vector3 originalScale = collapseObject.localScale;
        float elapsed = 0f;

        while (elapsed < collapseDuration)
        {
            elapsed += Time.deltaTime;
            collapseObject.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / collapseDuration);
            yield return null;
        }

        collapseObject.gameObject.SetActive(false);
        collapseObject.localScale = originalScale;

        yield return new WaitForSeconds(0.2f);

        currentPhase = Phase.Explosion;
        phaseTimer = 0f;
        AudioManager.PlaySoundIfFileExists(Path.Combine(Main.folderPath, supernovaExplosionSoundName), 1f, false);
        wallClip = AudioManager.PlaySoundIfFileExists(Path.Combine(Main.folderPath, supernovaWallSoundName), wallBaseVolume, true);
        yield return new WaitForSeconds(explosionDuration);

        // ✅ Force final state before entering Wall phase — no white flash
        SunShaderUtils.ApplyCore(sunMaterial, superCore);
        SunShaderUtils.ApplyHalo(haloMaterial, superHalo);
        sunLight.color = sunlightBlue;
        transform.localScale = explosionTargetScale;

        currentPhase = Phase.Wall;
        phaseTimer = 0f;
        float currentRadius = sunRadiusPerUnitScale * transform.localScale.x;
        wallRampRange = Mathf.Max(Vector3.Distance(transform.position, playerTransform.position) - currentRadius, 1f);
    }

    private IEnumerator FadeOutAndDisable()
    {
        isFadingOut = true;

        float fadeDuration = 4f;

        if (wallClip != null)
            AudioManager.FadeOut(wallClip, fadeDuration, 0, wallClip.Reader.Volume, true);

        float startCoreAlpha = sunMaterial.GetFloat("_Alpha");
        float startHaloAlpha = haloMaterial.GetFloat("_Alpha");
        float startLightIntensity = sunLight.intensity;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));

            sunMaterial.SetFloat("_Alpha", Mathf.Lerp(startCoreAlpha, 0f, t));
            haloMaterial.SetFloat("_Alpha", Mathf.Lerp(startHaloAlpha, 0f, t));
            sunLight.intensity = Mathf.Lerp(startLightIntensity, 0f, t);

            yield return null;
        }

        sunMaterial.SetFloat("_Alpha", 0f);
        haloMaterial.SetFloat("_Alpha", 0f);
        sunLight.intensity = 0f;

        gameObject.SetActive(false);
        currentPhase = Phase.Done;
    }

    private float CalculateWorldRadius(Transform obj)
    {
        Renderer[] allRenderers = obj.GetComponentsInChildren<Renderer>();
        Vector3 pivot = obj.position;
        float maxDist = 0f;

        foreach (Renderer r in allRenderers)
        {
            string name = r.gameObject.name;
            if (name.Contains("Proxy") || name.Contains("Sand"))
                continue;

            Vector3 center = r.bounds.center;
            Vector3 extents = r.bounds.extents;
            float dist = (center - pivot).magnitude + extents.magnitude;
            if (dist > maxDist)
                maxDist = dist;
        }

        return maxDist > 0f ? maxDist : 0.25f;
    }

    // ---------- Lerp helpers (unchanged) ----------
    private SunShaderUtils.SunCoreSettings LerpCore(SunShaderUtils.SunCoreSettings a, SunShaderUtils.SunCoreSettings b, float t)
    {
        return new SunShaderUtils.SunCoreSettings
        {
            SunBright = Mathf.Lerp(a.SunBright, b.SunBright, t),
            SunSpeed = a.SunSpeed,
            Color1 = Color.Lerp(a.Color1, b.Color1, t),
            Color2 = Color.Lerp(a.Color2, b.Color2, t),
            Color3 = Color.Lerp(a.Color3, b.Color3, t),
            Color4 = Color.Lerp(a.Color4, b.Color4, t),
        };
    }

    private SunShaderUtils.SunHaloSettings LerpHalo(SunShaderUtils.SunHaloSettings a, SunShaderUtils.SunHaloSettings b, float t)
    {
        return new SunShaderUtils.SunHaloSettings
        {
            HaloRing1 = a.HaloRing1,
            HaloRing1Color = Color.Lerp(a.HaloRing1Color, b.HaloRing1Color, t),
            HaloRing1Size = a.HaloRing1Size,
            HaloRing1Intensity = a.HaloRing1Intensity,
            HaloRing1Strength = a.HaloRing1Strength,

            HaloRing2 = a.HaloRing2,
            HaloRing2Str = a.HaloRing2Str,
            HaloRing2Thickness = a.HaloRing2Thickness,
            HaloRing2Color = Color.Lerp(a.HaloRing2Color, b.HaloRing2Color, t),
            HaloRing2Size = a.HaloRing2Size,
            HaloRing2Intensity = a.HaloRing2Intensity,
            HaloRing2Width = a.HaloRing2Width,
        };
    }
    
    /// <summary>
    /// Resets the sun completely, but ONLY if it has already finished exploding (phase Done).
    /// Call this when loading a new scene to prepare the sun for a fresh cycle.
    /// </summary>
    public void ResetAfterExplosion()
    {
        if (currentPhase != Phase.Done)
            return;

        // Kill wall sound
        if (wallClip != null)
        {
            AudioManager.FadeOut(wallClip, 0f, 0f, 0f, true);
            wallClip = null;
        }

        requiredTarget.transform.gameObject.SetActive(true);
        

        // Swallowed bodies
        foreach (var body in bodiesToSwallow)
        {
            if (body.transform != null && !body.transform.gameObject.activeSelf)
                body.transform.gameObject.SetActive(true);
        }

        // Reset state
        currentPhase = Phase.Red;
        phaseTimer = 0f;
        hasReachedPlayer = false;
        isFadingOut = false;

        // Restore visuals
        transform.localScale = initialScale;

        if (sunMaterial != null)
            SunShaderUtils.ApplyCore(sunMaterial, startCore);
        if (haloMaterial != null)
            SunShaderUtils.ApplyHalo(haloMaterial, startHalo);

        if (sunLight != null)
        {
            sunLight.color = sunlightOriginal;
            sunLight.intensity = originalLightIntensity;
        }
        
        gameObject.transform.GetChild(1).localScale = Vector3.one;
        gameObject.transform.GetChild(1).gameObject.SetActive(true);
        
        
        sunMaterial.SetFloat("_Alpha", 1f);
        haloMaterial.SetFloat("_Alpha", 1f);

        requiredTarget.engulfed = false;

        // Re-enable the sun itself
        gameObject.SetActive(true);

        // Re-read shader defaults on next frame
        MelonCoroutines.Start(InitializeAfterFrame());
    }
}