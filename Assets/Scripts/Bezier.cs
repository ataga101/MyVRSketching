using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bezier : MonoBehaviour
{
    public Vector3 P0, P1, P2, P3;

    public bool Rendered;

    public int numSegment = 50;


    public void Awake()
    {
        Rendered = false;
        
    }

    public void SetPoint(List<Vector3> P)
    {
        this.P0 = P[0];
        this.P1 = P[1];
        this.P2 = P[2];
        this.P3 = P[3];
    }
    public Vector3 GetPoint(float t)
    {   
        float one_t = 1f - t;

        return one_t * one_t * one_t * P0 +
            3f * one_t * one_t * t * P1 +
            3f * one_t * t * t * P2 +
            t * t * t * P3;
    }

    public List<Vector3> GetPoints()
    {
        List<Vector3> Points = new List<Vector3>();

                
        for(int t = 0; t <= numSegment; t++)
        {
            Points.Add(this.GetPoint(t / (float)numSegment));            
        }

        return Points;
    }

    public (float, float) getNearestT(Vector3 pos)
    {
        for(int i=0; i<numSegment; i++)
        {
           
        }

        return (0f, 0f);
    }

}
