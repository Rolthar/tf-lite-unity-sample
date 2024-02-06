using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    public bool isFirstGen = true;
    public float totalDifference = 0;

    public float Age = 0f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    public Area nearestArea;

    public Conecast conecast;

    public bool ProvedItself = false;

    public int matchingSemanticsCount = 0;

    public CustomRaycastScript raycastScript;

    public string uuid;
    void Awake()
    {
        lastPosition = transform.localPosition;
        lastRotation = transform.localRotation;
        uuid = System.Guid.NewGuid().ToString();
        transform.parent.name = uuid;
    }

    void Update()
    {
        if (FilterTwin.Instance.cullParticles)
            Age += Time.deltaTime;

        if (CameraScript.Instance != null)
        {
            Vector3 positionDelta = CameraScript.Instance.PositionDelta;
            Quaternion rotationDelta = CameraScript.Instance.RotationDelta;
            Vector3 transformedPositionDelta = rotationDelta * positionDelta;
            transform.localPosition += transformedPositionDelta;
            transform.localRotation = rotationDelta * transform.localRotation;
        }


        // RaycastAndDraw(transform.forward, "forward");

        // PerformRaycast(22.5f, transform.up, "up22.5");
        // PerformRaycast(-22.5f, transform.up, "up-22.5");
        // PerformRaycast(22.5f, transform.right, "right22.5");
        // PerformRaycast(-22.5f, transform.right, "right-22.5");
        // PerformRaycast(-45, transform.up, "up-45");
        // PerformRaycast(45, transform.up, "up-45");
        // PerformRaycast(45, transform.right, "right-45");


        // totalHitDistance = 0;
        // totaldelta = 0;
        // foreach (var kvp in currentHits)
        // {
        //     string key = kvp.Key;
        //     float currentHit = kvp.Value;
        //     float lastHit = lastHits.ContainsKey(key) ? lastHits[key] ?? 0 : 0;
        //     deltas[key] = currentHit - lastHit;
        //     lastHits[key] = currentHit;
        //     totalHitDistance += currentHit;
        //     totaldelta += currentHit - lastHit;
        // }

        // float totalDiff = 0;
        // foreach (var cameraHit in CameraScript.Instance.lastHits)
        // {
        //     string key = cameraHit.Key;
        //     float? cameraHitValue = cameraHit.Value;
        //     float? particleHitValue = lastHits.ContainsKey(key) ? lastHits[key] : null;
        //     totalDiff += Mathf.Abs(particleHitValue.Value - cameraHitValue.Value);
        // }

        // foreach (var delta in CameraScript.Instance.deltas)
        // {
        //     string key = delta.Key;
        //     float? deltaValue = delta.Value;
        //     float? particleDeltaValue = deltas.ContainsKey(key) ? deltas[key] : null;

        //     // if (particleHitValue != 10)
        //     totalDiff += Mathf.Abs(particleDeltaValue.Value - deltaValue.Value);

        // }
        //  totalDifference = (totalHitDistance == 80 && totaldelta == 0) ? 100 : totalDiff;
        // if (totalDiff <= 1.5 && totalDiff > 0 && Age > 0.3f)
        // ProvedItself = true;

        CompareWithCamera();
    }

    // void PerformRaycast(float angle, Vector3 axis, string axisName)
    // {
    //     Vector3 direction = Quaternion.AngleAxis(angle, axis) * transform.forward;
    //     string key = axisName + angle.ToString();
    //     RaycastAndDraw(direction, key);
    // }

    // void RaycastAndDraw(Vector3 direction, string key)
    // {
    //     Ray ray = new Ray(transform.position, direction);
    //     RaycastHit hit;
    //     if (Physics.Raycast(ray, out hit, 10f, 1 << 6))
    //     {
    //         if (!isFirstGen)
    //             Debug.DrawLine(transform.position, hit.point, Age > 1 ? Color.yellow : Color.green);
    //         currentHits[key] = hit.distance;
    //     }
    //     else
    //     {
    //         if (!isFirstGen)
    //             Debug.DrawRay(transform.position, direction * 10f, Color.red);
    //         currentHits[key] = 10f;
    //     }
    // }

    void OnRenderObject()
    {
        //  RuntimeGizmos.Cone(transform.position, transform.rotation, 6f, 60f, Color.Lerp(Color.green, Color.red, totalDifference), true);
    }

    void OnDrawGizmos()
    {
        //  RuntimeGizmos.Cone(transform.position, transform.rotation, 6f, 60f, Color.Lerp(Color.green, Color.red, totalDifference), true);
    }


    public void CompareWithCamera()
    {
        float totalCurrentHitDifference = 0f;
        float totalDeltaDifference = 0f;
        int validCurrentHitComparisons = 0;
        int validDeltaComparisons = 0;

        // Access the CameraScript's CustomRaycastScript for comparison
        CustomRaycastScript cameraRaycastScript = CameraScript.Instance.raycastScript;

        // Calculate differences in current hits
        foreach (var particleHit in raycastScript.currentHits)
        {
            string key = particleHit.Key;
            float particleDistance = particleHit.Value;
            if (cameraRaycastScript.currentHits.TryGetValue(key, out float cameraDistance))
            {
                // Skip comparison if either value is Mathf.Infinity
                if (particleDistance != Mathf.Infinity && cameraDistance != Mathf.Infinity)
                {
                    totalCurrentHitDifference += Mathf.Abs(particleDistance - cameraDistance);
                    validCurrentHitComparisons++;
                }
            }
        }

        // Calculate differences in deltas
        foreach (var particleDelta in raycastScript.deltas)
        {
            string key = particleDelta.Key;
            float particleDeltaValue = particleDelta.Value;
            if (cameraRaycastScript.deltas.TryGetValue(key, out float cameraDeltaValue))
            {
                // Skip comparison if either delta is Mathf.Infinity, which signifies no previous hit
                if (particleDeltaValue != Mathf.Infinity && cameraDeltaValue != Mathf.Infinity)
                {
                    totalDeltaDifference += Mathf.Abs(particleDeltaValue - cameraDeltaValue);
                    validDeltaComparisons++;
                }
            }
        }

        // Calculate averages, being mindful of division by zero
        float averageCurrentHitDifference = validCurrentHitComparisons > 0 ? totalCurrentHitDifference / validCurrentHitComparisons : 0;
        // Debug.Log($"Total Current Hit Difference: {totalCurrentHitDifference}, Based on {validCurrentHitComparisons} comparisons");
        // Debug.Log($"Average Current Hit Difference: {averageCurrentHitDifference}");

        // After calculating totalDeltaDifference
        if (!float.IsNaN(totalDeltaDifference) && !float.IsInfinity(totalDeltaDifference))
        {
            if (validDeltaComparisons > 0)
            {
                float averageDeltaDifference = totalDeltaDifference / validDeltaComparisons;
                // Debug.Log($"Total Delta Difference: {totalDeltaDifference}, Based on {validDeltaComparisons} comparisons");
                // Debug.Log($"Average Delta Difference: {averageDeltaDifference}");
            }
        }

        totalDifference = averageCurrentHitDifference;
        transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, totalDifference);
        if (totalDifference < 0.2 && Age > 5)
            ProvedItself = true;
    }
}
