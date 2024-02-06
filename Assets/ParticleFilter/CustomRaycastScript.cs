using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomRaycastScript : MonoBehaviour
{
    public float horizontalRad = 30f;
    public int x = 5;
    public float verticalRad = 60f;
    public int y = 3;
    public LayerMask hitLayers; // Define which layers the raycast should hit

    public Dictionary<string, float> currentHits = new Dictionary<string, float>();
    public Dictionary<string, float> lastHits = new Dictionary<string, float>();
    public Dictionary<string, float> deltas = new Dictionary<string, float>();

    public float DebugAlphaRay = 0.1f;

    void Update()
    {
        PerformRaycasts();
        CalculateDeltas();
        UpdateLastHits();
    }

    void PerformRaycasts()
    {
        currentHits.Clear(); // Clear current hits before each update

        float horizontalStep = horizontalRad * 2 / (x - 1);
        float verticalStep = verticalRad / (y - 1);

        for (int vertStep = 0; vertStep < y; vertStep++)
        {
            float currentVerticalAngle = vertStep * verticalStep; // Positive for downward angles

            for (int horizStep = 0; horizStep < x; horizStep++)
            {
                float currentHorizontalAngle = -horizontalRad + horizStep * horizontalStep;
                Vector3 direction = CalculateDirection(currentHorizontalAngle, currentVerticalAngle);
                string key = $"H{currentHorizontalAngle}V{currentVerticalAngle}";

                Ray ray = new Ray(transform.position, direction);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 3f, hitLayers))
                {
                    currentHits[key] = hit.distance;
                    Debug.DrawLine(ray.origin, hit.point, new Color(0, 1, 0, DebugAlphaRay)); // Draw line to show the raycast
                }
                else
                {
                    currentHits[key] = Mathf.Infinity;
                    Debug.DrawRay(ray.origin, direction * 3f, new Color(1, 0, 0, DebugAlphaRay)); // Draw ray in red if nothing is hit
                }
            }
        }
    }

    void CalculateDeltas()
    {
        deltas.Clear(); // Clear deltas before calculating new ones

        foreach (var currentHit in currentHits)
        {
            string key = currentHit.Key;
            float distance = currentHit.Value;
            if (lastHits.TryGetValue(key, out float lastDistance))
            {
                deltas[key] = distance - lastDistance;
            }
            else
            {
                deltas[key] = Mathf.Infinity; // If there's no last hit, set delta to Infinity
            }
        }
    }

    void UpdateLastHits()
    {
        foreach (var currentHit in currentHits)
        {
            lastHits[currentHit.Key] = currentHit.Value;
        }
    }

    Vector3 CalculateDirection(float horizontalAngle, float verticalAngle)
    {
        Vector3 horizontalDirection = Quaternion.Euler(0, horizontalAngle, 0) * transform.forward;
        Vector3 finalDirection = Quaternion.AngleAxis(verticalAngle, transform.right) * horizontalDirection;
        return finalDirection;
    }

    void OnDrawGizmos()
    {
        // RuntimeGizmos.Cone(transform.position, transform.rotation, 0.5f, 45f, Color.green);
    }

    void OnRenderObject()
    {
        // RuntimeGizmos.Cone(transform.position, transform.rotation, 0.5f, 45f, Color.green);
    }
}
