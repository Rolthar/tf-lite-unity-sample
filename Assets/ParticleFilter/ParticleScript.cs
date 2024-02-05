using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleScript : MonoBehaviour
{
    public bool isFirstGen = true;
    public float totalDifference = 0;
    private Dictionary<string, float> currentHits = new Dictionary<string, float>();
    public Dictionary<string, float?> lastHits = new Dictionary<string, float?>();
    public Dictionary<string, float?> deltas = new Dictionary<string, float?>();

    public float totalHitDistance = 0;
    public float totaldelta = 0;

    public float Age = 0f;

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    public Area nearestArea;

    public Conecast conecast;

    public bool ProvedItself = false;

    public bool matchingSemantics = false;

    void Awake()
    {
        lastPosition = transform.localPosition;
        lastRotation = transform.localRotation;
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


        RaycastAndDraw(transform.forward, "forward");

        PerformRaycast(22.5f, transform.up, "up22.5");
        PerformRaycast(-22.5f, transform.up, "up-22.5");
        PerformRaycast(22.5f, transform.right, "right22.5");
        PerformRaycast(-22.5f, transform.right, "right-22.5");
        PerformRaycast(-45, transform.up, "up-45");
        PerformRaycast(45, transform.up, "up-45");
        PerformRaycast(45, transform.right, "right-45");


        totalHitDistance = 0;
        totaldelta = 0;
        foreach (var kvp in currentHits)
        {
            string key = kvp.Key;
            float currentHit = kvp.Value;
            float lastHit = lastHits.ContainsKey(key) ? lastHits[key] ?? 0 : 0;
            deltas[key] = currentHit - lastHit;
            lastHits[key] = currentHit;
            totalHitDistance += currentHit;
            totaldelta += currentHit - lastHit;
        }

        float totalDiff = 0;
        foreach (var cameraHit in CameraScript.Instance.lastHits)
        {
            string key = cameraHit.Key;
            float? cameraHitValue = cameraHit.Value;
            float? particleHitValue = lastHits.ContainsKey(key) ? lastHits[key] : null;
            totalDiff += Mathf.Abs(particleHitValue.Value - cameraHitValue.Value);
        }

        foreach (var delta in CameraScript.Instance.deltas)
        {
            string key = delta.Key;
            float? deltaValue = delta.Value;
            float? particleDeltaValue = deltas.ContainsKey(key) ? deltas[key] : null;

            // if (particleHitValue != 10)
            totalDiff += Mathf.Abs(particleDeltaValue.Value - deltaValue.Value);

        }
        totalDifference = (totalHitDistance == 80 && totaldelta == 0) ? 100 : totalDiff;
        // if (totalDiff <= 1.5 && totalDiff > 0 && Age > 0.3f)
        // ProvedItself = true;
    }

    void PerformRaycast(float angle, Vector3 axis, string axisName)
    {
        Vector3 direction = Quaternion.AngleAxis(angle, axis) * transform.forward;
        string key = axisName + angle.ToString();
        RaycastAndDraw(direction, key);
    }

    void RaycastAndDraw(Vector3 direction, string key)
    {
        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 10f, 1 << 6))
        {
            if (!isFirstGen)
                Debug.DrawLine(transform.position, hit.point, Age > 1 ? Color.yellow : Color.green);
            currentHits[key] = hit.distance;
        }
        else
        {
            if (!isFirstGen)
                Debug.DrawRay(transform.position, direction * 10f, Color.red);
            currentHits[key] = 10f;
        }
    }

    void OnRenderObject()
    {
        if (!isFirstGen)
        {
            RuntimeGizmos.Cone(transform.position, transform.rotation, 0.5f, 45f, Color.white);
            Vector3 forwardEndPos = transform.position + transform.forward * 1f;
            RuntimeGizmos.Line(transform.position, forwardEndPos, Color.white);

            Quaternion[] rotations = new Quaternion[]
            {
            Quaternion.AngleAxis(-45, transform.up),
            Quaternion.AngleAxis(-22.5f, transform.up),
            Quaternion.AngleAxis(22.5f, transform.up),
            Quaternion.AngleAxis(45, transform.up),
            Quaternion.AngleAxis(-22.5f, transform.right),
            Quaternion.AngleAxis(22.5f, transform.right),
            Quaternion.AngleAxis(45, transform.right)
            };

            foreach (var rotation in rotations)
            {
                Vector3 direction = rotation * transform.forward;
                Vector3 endPos = transform.position + direction * 1f;
                RuntimeGizmos.Line(transform.position, endPos, Color.yellow);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!isFirstGen)
        {
            RuntimeGizmos.Cone(transform.position, transform.rotation, 0.5f, 45f, Color.white);
            Vector3 forwardEndPos = transform.position + transform.forward * 1f;
            RuntimeGizmos.Line(transform.position, forwardEndPos, Color.white);

            Quaternion[] rotations = new Quaternion[]
            {
            Quaternion.AngleAxis(-45, transform.up),
            Quaternion.AngleAxis(-22.5f, transform.up),
            Quaternion.AngleAxis(22.5f, transform.up),
            Quaternion.AngleAxis(45, transform.up),
            Quaternion.AngleAxis(-22.5f, transform.right),
            Quaternion.AngleAxis(22.5f, transform.right),
            Quaternion.AngleAxis(45, transform.right)
            };

            foreach (var rotation in rotations)
            {
                Vector3 direction = rotation * transform.forward;
                Vector3 endPos = transform.position + direction * 1f;
                RuntimeGizmos.Line(transform.position, endPos, Color.yellow);
            }
        }

    }
}
