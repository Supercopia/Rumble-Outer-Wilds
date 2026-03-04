using System;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;
using Random = UnityEngine.Random;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class Orbiter : MonoBehaviour
{
    public Transform orbitParent;
    public float orbitDistance = 5f;
        
    // Defines the "Starting point" or Tilt of the orbit
    public Vector3 orbitAngles = Vector3.zero; 
        
    public Vector3 orbitAxis = Vector3.up; 
    public float orbitSpeed = 30f;

    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 30f; // Set this equal to orbitSpeed for tidal locking

    public bool randomisePos = true;

    // We now track BOTH angles
    private float _currentOrbitAngle = 0;
    private float _currentSpinAngle = 0;

    public Orbiter(IntPtr ptr) : base(ptr) {}

    void Start()
    {
        if (randomisePos)
        {
            _currentOrbitAngle = GetRandomAngle();
            // We also randomise spin so it doesn't always start facing the same way
            _currentSpinAngle = GetRandomAngle(); 
        }
    }

    void FixedUpdate()
    {
        if (orbitParent) 
        {
            float dt = Time.fixedDeltaTime;

            // --- 1. Update Angles ---
            _currentOrbitAngle += orbitSpeed * dt;
            _currentSpinAngle += spinSpeed * dt;

            // Keep angles clean
            _currentOrbitAngle %= 360f;
            _currentSpinAngle %= 360f;


            // --- 2. Calculate Position ---
            // Base direction (Forward) -> Tilted by orbitAngles -> Rotated around orbitAxis
            Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
            Quaternion orbitRot = Quaternion.AngleAxis(_currentOrbitAngle, orbitAxis);
            Vector3 localOffsetDirection = orbitRot * baseDir;
            
            // Apply Parent Rotation to position
            Vector3 worldOffset = orbitParent.rotation * localOffsetDirection * orbitDistance;
            transform.position = orbitParent.position + worldOffset;


            // --- 3. Calculate Rotation (THE FIX) ---
            // A: The Tilt of the orbit plane
            Quaternion tiltRot = Quaternion.Euler(orbitAngles);
            
            // B: The Spin of the planet (around its own axis)
            Quaternion spinRot = Quaternion.AngleAxis(_currentSpinAngle, spinAxis);

            // C: Combine: ParentRot * OrbitTilt * Spin
            // This ensures the axis of rotation tilts WITH the solar system.
            transform.rotation = orbitParent.rotation * tiltRot * spinRot;
        }
    }
    
    public void SetCurrentAngle(float angle)
    {
        _currentOrbitAngle = angle;
    }
    
    public static float GetRandomAngle()
    {
        return Random.Range(0f, 360f); // Random.RandomRange is deprecated in newer Unity
    }
}