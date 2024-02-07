using UnityEngine;

public class Follower : MonoBehaviour
{
    private Vector3 lastLeaderEulerAngles;

    void Start()
    {
        if (Leader.Instance != null)
        {
            lastLeaderEulerAngles = Leader.Instance.transform.rotation.eulerAngles;
        }
    }

    void Update()
    {
        if (Leader.Instance != null)
        {
            // Get the current leader's Euler angles
            Vector3 currentLeaderEulerAngles = Leader.Instance.transform.rotation.eulerAngles;

            // Calculate the delta in Euler angles
            Vector3 eulerAngleDelta = currentLeaderEulerAngles - lastLeaderEulerAngles;

            // Correct potential wrap-around issues by ensuring the delta is within -180 to 180 degrees
            eulerAngleDelta = NormalizeEulerAngleDelta(eulerAngleDelta);

            // Apply the Euler angle delta to the follower's local Euler angles
            transform.localEulerAngles += eulerAngleDelta;

            // Update lastLeaderEulerAngles for the next frame
            lastLeaderEulerAngles = currentLeaderEulerAngles;

            // Position adjustment code remains the same
            Vector3 leaderMovementInLocalSpace = Leader.Instance.transform.InverseTransformDirection(Leader.Instance.PositionDelta);
            Vector3 totalMovement = transform.forward * leaderMovementInLocalSpace.z +
                                    transform.right * leaderMovementInLocalSpace.x +
                                    transform.up * leaderMovementInLocalSpace.y;
            transform.position += totalMovement;
        }
    }

    Vector3 NormalizeEulerAngleDelta(Vector3 delta)
    {
        // Adjust each component of the delta to be within -180 to 180
        delta.x = NormalizeAngle(delta.x);
        delta.y = NormalizeAngle(delta.y);
        delta.z = NormalizeAngle(delta.z);
        return delta;
    }

    float NormalizeAngle(float angle)
    {
        // Normalize an angle to be within -180 to 180
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
}
