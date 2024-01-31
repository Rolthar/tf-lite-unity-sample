using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Area : MonoBehaviour
{
    public string Name;
    public Level level;
    public MeshRenderer rend;

    public List<SemanticItem> SemanticsInArea;
    void Awake()
    {
        rend = gameObject.GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
