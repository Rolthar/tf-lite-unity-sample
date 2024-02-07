using UnityEngine;

public class Leader : MonoBehaviour
{
    public static Leader Instance { get; private set; }

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    public Vector3 PositionDelta { get; private set; }
    public Quaternion RotationDelta { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void Update()
    {
        // Calculate position and rotation deltas
        PositionDelta = transform.position - lastPosition;
        RotationDelta = transform.rotation * Quaternion.Inverse(lastRotation);

        // Update last position and rotation for the next frame
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
