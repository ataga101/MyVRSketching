using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier
{
    public Vector3 P0, P1, P2, P3;

    public bool Rendered;
    GameObject gameObject;

    public int numSegment = 20;


    public Bezier(List<Vector3> P)
    {
        this.P0 = P[0];
        this.P1 = P[1];
        this.P2 = P[2];
        this.P3 = P[3];
        gameObject = new GameObject();
        Rendered = false;
    }

    public Vector3 GetPoint(float t)
    {   
        float one_t = 1f - t;

        return one_t * one_t * one_t * P0 +
            3f * one_t * one_t * t * P1 +
            3f * one_t * t * t * P2 +
            t * t * t * P3;
    }

    public void Render()
    {
        if (Rendered) { return; }
        else
        {
            List<Vector3> Points = new List<Vector3>();

            var lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.startColor = new Color(1f, 1f, 1f);
            lineRenderer.endColor = new Color(1f, 1f, 1f);
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.01f;
                
            for(int t = 0; t <= numSegment; t++)
            {
                Points.Add(this.GetPoint(t / (float)numSegment));            
            }

            lineRenderer.positionCount = numSegment + 1;
            lineRenderer.SetPositions(Points.ToArray());
        }
    }
}
