using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using MelonLoader.Utils;
using OuterWildsRumble.Components;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.VFX;
using BuildInfo = OuterWildsRumble.BuildInfo;
using Object = UnityEngine.Object;

[assembly: MelonInfo(typeof(OuterWildsRumble.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

namespace OuterWildsRumble
{
    public static class BuildInfo
    {
        public const string ModName = "OuterWildsRumble";
        public const string ModVersion = "1.0.0";
        public const string Description = "For my fellow hatchlings";
        public const string Author = "oreotrollturbo";
        public const string Company = "";
    }

    public class Main : MelonMod
    {
        public static SolarSystemData solarSystem;

        public static float heightOffset = 260f;
        public static Vector3 systemCenter = new Vector3(-32f, 5.5f + heightOffset, 0f);
        public static Camera playerCam;
        
        const string outerWildsBundlePath = "OuterWildsRumble.OuterWildsStuff.outerwilds";
        const string eventHorizonBundlePath = "OuterWildsRumble.OuterWildsStuff.eventhorizon";
        
        public static Shader atmosphereShader;
        public static Material copyDepthMaterial;
        public static AtmosphereProfile defaultAtmosphereProfile;
        public static ComputeShader opticalDepth;
        
        public static ScriptableRendererData activeRenderData;

        public static Shader replacementShader;
        
        public static string folderPath = MelonEnvironment.UserDataDirectory + @"\OuterWildsRumble";
        
        public override void OnLateInitializeMelon()
        {
            if (!Directory.Exists(folderPath))
            {
                MelonLogger.Warning("File at " + folderPath + " does not exist");
                Directory.CreateDirectory(folderPath);
            }
            
            Actions.onMapInitialized += SceneLoaded;
            ClassInjector.RegisterTypeInIl2Cpp<Orbiter>();
            ClassInjector.RegisterTypeInIl2Cpp<EllipticalOrbiter>();
            ClassInjector.RegisterTypeInIl2Cpp<QuantumObject>();
            ClassInjector.RegisterTypeInIl2Cpp<HourGlassTwins>();
            ClassInjector.RegisterTypeInIl2Cpp<SolarSystem>();
            ClassInjector.RegisterTypeInIl2Cpp<SignalScope>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereEffect>();
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereProfile>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereRendererFeatureTest>();
            //ClassInjector.RegisterTypeInIl2Cpp<DepthStackRenderFeature>();
            //ClassInjector.RegisterTypeInIl2Cpp<TestRenderFeature>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereRenderPass>();
            //ClassInjector.RegisterTypeInIl2Cpp<BlitEndRenderPass>();
            //ClassInjector.RegisterTypeInIl2Cpp<BlitStartRenderPass>();
            //ClassInjector.RegisterTypeInIl2Cpp<DepthStackRenderPass>();
            
            //ClassInjector.RegisterTypeInIl2Cpp<AtmosphereRendererFeatureTest>();

            //atmosphereShader = AssetBundles.LoadAssetFromStream<Shader>(this, outerWildsBundlePath, "AtmosphereNew");
            //copyDepthMaterial = AssetBundles.LoadAssetFromStream<Material>(this, outerWildsBundlePath, "CopyDepth");
            //opticalDepth = AssetBundles.LoadAssetFromStream<ComputeShader>(this, outerWildsBundlePath, "OpticalDepth");
            //defaultAtmosphereProfile =  AtmosphereProfileFactory.CreateDefaultAtmosphereProfile(opticalDepth);
            
            //Object.DontDestroyOnLoad(atmosphereShader);
            //Object.DontDestroyOnLoad(defaultAtmosphereProfile);
            //Object.DontDestroyOnLoad(opticalDepth);
            //atmosphereShader.hideFlags = HideFlags.DontUnloadUnusedAsset;
            //defaultAtmosphereProfile.hideFlags = HideFlags.DontUnloadUnusedAsset;
            //opticalDepth.hideFlags = HideFlags.DontUnloadUnusedAsset;
            
            //ScriptableObject.CreateInstance<TestRenderFeature>().hideFlags = HideFlags.DontUnloadUnusedAsset; //TODO
            
            //AtmosphereSetup.InjectAtmosphereFeatureAtRuntime(atmosphereShader);
            //AtmosphereSetup.AddAtmosphereFeatureAtRuntime();TODO
            //AtmosphereSetup.Test();
            //AtmospherePassManager.Init(atmosphereShader);
            
            replacementShader = AssetBundles.LoadAssetFromStream<Shader>(this, outerWildsBundlePath, "ReplacementShader");
            replacementShader.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Object.DontDestroyOnLoad(replacementShader);

            if (replacementShader == null)
            {
                MelonLogger.Error("SHADER IS NULL NOOOOOOOOO");
            }
        }
        

        private void SceneLoaded(string mapName)
        {
            // var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // cube.name = "ShaderHolder";
            // cube.GetComponent<MeshRenderer>().material.shader = replacementShader;

            // string shaderName = GameObject.Find("SCENE").transform.GetChild(0).GetChild(0).gameObject.GetComponent<MeshRenderer>().material.shader.name;
            // MelonLogger.Msg("Shader name is: " + shaderName);
            // "Shader Graphs/MobileEnvironmentUV0"
            
            ReplaceAllShaders();
            
            RenderSettings.fog = false; 
            playerCam = Camera.main;
            switch (mapName)
            {
                case "Gym":
                    if (solarSystem.Root == null)
                    {
                        SetupSolarSystem();
                    }
                    solarSystem.Root.transform.position = new Vector3(2.28f, 16.06f + heightOffset, -306.16f);
                    solarSystem.Root.transform.rotation = Quaternion.Euler(0, 0, 0);
                    GameObject.Find("Player Controller(Clone)").transform.GetChild(2).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);
                    
                    solarSystem.PlayerShip.transform.position = new Vector3(-35.4736f, 10.75f, -15.9372f);
                    solarSystem.PlayerShip.transform.rotation = Quaternion.Euler(-0, 163f, 0);
                    break;
                
                case "Map0":
                    solarSystem.Root.transform.position = new Vector3(325.45f,76f + heightOffset,240.54f);
                    solarSystem.Root.transform.rotation = Quaternion.Euler(0, 0, 0);

                    Vector3 pos = new Vector3(15.7553f, 5.7286f, 29.0445f);
                    Quaternion rot = Quaternion.Euler(349.3421f, 295.3221f, 358.0536f);
                    if (!Calls.Players.IsHost())
                    {
                        pos = new Vector3(22.8553f, 23.9273f, -28.9091f);
                        rot = Quaternion.Euler(6f, 53.4914f, 359.1993f);
                    }
                    
                    solarSystem.PlayerShip.transform.position = pos;
                    solarSystem.PlayerShip.transform.rotation = rot;
                    break;
                
                case "Map1":
                    solarSystem.Root.transform.position = new Vector3(0,20.5f + heightOffset,0);
                    solarSystem.Root.transform.rotation = Quaternion.Euler(0, 0, 0);
                    
                    solarSystem.PlayerShip.transform.position = new Vector3(-34.4782f, 19.8146f, -14.1971f);
                    solarSystem.PlayerShip.transform.rotation = Quaternion.Euler(359.8188f, 143.5267f, 6.4373f);
                    break;
                
                case "Park":
                    solarSystem.Root.transform.position = new Vector3(373.32f, 471f + heightOffset, 520.5325f);
                    solarSystem.Root.transform.rotation = Quaternion.Euler(0, 124.1311f, 0);
                    
                    solarSystem.PlayerShip.transform.position = new Vector3(-17.3906f, 5.0094f, -24.2655f);
                    solarSystem.PlayerShip.transform.rotation = Quaternion.Euler(0f, 88.8748f, 356.2899f);
                    break;
            }
            
            var SignalScope         = LoadAndSpawn("SignalscopeGO"); 
            SignalScope.AddComponent<SignalScope>();
            SignalScope.transform.localScale = new Vector3(2,2,2);
        }

        public void ReplaceAllShaders()
        {
            MelonLogger.Msg("Starting shader replacement");

            if (replacementShader == null)
            {
                MelonLogger.Error("Replacement Shader variable is not assigned!");
                return;
            }

            foreach (Renderer item in Object.FindObjectsOfType<Renderer>())
            {
                Material sharedMaterial = item.sharedMaterial;
                if ((Object)(object)sharedMaterial == (Object)null)
                {
                    continue;
                }
                if (((Object)sharedMaterial.shader).name == "Shader Graphs/MobileEnvironmentUV0")
                {
                    Shader val = replacementShader;
                    if ((Object)(object)item.sharedMaterial.shader != (Object)(object)val)
                    {
                        Material val2 = new Material(val);
                        Texture texture = sharedMaterial.GetTexture("_TEXTURE");
                        val2.SetTexture("Texture2D_2058E65A", texture);
                        val2.SetTexture("Texture2D_3812B1EC", texture);
                        val2.SetColor("Color_D943764B", Color.white);
                        item.sharedMaterial = val2;
                    }
                }
                item.lightmapIndex = -1;
                item.lightProbeUsage = (LightProbeUsage)0;
                item.reflectionProbeUsage = (ReflectionProbeUsage)0;
            }
        }


        void SetupSolarSystem()
        {
            solarSystem.Root = new GameObject("OuterWilds_System");
            solarSystem.Root.AddComponent<SolarSystem>();
            
            solarSystem.Root.transform.position = systemCenter;
                
            GameObject.DontDestroyOnLoad(solarSystem.Root);
            
            MelonLogger.Msg("Loading assets");
            LoadAssets();
            
            MelonLogger.Msg("Creating solar system");
            CreateSun();
            CreateWhiteHole();
            SetupOrbitals();
            
            if (solarSystem.Sun != null) solarSystem.Sun.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.SunStation != null) solarSystem.SunStation.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.HourGlassTwins != null) solarSystem.HourGlassTwins.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.TimberHearth != null) solarSystem.TimberHearth.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.Attlerock != null) solarSystem.Attlerock.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.BrittleHollow != null) solarSystem.BrittleHollow.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.HollowsLantern != null) solarSystem.HollowsLantern.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.GiantsDeep != null) solarSystem.GiantsDeep.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.OrbitalProbeCannon != null) solarSystem.OrbitalProbeCannon.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.QuantumMoon != null) solarSystem.QuantumMoon.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.DarkBramble != null) solarSystem.DarkBramble.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.WhiteHole != null) solarSystem.WhiteHole.transform.SetParent(solarSystem.Root.transform, true);
            if (solarSystem.WhiteHoleStation != null) solarSystem.WhiteHoleStation.transform.SetParent(solarSystem.Root.transform, true);
                
            if (solarSystem.Interloper != null) solarSystem.Interloper.transform.SetParent(solarSystem.Root.transform, true);
            
            //if (solarSystem.PlayerShip != null) solarSystem.PlayerShip.transform.SetParent(solarSystem.Root.transform, true);
            
            
            solarSystem.Root.GetComponent<SolarSystem>().Scale(30f);
            solarSystem.Root.transform.position += new Vector3(0, 260f, 0);
            
            MelonLogger.Msg($"Finished setup!");
        }
        
        

        public void LoadAssets()
        {
            // Helper to load, instantiate, and log errors in one go
            

            Material GetMaterial(string assetName)
            {
                var material = AssetBundles.LoadAssetFromStream<Material>(this, eventHorizonBundlePath, assetName);
                if (material != null)
                {
                    MelonLogger.Msg($"Loaded: {assetName}");
                    return material;
                }

                MelonLogger.Error($"Failed to load material: {assetName}");
                return null;
            }

            solarSystem.Sun = LoadAndSpawn("SunV2");
            solarSystem.SunStation         = LoadAndSpawn("SunStation");
            solarSystem.HourGlassTwins = LoadAndSpawn("HourGlassTwinsGO");
            solarSystem.TimberHearth   = LoadAndSpawn("TimberHearth");
            solarSystem.Attlerock      = LoadAndSpawn("Attlerock");
            solarSystem.BrittleHollow      = LoadAndSpawn("BrittleHollowHollow");
            solarSystem.HollowsLantern     = LoadAndSpawn("HollowsLantern");
            solarSystem.GiantsDeep     = LoadAndSpawn("GiantsDeep");
            solarSystem.OrbitalProbeCannon = LoadAndSpawn("OrbitalProbeCannon");
            solarSystem.QuantumMoon        = LoadAndSpawn("QuantumMoon");
            solarSystem.DarkBramble        = LoadAndSpawn("DarkBramble");
            solarSystem.WhiteHoleStation   = LoadAndSpawn("WhiteHoleStation");
            solarSystem.Interloper         = LoadAndSpawn("InterloperGameObject");
            
            solarSystem.PlayerShip         = LoadAndSpawn("HearthianSpaceShip");
            GameObject.DontDestroyOnLoad(solarSystem.PlayerShip);
            
            
            solarSystem.WhiteHoleMaterial  = GetMaterial("WhiteHoleMaterial");
            solarSystem.BlackHoleMaterial  = GetMaterial("BlackholeMaterial");

            FixSolarSystemShaders();

            //Test stuff
            //LoadAndSpawn("AtmospherePlanet");
        }
        
        GameObject LoadAndSpawn(string assetName)
        {
            var asset = AssetBundles.LoadAssetFromStream<GameObject>(this, outerWildsBundlePath, assetName);
            if (asset != null)
            {
                MelonLogger.Msg($"Loaded: {assetName}");
                return GameObject.Instantiate(asset);
            }
            MelonLogger.Error($"Failed to load asset: {assetName}");
            return null;
        }
        
        public static void FixShaders(GameObject spawnedObject)
        {
            // Use "UI/Default" for full brightness
            // Use "Universal Render Pipeline/Lit" for the games dynamic lighting !
            string shaderName = "Universal Render Pipeline/Lit";

            Shader vanillaShader = Shader.Find(shaderName);

            if (vanillaShader == null)
            {
                MelonLogger.Warning($"Could not find shader: {shaderName}");
                return;
            }

            Renderer[] allRenderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
    
            // Check if this is the HourGlassTwins once at the start
            bool isTwins = spawnedObject.name.Contains("HourGlassTwins");

            foreach (Renderer renderer in allRenderers)
            {
                if (isTwins && renderer.gameObject.name.Contains("Proxy"))
                {
                    MelonLogger.Msg("Skipping Sand on HourGlassTwins");
                    continue;
                }

                foreach (Material mat in renderer.materials)
                {
                    if (mat.name.ToLower().Contains("sand") || mat.shader.name.ToLower().Contains("sand"))
                    {
                        MelonLogger.Msg("Found sand, ignoring");
                        continue;
                    }
                    mat.shader = vanillaShader;
                }
            }

            MelonLogger.Msg($"Successfully updated shaders on {spawnedObject.name} to {shaderName}!");
        }
        
        public static void FixSolarSystemShaders()
        {
            MelonLogger.Msg("Starting shader fix for the Solar System...");
            
            //pass each GameObject field into the FixShaders method.
            if (solarSystem.Root != null) FixShaders(solarSystem.Root);
            
            if (solarSystem.SunStation != null) FixShaders(solarSystem.SunStation);
    
            if (solarSystem.HourGlassTwins != null) FixShaders(solarSystem.HourGlassTwins);
    
            if (solarSystem.TimberHearth != null) FixShaders(solarSystem.TimberHearth);
            if (solarSystem.Attlerock != null) FixShaders(solarSystem.Attlerock);
    
            if (solarSystem.BrittleHollow != null) FixShaders(solarSystem.BrittleHollow);
            if (solarSystem.HollowsLantern != null) FixShaders(solarSystem.HollowsLantern);
    
            //if (solarSystem.GiantsDeep != null) FixShaders(solarSystem.GiantsDeep);
            if (solarSystem.OrbitalProbeCannon != null) FixShaders(solarSystem.OrbitalProbeCannon);
            if (solarSystem.QuantumMoon != null) FixShaders(solarSystem.QuantumMoon);
    
            if (solarSystem.DarkBramble != null) FixShaders(solarSystem.DarkBramble);
    
            if (solarSystem.WhiteHole != null) FixShaders(solarSystem.WhiteHole);
            if (solarSystem.WhiteHoleStation != null) FixShaders(solarSystem.WhiteHoleStation);
    
            if (solarSystem.Interloper != null) FixShaders(solarSystem.Interloper);
            if (solarSystem.PlayerShip != null) FixShaders(solarSystem.PlayerShip);

            MelonLogger.Msg("Solar System shaders successfully updated!");
        }

        void CreateSun()
        {
            solarSystem.Sun.transform.position = solarSystem.Root.transform.position;
            solarSystem.Sun.transform.localScale = Vector3.one * 1.65f;
            solarSystem.Sun.name = "Sun";
    
            // let there be light
            Light sunLight = solarSystem.Sun.AddComponent<Light>();
            sunLight.type = LightType.Point;       
            sunLight.range = 15000f;                 
            sunLight.intensity = 18000f;               
            sunLight.color = new Color(1f, 0.8392f, 0.7098f, 1f);
            sunLight.shadows = LightShadows.Soft;

            SupernovaSun supernovaSun = solarSystem.Sun.AddComponent<SupernovaSun>();
            supernovaSun.sunLight = sunLight;


            // var mat = solarSystem.Sun.GetComponent<Renderer>().material;
            // var shader = mat.shader;

            // int count = shader.GetPropertyCount();
            // MelonLogger.Msg("SUN CORE PROPERTIES");
            // for (int i = 0; i < count; i++)
            // {
            //     MelonLogger.Msg("Shader property:" + shader.GetPropertyName(i));
            // }
            //
            //
            // var mat1 = solarSystem.Sun.transform.GetChild(0).GetComponent<Renderer>().material;
            // var shader1 = mat1.shader;
            //
            // int count1 = shader1.GetPropertyCount();
            // MelonLogger.Msg("SUN HALO PROPERTIES");
            // for (int i = 0; i < count1; i++)
            // {
            //     MelonLogger.Msg("Shader property:" + shader1.GetPropertyName(i));
            // }
        }

        void CreateWhiteHole()
        {
            solarSystem.WhiteHole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            solarSystem.WhiteHole.transform.position = solarSystem.Root.transform.position + new Vector3(0,0, 17.6f);
            solarSystem.WhiteHole.transform.localScale = Vector3.one * 0.14f;
            solarSystem.WhiteHole.name = "WhiteHole";

            Renderer r = solarSystem.WhiteHole.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = solarSystem.WhiteHoleMaterial;
            }
            
            Light whiteHoleLight = solarSystem.WhiteHole.AddComponent<Light>();
            whiteHoleLight.type = LightType.Point;       
            whiteHoleLight.range = 300f;
            whiteHoleLight.intensity = 130f;
            whiteHoleLight.color = new Color(1f, 1f, 1f, 1f);
            whiteHoleLight.shadows = LightShadows.Soft;  
        }

        void SetupOrbitals()
        {
            //Planets from sun 4.5 7.9 10.85 15.35 18.6 21.5
            //2.28 4.0 5.49 7.77 9.42 10.89
            
            //Moons from planets 0.85 0.97 1.5
            //0.430 0.491 0.759
            
            if (solarSystem.Sun != null)
            {
                SetupSunStation();
                SetupHourGlassTwins();
                SetupTimberHearth();
                SetupBrittleHollow();
                SetupGiantsDeep();
                SetupDarkBramble();
                SetupWhiteHoleStation();
                SetupInterloper();
                // must be called after "parent" planets
                SetupQuantumMoon();
                
                SetupPlayerShip();
                
                MusicEmitter emitter = solarSystem.HourGlassTwins.AddComponent<MusicEmitter>();
                emitter.musicFileName = "OW_TravelerTheme_drums.wav"; 
                
                MusicEmitter emitter1 = solarSystem.Attlerock.AddComponent<MusicEmitter>();
                emitter1.musicFileName = "OW_TravelerTheme_whistling.wav";
                
                MusicEmitter emitter2 = solarSystem.BrittleHollow.AddComponent<MusicEmitter>();
                emitter2.musicFileName = "OW_TravelerTheme_banjo.wav";
                
                MusicEmitter emitter3 = solarSystem.GiantsDeep.AddComponent<MusicEmitter>();
                emitter3.musicFileName = "OW_TravelerTheme_flute.wav"; 
                
                MusicEmitter emitter4 = solarSystem.DarkBramble.AddComponent<MusicEmitter>();
                emitter4.musicFileName = "OW_TravelerTheme_harmonica.wav";
                
                MusicEmitter emitter5 = solarSystem.QuantumMoon.AddComponent<MusicEmitter>();
                emitter5.musicFileName = "OW_TravelerTheme_piano.wav";
            }
        }

        void SetupSunStation()
        {
            GameObject sunStationPivot = new GameObject("SunStationPivot");
            
            solarSystem.SunStation.transform.SetParent(sunStationPivot.transform, false);
            solarSystem.SunStation.transform.localScale = Vector3.one * 0.047f;
            solarSystem.SunStation.transform.localPosition = Vector3.zero; 
            solarSystem.SunStation.transform.Rotate(0, 180f, 0);
            
            Orbiter sunStation = sunStationPivot.AddComponent<Orbiter>();

            sunStation.randomisePos = false;
            sunStation.orbitParent = solarSystem.Sun.transform;  
            sunStation.orbitDistance = 23/30f;
            sunStation.orbitSpeed = 16f;     
            sunStation.spinSpeed = 16f;
            sunStation.orbitAxis = Vector3.up;
            
            solarSystem.SunStation = sunStationPivot;
        }

        void SetupHourGlassTwins()
        {
            solarSystem.HourGlassTwins.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter hourGlassTwins = solarSystem.HourGlassTwins.AddComponent<Orbiter>();
            
            hourGlassTwins.orbitParent = solarSystem.Sun.transform;  
            hourGlassTwins.orbitDistance = 3.88f;           
            hourGlassTwins.orbitSpeed = 2.27f;          
            hourGlassTwins.spinSpeed = 20.5f;
            hourGlassTwins.orbitAxis = Vector3.up;

            HourGlassTwins sandComponent = solarSystem.HourGlassTwins.AddComponent<HourGlassTwins>();
            //Keeping default settings
        }

        void SetupTimberHearth()
        {
            solarSystem.TimberHearth.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter heartOrbit = solarSystem.TimberHearth.AddComponent<Orbiter>();
            
            heartOrbit.orbitParent = solarSystem.Sun.transform;  
            heartOrbit.orbitDistance = 5.6f;           
            heartOrbit.orbitSpeed = 1f;             
            heartOrbit.spinSpeed = 7.5f;       
            heartOrbit.orbitAxis = Vector3.up;
            
            //AtmosphereEffect atmosphere = solarSystem.TimberHearth.AddComponent<AtmosphereEffect>();
            //atmosphere.sun = solarSystem.Sun.transform;
            //atmosphere.directional = false;
            //atmosphere.cutoffDepth = 0.5f;
            //atmosphere.planetRadius = 90f;
            //atmosphere.profile = defaultAtmosphereProfile;
            //TODO harmonica
            
            
            if (solarSystem.Attlerock != null)
            {
                solarSystem.Attlerock.transform.localScale = Vector3.one * 0.05f;
                solarSystem.Attlerock.transform.rotation = Quaternion.Euler(3.4551f, 6.3135f, 350.4707f);

                Orbiter rockOrbit = solarSystem.Attlerock.AddComponent<Orbiter>();
                
                rockOrbit.orbitParent = solarSystem.TimberHearth.transform; 
                rockOrbit.orbitDistance = 0.8f;                
                rockOrbit.orbitSpeed = 15f;                 
                rockOrbit.spinSpeed = 15f;
                rockOrbit.orbitAxis = new Vector3(0.1f, 1f, 0f).normalized;
                rockOrbit.randomisePos = false;
            }
        }
        
        void SetupBrittleHollow()
        {
            solarSystem.BrittleHollow.transform.localScale = Vector3.one * 0.1f;
    
            // Add the Orbiter component
            Orbiter brittleHollowOrbit = solarSystem.BrittleHollow.AddComponent<Orbiter>();
    
            brittleHollowOrbit.orbitParent = solarSystem.Sun.transform;  
            brittleHollowOrbit.orbitDistance = 7.8f;          
            brittleHollowOrbit.orbitSpeed = 0.8f;            
            brittleHollowOrbit.spinSpeed = 7f;     
            brittleHollowOrbit.orbitAxis = Vector3.up;

            if (solarSystem.BlackHoleMaterial)
            {
                GameObject blackHole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blackHole.name = "BlackHole";

                Renderer r = blackHole.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = solarSystem.BlackHoleMaterial;
                }
                
                blackHole.transform.SetParent(solarSystem.BrittleHollow.transform,false);
                blackHole.transform.localPosition = new Vector3(0, 0, 0);
                blackHole.transform.localScale = new Vector3(2, 2, 2);
            }
    
            if (solarSystem.HollowsLantern != null)
            {
                solarSystem.HollowsLantern.transform.localScale = Vector3.one * 0.05f;
                solarSystem.HollowsLantern.transform.rotation = Quaternion.Euler(3.4551f, 6.3135f, 350.4707f);

                Orbiter lanternOrbit = solarSystem.HollowsLantern.AddComponent<Orbiter>();
        
                lanternOrbit.orbitParent = solarSystem.BrittleHollow.transform; 
                lanternOrbit.orbitDistance = 0.66f;             
                lanternOrbit.orbitSpeed = 20f;              
                lanternOrbit.spinSpeed = 30f;            
                lanternOrbit.orbitAxis = new Vector3(0.1f, 1f, 0f).normalized;
                
                if (solarSystem.HollowsLantern.transform.childCount > 1)
                {
                    Renderer r = solarSystem.HollowsLantern.transform.GetChild(1).GetComponent<Renderer>();
            
                    if (r != null)
                    {
                        // Set color to a lava-like orange/red
                        Color lanternColor = new Color32(217, 103, 15, 255);

                        // Set intensity (lower than White Hole, but still glowing)
                        float intensity = 5.0f;
                        Color finalEmission = lanternColor * intensity;

                        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

                        if (shader != null)
                        {
                            Material mat = new Material(shader);

                            mat.SetColor("_BaseColor", lanternColor);
            
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", finalEmission);

                            r.material = mat;
                        }
                    }
                }
                
                Light lanternLight = solarSystem.HollowsLantern.AddComponent<Light>();
                lanternLight.type = LightType.Point;       
                lanternLight.range = 300f;                 
                lanternLight.intensity = 100f;               
                lanternLight.color = new Color(0.851f, 0.404f, 0.059f, 1f);
                lanternLight.shadows = LightShadows.Soft;  
            }
        }

        void SetupGiantsDeep()
        {
            solarSystem.GiantsDeep.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter giantsDeepOrbit = solarSystem.GiantsDeep.AddComponent<Orbiter>();
            
            giantsDeepOrbit.orbitParent = solarSystem.Sun.transform;  
            giantsDeepOrbit.orbitDistance = 10.6f;           
            giantsDeepOrbit.orbitSpeed = 0.6f;             
            giantsDeepOrbit.spinSpeed = 0.2f;       
            giantsDeepOrbit.orbitAxis = Vector3.up;       
            
            if (solarSystem.OrbitalProbeCannon != null)
            {
                solarSystem.OrbitalProbeCannon.transform.localScale = Vector3.one * 0.001f;
                solarSystem.OrbitalProbeCannon.transform.rotation = Quaternion.Euler(3.4551f, 6.3135f, 350.4707f);

                Orbiter cannonOrbit = solarSystem.OrbitalProbeCannon.AddComponent<Orbiter>();
                
                cannonOrbit.orbitParent = solarSystem.GiantsDeep.transform; 
                cannonOrbit.orbitDistance = 1.8f;                
                cannonOrbit.orbitSpeed = 10f;                 
                cannonOrbit.spinSpeed = 10f;
                cannonOrbit.orbitAxis = Vector3.up;
                cannonOrbit.randomisePos = false;
                
                solarSystem.OrbitalProbeCannon.transform.GetChild(0).gameObject.SetActive(false);


                if (true) //TODO
                {
                    Transform probeCannonBase = solarSystem.OrbitalProbeCannon.transform.GetChild(1);
                    probeCannonBase.GetChild(0).rotation = Quaternion.Euler(354.7151f, 225.9565f, 0);
                    probeCannonBase.GetChild(1).rotation = Quaternion.Euler(-0f, 0f, 69.0547f);
                    
                    probeCannonBase.GetChild(2).rotation = Quaternion.Euler(-0, 0, 312.6561f);
                    probeCannonBase.GetChild(2).localPosition = new Vector3(3.7276f, 0.006f, -0.006f);
                }
            }
        }

        void SetupDarkBramble()
        {
            solarSystem.DarkBramble.transform.localScale = Vector3.one * 0.1f;
            
            // Add the Orbiter component
            Orbiter darkBrambleOrbit = solarSystem.DarkBramble.AddComponent<Orbiter>();
            
            darkBrambleOrbit.orbitParent = solarSystem.Sun.transform;  
            darkBrambleOrbit.orbitDistance = 14.6f;           
            darkBrambleOrbit.orbitSpeed = 0.38f;             
            darkBrambleOrbit.spinSpeed = 0f;       
            darkBrambleOrbit.orbitAxis = Vector3.up;
        }

        void SetupWhiteHoleStation()
        {
            solarSystem.WhiteHoleStation.transform.localScale = Vector3.one * 0.1f;
            
            Vector3 targetPosition = solarSystem.WhiteHole.transform.position;
            targetPosition.z -= 0.3f;
            solarSystem.WhiteHoleStation.transform.position = targetPosition;
            solarSystem.WhiteHoleStation.transform.rotation = Quaternion.Euler(0, 90f, 0);
        }
        
        void SetupInterloper()
        {
            GameObject interloper = solarSystem.Interloper;
            interloper.transform.localScale = Vector3.one * 0.1f;
    
            EllipticalOrbiter interloperOrbiter = interloper.AddComponent<EllipticalOrbiter>();

            Transform ice = interloper.transform.GetChild(0).GetChild(0);
            ice.localScale = new Vector3(1.2f,1f,1f);
            interloperOrbiter.iceTransform = ice;
            
            interloperOrbiter.focusA = solarSystem.Sun.transform;
            interloperOrbiter.focusB = solarSystem.WhiteHole.transform; 
            
            // semiMinorAxis determines how "fat" or "skinny" the ellipse is
            interloperOrbiter.semiMinorAxis = 5.66f; 
            
            interloperOrbiter.orbitSpeed = 11f;
            interloperOrbiter.speedIntensity = 1.1f;
            interloperOrbiter.spinAxis = Vector3.up;
        }
        
        
        void SetupQuantumMoon()
        {
            solarSystem.QuantumMoon.transform.localScale = Vector3.one * 10f;
            solarSystem.QuantumMoon.transform.rotation = Quaternion.identity;

            Orbiter quantumMoonOrbit = solarSystem.QuantumMoon.AddComponent<Orbiter>();
            
            quantumMoonOrbit.orbitDistance = 1.7f;                
            quantumMoonOrbit.orbitSpeed = 2f;                 
            quantumMoonOrbit.spinSpeed = 4f;    
            quantumMoonOrbit.orbitAxis = new Vector3(1, 1, 0);
            
            QuantumOrbiter quantumObject = solarSystem.QuantumMoon.AddComponent<QuantumOrbiter>();

            quantumObject.orbitParents = new Dictionary<Transform, float> 
            { 
                { solarSystem.HourGlassTwins.transform.GetChild(2), 0.59f },
                { solarSystem.HourGlassTwins.transform.GetChild(0), 0.59f },
                { solarSystem.TimberHearth.transform,   0.86f },
                { solarSystem.BrittleHollow.transform,  0.72f },
                { solarSystem.GiantsDeep.transform,     1.7f },
                { solarSystem.DarkBramble.transform,    1.7f }
            };
        }

        void SetupPlayerShip()
        {
            solarSystem.PlayerShip.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        }
    }
    
    
    public struct SolarSystemData
    {
        public GameObject Root;
        
        public GameObject Sun;
        public GameObject SunStation;
        
        public GameObject HourGlassTwins;
        
        public GameObject TimberHearth;
        public GameObject Attlerock;
        
        public GameObject BrittleHollow;
        public GameObject HollowsLantern;
        
        public GameObject GiantsDeep;
        public GameObject OrbitalProbeCannon;
        public GameObject QuantumMoon;
        
        public GameObject DarkBramble;
        
        public GameObject WhiteHole;
        public GameObject WhiteHoleStation;
        
        public GameObject Interloper;

        public GameObject PlayerShip;

        public Material BlackHoleMaterial;
        public Material WhiteHoleMaterial;
    }
}