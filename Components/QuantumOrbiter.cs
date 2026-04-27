using System;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class QuantumOrbiter : MonoBehaviour
{
    private bool hasChangedPositions;
    private Renderer _renderer;
    private Orbiter _orbiter;
    
    public Dictionary<Transform,float> orbitParents = new ();
    
    private List<OrbitTarget> _cachedOrbitTargets = new ();
    
    public QuantumOrbiter(IntPtr ptr) : base(ptr) {}
    
    private struct OrbitTarget
    {
        public Transform ParentTransform;
        public Renderer ParentRenderer;
        public float OrbitDistance;
    }

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        _orbiter = GetComponent<Orbiter>();
            
        if (orbitParents != null)
        {
            foreach (var parent in orbitParents)
            {
                if (parent.Key == null) continue;

                var rend = parent.Key.GetComponentInChildren<Renderer>();
                    
                _cachedOrbitTargets.Add(new OrbitTarget 
                { 
                    ParentTransform = parent.Key, 
                    ParentRenderer = rend,
                    OrbitDistance = parent.Value,
                });
            }
        }

        if (_cachedOrbitTargets.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, _cachedOrbitTargets.Count);
            _orbiter.orbitParent = _cachedOrbitTargets[randomIndex].ParentTransform;
        }
    }

    void FixedUpdate()
    {
        bool isBeingLookedAt = _renderer.isVisible;
        
        if (hasChangedPositions)
        {
            if (isBeingLookedAt)
            {
                hasChangedPositions = false;
            }
            return;
        }
        if (isBeingLookedAt)
        {
            return;
        }

        if (_cachedOrbitTargets.Count > 0)
        {
            int count = _cachedOrbitTargets.Count;
            int startIndex = UnityEngine.Random.Range(0, count);
            OrbitTarget chosenTarget = default;
            bool foundTarget = false;

            for (int i = 0; i < count; i++)
            {
                int index = (startIndex + i) % count;
                var target = _cachedOrbitTargets[index];
                
                if (!target.ParentRenderer.isVisible)
                {
                    
                    chosenTarget = target;
                    foundTarget = true;
                    break;
                }
            }

            if (foundTarget)
            {
                _orbiter.orbitParent = chosenTarget.ParentTransform;
                _orbiter.SetCurrentAngle(Orbiter.GetRandomAngle());
                _orbiter.orbitDistance = chosenTarget.OrbitDistance;
                hasChangedPositions = true;
            }
        }
    }
    
}