using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class Conecast : MonoBehaviour
{
    public float coneRadius = 8f;
    public float coneAngle = 60f;

    public List<SemanticItem> SemanticGazeList = new();

    public UnityEvent<List<SemanticItem>> OnSemanticListUpdate = new UnityEvent<List<SemanticItem>>();

    void Update()
    {

        var newList = FindObjectsInCone<SemanticItem>(transform.position, transform.forward, coneRadius, coneAngle);
        if (newList.Count > 0)
        {
            SemanticGazeList = newList;

            OnSemanticListUpdate.Invoke(newList);
        }
        else
        {
            SemanticGazeList = new();

            OnSemanticListUpdate.Invoke(new List<SemanticItem>());
        }

    }

    List<T> FindObjectsInCone<T>(Vector3 origin, Vector3 direction, float radius, float angle) where T : Component
    {
        List<T> objectsInCone = new List<T>();
        Collider[] hits = Physics.OverlapSphere(origin, radius);
        foreach (var hit in hits)
        {
            Vector3 toHit = (hit.transform.position - origin).normalized;
            if (Vector3.Angle(direction, toHit) <= angle / 2)
            {
                T component = hit.GetComponent<T>();
                if (component != null)
                {
                    objectsInCone.Add(component);
                }
            }
        }

        // Perform raycasts and filter out objects based on the raycast results
        var objectsToRemove = new List<T>();
        foreach (var obj in objectsInCone)
        {
            RaycastHit hitInfo;
            Vector3 toObject = obj.transform.position - origin;
            if (Physics.Raycast(origin, toObject.normalized, out hitInfo, toObject.magnitude))
            {
                // Check if the hit is not the object itself
                if (hitInfo.collider.gameObject != obj.gameObject)
                {
                    // If the difference in distance is greater than 1, plan to remove the object from the list
                    if (Mathf.Abs(hitInfo.distance - toObject.magnitude) > 1)
                    {
                        objectsToRemove.Add(obj);
                    }
                }
            }
        }

        // Remove the objects that didn't pass the raycast check
        foreach (var objToRemove in objectsToRemove)
        {
            objectsInCone.Remove(objToRemove);
        }

        return objectsInCone;
    }



    void OnRenderObject()
    {
        RuntimeGizmos.Cone(transform.position, transform.rotation, 8f, 60f, new Color(1, 1, 1, 0.3f), true);
    }

    void OnDrawGizmos()
    {
        RuntimeGizmos.Cone(transform.position, transform.rotation, 8f, 60f, new Color(1, 1, 1, 0.3f), true);
    }
}
