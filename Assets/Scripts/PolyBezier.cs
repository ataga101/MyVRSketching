using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolyBezier : MonoBehaviour
{
    public List<Bezier> beziers = new List<Bezier> () { };
    public int bezierCount = 0;
    public List<Vector3> controlPoints;

    GameObject ControlPointsViewer = null;

    public void setControlPoints(List<Vector3> points)
    {
        controlPoints = points;
        beziers = new List<Bezier>();
        for(int i=0; i<points.Count-1; i+=3)
        {
            var nowPoints = points.GetRange(i, 4);
            var bezier = gameObject.AddComponent<Bezier>();
            bezier.SetPoint(nowPoints);
            beziers.Add(bezier);
        }
        bezierCount = beziers.Count;
    }

    public void Render()
    {
        var lineRenderer = gameObject.GetComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
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

        if(bestT == 0f || bestT == 1f)
        {
            return (3 * (bestIdx + (int)bestT));
        }

        var bestBezier = beziers[bestIdx];
        var (L1, L2) = bestBezier.Split(bestT);

        //Update control points
        var pointsToInsert = new List<Vector3>() { L1[1], L1[2], L1[3], L2[1], L2[2] };
        controlPoints.RemoveRange(bestIdx * 3 + 1, 2);
        controlPoints.InsertRange(bestIdx * 3 + 1, pointsToInsert);

        this.setControlPoints(this.controlPoints);

        return bestIdx * 3 + 3;
    }

    public (Vector3, int, float) getNearestPosAndIdxAndT(Vector3 pos)
    {
        int bestBezierIdx = 0;
        float minDist = 10e6f;
        Vector3 bestPos = Vector3.zero;
        float bestT = 0f;

        for(int i=0; i<bezierCount; i++)
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

    public void addLineRenderer()
    {
        gameObject.AddComponent<LineRenderer>();
    }

    public void SetCollision()
    {
        foreach(var b in beziers)
        {
            //Debug.Log("Start setting collider");
            b.SetCollision();
        }
    }

    public Vector3 getTangentAt(int ctrlPtIdx)
    {
        if(ctrlPtIdx > 0)
        {
            return controlPoints[ctrlPtIdx] - controlPoints[ctrlPtIdx - 1];
        }
        else
        {
            return controlPoints[ctrlPtIdx + 1] - controlPoints[ctrlPtIdx]; 
        }
    }

    public void ShowControl()
    {
        LineRenderer lineRenderer;
        if(ControlPointsViewer != null)
        {
            Destroy(ControlPointsViewer);
        }
        ControlPointsViewer = new GameObject();
        ControlPointsViewer.name = "Control Points";
        ControlPointsViewer.transform.SetParent(this.gameObject.transform);
        lineRenderer = ControlPointsViewer.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
        lineRenderer.startWidth = 0.002f;
        lineRenderer.endWidth = 0.002f;

        int cnt = 0;
        foreach(var point in controlPoints)
        {
            var pointSphere = (cnt % 3 == 0) ? GameObject.CreatePrimitive(PrimitiveType.Cube) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointSphere.transform.SetParent(ControlPointsViewer.transform);
            pointSphere.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
            pointSphere.transform.position = point;
            cnt++;
        }

        lineRenderer.positionCount = controlPoints.Count;
        lineRenderer.SetPositions(controlPoints.ToArray());
    }

}
