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
        
    public Vector3 orbitAngles = Vector3.zero; 
        
    public Vector3 orbitAxis = Vector3.up; 
    public float orbitSpeed = 30f;

    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 30f; //Set this equal to orbitSpeed for tidal locking

    public bool randomisePos = true;

    
    private float _currentOrbitAngle = 0;
    private float _currentSpinAngle = 0;

    

    public Orbiter(IntPtr ptr) : base(ptr) {}

    void Start()
    {
        if (randomisePos)
        {
            _currentOrbitAngle = GetRandomAngle();
            _currentSpinAngle = GetRandomAngle(); 
        }
    }

    void FixedUpdate()
    {
        if (orbitParent) 
        {
            float dt = Time.fixedDeltaTime;
            
            _currentOrbitAngle += orbitSpeed * dt;
            _currentSpinAngle += spinSpeed * dt;

            _currentOrbitAngle %= 360f;
            _currentSpinAngle %= 360f;


            Vector3 baseDir = Quaternion.Euler(orbitAngles) * Vector3.forward;
            Quaternion orbitRot = Quaternion.AngleAxis(_currentOrbitAngle, orbitAxis);
            Vector3 localOffsetDirection = orbitRot * baseDir;
            
            Vector3 worldOffset = orbitParent.rotation * localOffsetDirection * orbitDistance;
            transform.position = orbitParent.position + worldOffset;

            
            Quaternion tiltRot = Quaternion.Euler(orbitAngles);
            
            Quaternion spinRot = Quaternion.AngleAxis(_currentSpinAngle, spinAxis);
            
            transform.rotation = orbitParent.rotation * tiltRot * spinRot;
        }
    }
    
    public void SetCurrentAngle(float angle)
    {
        _currentOrbitAngle = angle;
    }
    
    public static float GetRandomAngle()
    {
        return Random.Range(0f, 360f);
    }
}