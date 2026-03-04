using System;
using MelonLoader;
using UnityEngine;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class EllipticalOrbiter : MonoBehaviour
{
    public Transform focusA; // The Sun
    public Transform focusB; // The White hole
    
    public float semiMinorAxis = 5f; 
    public float orbitSpeed = 10f; 
    public float speedIntensity = 1.1f;

    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 50f;

    public Transform iceTransform;
    public float maxIceScale = 1.2f;       
    public float minIceScale = 0.0f;       
    public float meltStartDistance = 8.66659f; 
    public float meltCompleteDistance = 3.2978f; 

    private float _currentAngle;

    public bool randomOrbitAngle = true;

    public EllipticalOrbiter(IntPtr ptr) : base(ptr) {}

    public void Start()
    {
        if (randomOrbitAngle) _currentAngle = Orbiter.GetRandomAngle();
    }

    void FixedUpdate()
    {
        if (focusA && focusB)
        {
            // Geometric Setup
            Vector3 posA = focusA.position;
            Vector3 posB = focusB.position;
            Vector3 center = (posA + posB) * 0.5f;
            Vector3 dirBetweenFoci = posB - posA;
            
            float c = dirBetweenFoci.magnitude / 2f; 
            float b = semiMinorAxis; 
            float a = Mathf.Sqrt((c * c) + (b * b)); 

            // Movement Logic (Kepler-ish approximation)
            float distToSun = Vector3.Distance(transform.position, posA);
            float speedMult = Mathf.Pow(a / Mathf.Max(distToSun, 0.1f), speedIntensity);

            _currentAngle += orbitSpeed * speedMult * Time.fixedDeltaTime;
            float rad = _currentAngle * Mathf.Deg2Rad;

            // 1. Position Calculation (Local 2D Plane)
            Vector3 localPos = new Vector3(-Mathf.Sin(rad) * b, 0, Mathf.Cos(rad) * a);

            // 2. Rotation Logic (The Fix)
            // If dirBetweenFoci is mostly zero (foci overlap), use the focus rotation directly.
            // Otherwise, look at the other focus, but KEEP THE UP VECTOR ALIGNED with the Sun's up.
            Quaternion orbitPlaneRotation;
            if (dirBetweenFoci.sqrMagnitude > 0.001f)
            {
                // CRITICAL CHANGE: Pass focusA.up as the upward reference
                orbitPlaneRotation = Quaternion.LookRotation(dirBetweenFoci, focusA.up);
            }
            else
            {
                orbitPlaneRotation = focusA.rotation;
            }

            Vector3 nextPos = center + (orbitPlaneRotation * localPos);
            
            // 3. Tangent Rotation Logic (Banking)
            Vector3 localVelocity = new Vector3(-Mathf.Cos(rad) * b, 0, -Mathf.Sin(rad) * a);
            Vector3 worldVelocityDirection = orbitPlaneRotation * localVelocity;

            if (worldVelocityDirection.sqrMagnitude > 0.001f)
            {
                // Again, use the focus up vector to ensure we don't flip upside down relative to the system
                transform.rotation = Quaternion.LookRotation(worldVelocityDirection, focusA.up);
            }

            transform.position = nextPos;

            // Ice Melting Logic
            if (iceTransform)
            {
                float meltFactor = Mathf.InverseLerp(meltCompleteDistance, meltStartDistance, distToSun);
                float newScaleX = Mathf.Lerp(minIceScale, maxIceScale, meltFactor);
                Vector3 currentScale = iceTransform.localScale;
                iceTransform.localScale = new Vector3(newScaleX, currentScale.y, currentScale.z);
            }
        }

        // Spin
        transform.Rotate(spinAxis, spinSpeed * Time.fixedDeltaTime, Space.Self);
    }
}