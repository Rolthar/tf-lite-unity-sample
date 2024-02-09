using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    public bool isFirstGen = true;
    public float totalDifference = 0;

    public Queue<float> lastDifferences = new();
    public float averageDifferenceSinceCull = 0;

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

        if (lastDifferences.Count == FilterTwin.Instance.CullFrameTrigger)
        {
            lastDifferences.Enqueue(totalDifference);
            lastDifferences.Dequeue();
            averageDifferenceSinceCull = 0;
            foreach (var item in lastDifferences)
            {
                averageDifferenceSinceCull += item;
            }
            averageDifferenceSinceCull /= FilterTwin.Instance.CullFrameTrigger;
        }
        else
            lastDifferences.Enqueue(totalDifference);
        CompareWithCamera();
    }

    public void CompareWithCamera()
    {
        float totalCurrentHitDifference = 0f;
        float totalDeltaDifference = 0f;
        int validCurrentHitComparisons = 0;
        int validDeltaComparisons = 0;

        CustomRaycastScript cameraRaycastScript = CameraScript.Instance.raycastScript;

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
                if (particleDeltaValue != Mathf.Infinity && cameraDeltaValue != Mathf.Infinity)
                {
                    totalDeltaDifference += Mathf.Abs(particleDeltaValue - cameraDeltaValue);
                    validDeltaComparisons++;
                }
            }
        }

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
        if (totalDifference < 0.1 && Age > 1)
            ProvedItself = true;
    }
}
