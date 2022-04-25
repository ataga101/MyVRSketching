using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyBezier
{
    public List<Bezier> beziers = new List<Bezier> () { };
    public int numSegments = 0;

    public PolyBezier(List<Vector3> points)
    {
        numSegments = points.Count;
        Debug.Log(numSegments);
        for(int i=0; i<points.Count-1; i+=3)
        {
            var nowPoints = points.GetRange(i, 4);
            var bezier = new Bezier(nowPoints);
            beziers.Add(bezier);
        }
    }

    public void Render()
    {
        for(int i=0; i<beziers.Count; i++)
        {
            beziers[i].Render();
        }
    }
}
