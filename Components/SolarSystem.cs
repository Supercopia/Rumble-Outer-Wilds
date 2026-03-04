using System;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class SolarSystem : MonoBehaviour
{
    public SolarSystem(IntPtr ptr) : base(ptr) {}

    public void Scale(float scale)
    {
        GameObject Root = transform.gameObject;
        // Scale the root
        Root.transform.localScale *= scale;
            
        // Scale orbiters
        foreach (var orb in Root.GetComponentsInChildren<Orbiter>())
        {
            orb.orbitDistance *= scale;
        }
            
        // Scale elliptical orbiters
        foreach (var ell in Root.GetComponentsInChildren<EllipticalOrbiter>())
        {
            ell.semiMinorAxis *= scale;
            ell.meltStartDistance *= scale;
            ell.meltCompleteDistance *= scale;
        }
    }

    public void Rotate()
    {
        //TODO
    }
}