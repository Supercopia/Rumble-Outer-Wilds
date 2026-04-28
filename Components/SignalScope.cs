using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppRUMBLE.Players;
using RumbleModdingAPI.RMAPI;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SignalScope : MonoBehaviour
{
    public Player player;
    public Transform rightHandTransform;
    public Transform beltTransform;
    public GameObject Camera;
    public GameObject Screen;

    private const float hold_distance = 0.115f;
    private const float holdThreshold = 0.9f;
    private const float releaseThreshold = 0.1f;

    private float currentFOV;
    private float zoomIncrement = 4.6f;
    
    private float maxZoom = 120f;
    private float minZoom = 1.5f;

    private float startingZoom;
    
    public float minDetectionAngle = 0.5f;   
    public float detectionAngleBase = 8f;
    public float maxDetectionAngle = 15f;    

    private bool isHolding = false;

    private Vector3 beltLocalPosition = new Vector3(0.1f, 0f, -0.1f);
    private Quaternion beltLocalRotation = Quaternion.Euler(0, 0, 270);

    private Vector3 handLocalPosition = new Vector3(0, 0, 0.1f);
    private Quaternion handLocalRotation = Quaternion.Euler(0, 90, 0);
    
    private Vector3 zoomInButtonLocalPosition = new Vector3(-0.01f, 0.05f, -0.015f);
    private Quaternion zoomInButtonLocalRotation = Quaternion.Euler(90f, 180f, 0);
    
    private Vector3 zoomOutButtonLocalPosition = new Vector3(0.01f, 0.05f, -0.015f);
    private Quaternion zoomOutButtonLocalRotation = Quaternion.Euler(90f, 180f, 0);
    
    private GameObject ZoomInButton;
    private GameObject ZoomOutButton;

    private bool hasSetUp;
    
    Dictionary<GameObject,MusicEmitter> musicEmitters = new Dictionary<GameObject,MusicEmitter>();

    public SignalScope(IntPtr ptr) : base(ptr)
    {
    }

    void Start()
    {
        gameObject.name = "SignalScope";
       
        Camera = gameObject.transform.GetChild(0).gameObject;
        startingZoom = Camera.GetComponent<Camera>().fieldOfView;
        currentFOV = Camera.GetComponent<Camera>().fieldOfView;
        Screen = gameObject.transform.GetChild(35).gameObject;

        MelonCoroutines.Start(FindPlayerAndSetup());

        //Actions.onMapInitialized += SceneLoaded; TODO make signalscope carry over, right not it gets re loaded every scene
        
        SetupButtons();
        
        CacheMusicEmitters();
    }

    void CacheMusicEmitters()
    {
        Type type = typeof(SolarSystemData);
        
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(GameObject))
            {
                GameObject go = field.GetValue(Main.solarSystem) as GameObject;

                if (go != null)
                {
                    if (go.TryGetComponent<MusicEmitter>(out MusicEmitter emitter))
                    {
                        musicEmitters.Add(go, emitter);
                    }
                }
            }
        }
    }

    void SetupButtons()
    {
        Action zoomInAction = () =>
        {
            currentFOV = Camera.GetComponent<Camera>().fieldOfView;
            Camera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(currentFOV - zoomIncrement, minZoom, maxZoom);
        };

        Action zoomOutAction = () =>
        {
            currentFOV = Camera.GetComponent<Camera>().fieldOfView;
            Camera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(currentFOV + zoomIncrement, minZoom, maxZoom);
        };
        
        ZoomInButton = Create.NewButton(zoomInAction);
        ZoomInButton.name = "ZoomInButton";
        
        ZoomOutButton = Create.NewButton(zoomOutAction);
        ZoomOutButton.name = "ZoomOutButton";
        
        ZoomInButton.transform.SetParent(transform,false);
        ZoomOutButton.transform.SetParent(transform,false);
        
        ZoomInButton.transform.localPosition = zoomInButtonLocalPosition;
        ZoomOutButton.transform.localPosition = zoomOutButtonLocalPosition;

        ZoomInButton.transform.localRotation = zoomInButtonLocalRotation;
        ZoomOutButton.transform.localRotation = zoomOutButtonLocalRotation;
        
        ZoomInButton.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        ZoomOutButton.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
    }

    void SceneLoaded(string sceneName)
    {
        MelonCoroutines.Start(FindPlayerAndSetup());
    }

    void FixedUpdate()
    {
        if (!hasSetUp) return;
        
        float rightTrigger = Calls.ControllerMap.RightController.GetTrigger();

        if (!isHolding && rightTrigger > holdThreshold && IsHandCloseEnough(rightHandTransform.position))
        {
            Grab();
        }
        else if (isHolding && rightTrigger <= releaseThreshold)
        {
            ReleaseToBelt();
        }

        HandleMusicChange();
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

        player = Calls.Players.GetLocalPlayer();

        beltTransform = player.Controller.PlayerVisuals.transform.GetChild(1).GetChild(0).GetChild(3);
        rightHandTransform = player.Controller.gameObject.transform.GetChild(2).GetChild(2);
    
        ReleaseToBelt();
        MelonLogger.Msg("SignalScope: Player found, scope attached to belt.");
        hasSetUp = true;
    }

    private void Grab()
    {
        transform.SetParent(rightHandTransform);
        transform.localPosition = handLocalPosition;
        transform.localRotation = handLocalRotation;

        isHolding = true;
        EnableScreen(true);
    }

    private void ReleaseToBelt()
    {
        transform.SetParent(beltTransform);
        transform.localPosition = beltLocalPosition;
        transform.localRotation = beltLocalRotation;

        isHolding = false;
        EnableScreen(false);
    }

    void EnableScreen(bool enable)
    {
        Camera.SetActive(enable);
        Screen.SetActive(enable);
    }

    bool IsHandCloseEnough(Vector3 handPos)
    {
        float distance = Vector3.Distance(gameObject.transform.position, handPos);
        return distance <= hold_distance;
    }

    private void HandleMusicChange()
    {
        if (!isHolding)
        {
            foreach (var emitter in musicEmitters.Values)
            {
                emitter.SetVolume(0f);
            }
            return;
        }

        float currentDetectionAngle;

        if (currentFOV <= startingZoom)
        {
            float t = Mathf.InverseLerp(minZoom, startingZoom, currentFOV);
            currentDetectionAngle = Mathf.Lerp(minDetectionAngle, detectionAngleBase, t);
        }
        else
        {
            float t = Mathf.InverseLerp(startingZoom, maxZoom, currentFOV);
            currentDetectionAngle = Mathf.Lerp(detectionAngleBase, maxDetectionAngle, t);
        }

        foreach (KeyValuePair<GameObject, MusicEmitter> entry in musicEmitters)
        {
            GameObject body = entry.Key;
            MusicEmitter emitter = entry.Value;
            
            if (!body.activeSelf)
            {
                emitter.SetVolume(0);
                continue;
            }

            float strength = GetSignalStrengthForTarget(body, currentDetectionAngle);
            emitter.SetVolume(strength);
        }
    }
    
    private float GetSignalStrengthForTarget(GameObject target, float currentDetectionAngle)
    {
        Vector3 scopePos = Camera.transform.position;
        Vector3 scopeForward = Camera.transform.forward;
        Vector3 dirToTarget = (target.transform.position - scopePos).normalized;

        float angleToTarget = Vector3.Angle(scopeForward, dirToTarget);

        if (angleToTarget > currentDetectionAngle)
        {
            return 0f;
        }

        return 1f - (angleToTarget / currentDetectionAngle);
    }
}