using System;
using MelonLoader;
using UnityEngine;
using Il2CppInterop.Runtime;

namespace OuterWildsRumble.Components;

[RegisterTypeInIl2Cpp]
public class QuantumObject : MonoBehaviour
{
    private bool hasChangedPositions;
    private Renderer _renderer;

    public List<Vector3> teleportPositions = new();


    public QuantumObject(IntPtr ptr) : base(ptr)
    {
    }

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
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


        if (teleportPositions.Count > 0)
        {
            int attempts = 0;
            bool foundSafeSpot = false;
            Vector3 potentialPos = Vector3.zero;

            while (attempts < 5 && !foundSafeSpot)
            {
                int index = UnityEngine.Random.Range(0, teleportPositions.Count);
                potentialPos = teleportPositions[index];

                if (!IsPositionObserved(potentialPos))
                {
                    foundSafeSpot = true;
                }

                attempts++;
            }

            if (foundSafeSpot)
            {
                transform.position = potentialPos;
                hasChangedPositions = true;
            }
        }
    }

    private bool IsPositionObserved(Vector3 targetPosition)
    {
        Bounds futureBounds = new Bounds(targetPosition, _renderer.bounds.size);
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Main.playerCam);

        if (!GeometryUtility.TestPlanesAABB(planes, futureBounds))
        {
            return false;
        }

        Vector3 camPos = Main.playerCam.transform.position;
        Vector3 direction = targetPosition - camPos;
        float dist = direction.magnitude;

        int layerMask = ~0;

        if (Physics.Raycast(camPos, direction, out RaycastHit hit, dist, layerMask))
        {
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }
}