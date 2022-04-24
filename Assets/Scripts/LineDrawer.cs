using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LineDrawer : MonoBehaviour
{
    // Start is called before the first frame update
    [System.Obsolete]

    public PolyBezier bezier;

    [System.Obsolete]
    void Awake()
    {
        this.bezier = new PolyBezier(
            new List<Vector3>() {
            new Vector3(0, 0, 0),
            new Vector3(8, 0, 0),
            new Vector3(8, -3, 0),
            new Vector3(0, -3, 0),
            new Vector3(-4, -3, 0),
            new Vector3(0, -3, -4),
            new Vector3(0, 0, -4)});

        this.bezier.Render();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
