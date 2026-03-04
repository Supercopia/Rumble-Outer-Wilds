using System;
using MelonLoader;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class HourGlassTwins : MonoBehaviour
{
    // --- Configuration ---
    public float transferDurationRevs = 3.4f; // How many orbits the transfer takes
    public float waitDurationRevs = 0.4f;     // How many orbits to wait between transfers
    public bool randomSandStage = true;  //Start in a random sand stage
    
    
    private readonly Vector3 ashEmptyScale = new Vector3(66, 66, 66);
    private readonly Vector3 ashFullScale = new Vector3(330, 330, 330);

    private readonly Vector3 emberEmptyScale = new Vector3(30, 30, 30);
    private readonly Vector3 emberFullScale = new Vector3(320, 320, 320);

    private readonly Vector3 activeScale = new Vector3(1, 1, 1);
    private readonly Vector3 funnelInactiveScale = Vector3.zero;
    
    private Quaternion defaultFunnelRotation;

    // --- References ---
    private GameObject sandFunnel;
    private GameObject ashTwinSand;
    private GameObject emberTwinSand;
    private Orbiter twinsOrbiter;

    private enum State
    {
        WaitingForTransfer,
        Transferring
    }

    private State currentState = State.WaitingForTransfer;
    private bool flowingToEmber = true;
    private float currentRevsCounter = 0f;

    public HourGlassTwins(IntPtr ptr) : base(ptr) { }

    private void Start()
    {
        emberTwinSand = transform.GetChild(0).GetChild(1).gameObject;
        sandFunnel = transform.GetChild(1).gameObject;
        ashTwinSand = transform.GetChild(2).GetChild(0).gameObject;
        
        defaultFunnelRotation = sandFunnel.transform.localRotation;

        twinsOrbiter = GetComponent<Orbiter>();

        float sandProgress = 0f; // 0f progress = start of Ash->Ember flows (Ash Full)

        if (randomSandStage) sandProgress = UnityEngine.Random.Range( 0f, 1f );
        
        SetSandScales(sandProgress); 
        sandFunnel.transform.localScale = funnelInactiveScale;
        
        currentState = State.WaitingForTransfer;
        flowingToEmber = true;
    }

    private void FixedUpdate()
    {

        // Calculate how much "revolution" happened this frame
        float deltaRevs = Mathf.Abs(twinsOrbiter.orbitSpeed * Time.fixedDeltaTime) / 360f;
        currentRevsCounter += deltaRevs;

        if (currentState == State.WaitingForTransfer)
        {
            sandFunnel.transform.localScale = funnelInactiveScale;

            if (currentRevsCounter >= waitDurationRevs)
            {
                currentRevsCounter = 0f;
                currentState = State.Transferring;
                
                UpdateFunnelOrientation();
            }
        }
        else if (currentState == State.Transferring)
        {
            float progress = Mathf.Clamp01(currentRevsCounter / transferDurationRevs);

            // Animate Sand
            SetSandScales(progress);
            
            sandFunnel.transform.localScale = new Vector3(activeScale.x, activeScale.y, sandFunnel.transform.localScale.z);

            if (progress >= 1.0f)
            {
                // Transfer finished
                currentRevsCounter = 0f;
                currentState = State.WaitingForTransfer;
                flowingToEmber = !flowingToEmber; // Flip direction for next time
            }
        }
    }

    private void UpdateFunnelOrientation()
    {
        if (flowingToEmber)
        {
            sandFunnel.transform.localRotation = defaultFunnelRotation;
            Vector3 scale = sandFunnel.transform.localScale;
            scale.z = 1;
            sandFunnel.transform.localScale = scale;
        }
        else
        {
            sandFunnel.transform.localRotation = defaultFunnelRotation * Quaternion.Euler(180f, 0f, 0f);
            Vector3 scale = sandFunnel.transform.localScale;
            scale.z = -1;
            sandFunnel.transform.localScale = scale;
        }
    }

    private void SetSandScales(float progress)
    {
        float emberFill = flowingToEmber ? progress : (1f - progress);
        
        float ashFill = 1f - emberFill;

        emberTwinSand.transform.localScale = Vector3.Lerp(emberEmptyScale, emberFullScale, emberFill);
        ashTwinSand.transform.localScale = Vector3.Lerp(ashEmptyScale, ashFullScale, ashFill);
    }
}