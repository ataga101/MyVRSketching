using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyBezier
{
    List<Bezier> beziers = new List<Bezier> () { };

    public PolyBezier(List<Vector3> points)
    {
        for(int i=0; i<points.Count-1; i+=3)
        {
            var nowPoints = points.GetRange(i, 4);
            var bezier = new Bezier(nowPoints);
            beziers.Add(bezier);
        }
    }

    public void Render(LineRenderer lineRenderer)
    {
        for(int i=0; i<beziers.Count; i++)
        {
            beziers[i].Render(lineRenderer);
            Debug.Log(i);
        }
    }
}
