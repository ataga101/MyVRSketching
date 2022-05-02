using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyBezier : MonoBehaviour
{
    public List<Bezier> beziers = new List<Bezier> () { };
    public int numSegments = 0;
    public List<Vector3> controlPoints;

    public void setControlPoints(List<Vector3> points)
    {
        controlPoints = points;
        //Debug.Log(numSegments);
        for(int i=0; i<points.Count-1; i+=3)
        {
            var nowPoints = points.GetRange(i, 4);
            var bezier = gameObject.AddComponent<Bezier>();
            bezier.SetPoint(nowPoints);
            beziers.Add(bezier);
        }
        numSegments = beziers.Count;
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
    }

    //Split bezier and return control point index
    public int SplitNear(Vector3 pos)
    { 
        var (bestPos, bestIdx, bestT) = getNearestPosAndIdxAndT(pos);
        var bestBezier = beziers[bestIdx];
        var (L1, L2) = bestBezier.Split(bestT);

        //Update Beziers
        var newBezier1 = gameObject.AddComponent<Bezier>();
        var newBezier2 = gameObject.AddComponent<Bezier>();
        newBezier1.SetPoint(L1);
        newBezier2.SetPoint(L2);

        Destroy(beziers[bestIdx]);
        beziers.RemoveAt(bestIdx);

        beziers.Insert(bestIdx, newBezier2);
        beziers.Insert(bestIdx, newBezier1);

        //Update control points
        var pointsToInsert = new List<Vector3>() { L1[1], L1[2], L1[3], L2[2], L2[3] };
        controlPoints.RemoveRange(bestIdx * 3 + 1, bestIdx * 3 + 3);
        controlPoints.InsertRange(bestIdx * 3 + 1, pointsToInsert);

        return bestIdx * 3 + 3;
    }

    public (Vector3, int, float) getNearestPosAndIdxAndT(Vector3 pos)
    {
        int bestBezierIdx = 0;
        float minDist = 10e6f;
        Vector3 bestPos = Vector3.zero;
        float bestT = 0f;

        for(int i=0; i<numSegments; i++)
        {
            //Debug.Log("FUGA");
            Bezier b = beziers[i];
            (Vector3 nowPos, float nowdist, float nowT) = b.getNearestPosAndDistAndT(pos);
            if (nowdist < minDist)
            {
                minDist = nowdist;
                bestPos = nowPos;
                bestT = nowT;
                bestBezierIdx = i;
            }
        }

        return (bestPos, bestBezierIdx, bestT);
    }



    public void SetCollision()
    {
        foreach(var b in beziers)
        {
            //Debug.Log("Start setting collider");
            b.SetCollision();
        }
    }

}
