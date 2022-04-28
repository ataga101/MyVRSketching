using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyBezier : MonoBehaviour
{
    public List<Bezier> beziers = new List<Bezier> () { };
    public int numSegments = 0;

    public void setControlPoints(List<Vector3> points)
    {
        numSegments = points.Count;
        Debug.Log(numSegments);
        for(int i=0; i<points.Count-1; i+=3)
        {
            var nowPoints = points.GetRange(i, 4);
            var bezier = gameObject.AddComponent<Bezier>();
            bezier.SetPoint(nowPoints);
            beziers.Add(bezier);
        }
    }

    public void Render()
    {
        var lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        var Points = new List<Vector3>();
        for (int i=0; i<beziers.Count; i++)
        {
            Points.AddRange(beziers[i].GetPoints());
        }
        lineRenderer.positionCount = Points.Count;
        lineRenderer.SetPositions(Points.ToArray());
        Debug.Log("Rendered");
    }

    public Vector3 GetNearestPointTo(Vector3 pos)
    {
        float dist = 10e6f;
        Vector3 ret = Vector3.zero;
        foreach(Bezier b in beziers)
        {
            var (point, nowdist) = b.getNearestPosandDist(pos);
            if(nowdist < dist)
            {
                dist = nowdist;
                ret = point;
            }
        }

        return ret;
    }
}
