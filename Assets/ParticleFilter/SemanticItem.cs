using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZebrarWayfinding;

public class SemanticItem : MonoBehaviour
{
    public SemanticItemType type;


    void Awake()
    {

    }

    void Start()
    {
        FilterTwin.SemanticItems.Add(this);
    }

    // Update is called once per frame
    void Update()
    {

    }


    void OnRenderObject()
    {
        RuntimeGizmos.Sphere(transform.position, 0.2f, Color.yellow);
    }

    void OnDrawGizmos()
    {
        RuntimeGizmos.Sphere(transform.position, 0.2f, Color.yellow);
    }
}
