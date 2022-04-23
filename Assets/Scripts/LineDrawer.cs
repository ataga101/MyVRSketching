using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
    // Start is called before the first frame update
    [System.Obsolete]

    public PolyBezier bezier;

    [System.Obsolete]
    void Awake()
    {
        // Attach linerenderer
        var lineRenderer = gameObject.AddComponent<LineRenderer>();


        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;


        this.bezier = new PolyBezier(
            new List<Vector3>() {
            new Vector3(0, 0, 0),
            new Vector3(8, 0, 0),
            new Vector3(8, -3, 0),
            new Vector3(0, -3, 0),
            new Vector3(0, -3, -4),
            new Vector3(0, 0, -4),
            new Vector3(0, 0, 0)});

        this.bezier.Render(lineRenderer);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
