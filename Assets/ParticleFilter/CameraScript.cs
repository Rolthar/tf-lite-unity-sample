using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public static CameraScript Instance;
    private Dictionary<string, float> currentHits = new Dictionary<string, float>();
    public Dictionary<string, float?> lastHits = new Dictionary<string, float?>();
    public Dictionary<string, float?> deltas = new Dictionary<string, float?>();

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    public Vector3 PositionDelta { get; private set; }
    public Quaternion RotationDelta { get; private set; }

    public Conecast SemanticCast;

    void Awake()
    {
        Instance = this;
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Start()
    {
#if UNITY_EDITOR
        SemanticCast.OnSemanticListUpdate.AddListener(UpdateFilterTwinWithAreas);
#endif
    }

    public void UpdateFilterTwinWithAreas(List<SemanticItem> items)
    {
        FilterTwin.Instance.UpdatePotentialAreas(items);
    }

    void Update()
    {
        PositionDelta = transform.position - lastPosition;
        RotationDelta = transform.rotation * Quaternion.Inverse(lastRotation);

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        RaycastAndDraw(transform.forward, "forward");

        PerformRaycast(22.5f, transform.up, "up22.5");
        PerformRaycast(-22.5f, transform.up, "up-22.5");
        PerformRaycast(22.5f, transform.right, "right22.5");
        PerformRaycast(-22.5f, transform.right, "right-22.5");
        PerformRaycast(-45, transform.up, "up-45");
        PerformRaycast(45, transform.up, "up-45");
        PerformRaycast(45, transform.right, "right-45");

        foreach (var kvp in currentHits)
        {
            string key = kvp.Key;
            float currentHit = kvp.Value;
            float lastHit = lastHits.ContainsKey(key) ? lastHits[key] ?? 0 : 0;
            deltas[key] = currentHit - lastHit;
            lastHits[key] = currentHit;
        }

        // if (PositionDelta != Vector3.zero)
        //     FilterTwin.Instance.probabilityThreshold = 5f;
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
        if (Physics.Raycast(ray, out hit, 10f, -1))
        {
            Debug.DrawLine(transform.position, hit.point, Color.green);
            currentHits[key] = hit.distance;
        }
        else
        {
            Debug.DrawRay(transform.position, direction * 10f, Color.red);
            currentHits[key] = 10f;
        }
    }

    void OnRenderObject()
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

    void OnDrawGizmos()
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
