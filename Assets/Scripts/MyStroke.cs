using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MyStroke
{
    List<Vector3> positions;
    List<Vector3> velocities;
    List<float> times;
    int numSamples;

    float maxDistDelta = 0.1f;
    float maxTimeDelta = 0.2f;

    public PolyBezier pb { get; set; }

    public GameObject gameObject;

    LineRenderer linerenderer;  

    public MyStroke(int index)
    {
        positions = new List<Vector3>();
        velocities = new List<Vector3>();
        times = new List<float>();

        numSamples = 0;
        
        gameObject = new GameObject();
        gameObject.name = "Stroke" + index.ToString();

        linerenderer = gameObject.AddComponent<LineRenderer>();
        linerenderer.material = new Material(Shader.Find("Sprites/Default"));
        linerenderer.startWidth = 0.01f;
        linerenderer.endWidth = 0.01f;
        linerenderer.startColor = Color.white;
        linerenderer.endColor = Color.white;
    }

    public void addSample(Vector3 position, Vector3 velocity, float time)
    {
        //Add sample
        positions.Add(position);
        velocities.Add(velocity);
        times.Add(time);
        numSamples++;

        //Set up LineRenderer
        linerenderer = gameObject.GetComponent<LineRenderer>();

        linerenderer.positionCount = numSamples;
        linerenderer.SetPositions(positions.ToArray());
    }

    public void endSampling()
    {
        //Convert to PolyBezier
        convertToPolyBezier();
    }

    public ConstraintSolver cs;

    public void FitAndShow(List<CollisionData> collisionData)
    {
        Debug.Log("Start Constraint Solving");
        cs = new ConstraintSolver(pb, collisionData, times, this.gameObject);
        Debug.Log("Constraint Generated");
        var newControlPoints = cs.solve();
        pb.setControlPoints(newControlPoints);
        Debug.Log("Constraint Solved");
        pb.Render();
    }

    public void convertToPolyBezier()
    {
        var cPoints = new List<Vector3>();

        Vector3 formerPos = positions[0];
        Vector3 formerVel = velocities[0];
        float formerTime = times[0];
        cPoints.Add(positions[0]);
        //Debug.Log(numSamples);

        float distDelta = 0f;

        for(int i=1; i<numSamples; i++)
        {
            var nowPos = positions[i];
            Vector3 nowVel;

            if (i > 0) {
                nowVel = (positions[i] - positions[i - 1]) / (times[i] - times[i - 1]);
            }
            else
            {
                nowVel = (positions[i + 1] - positions[i]) / (times[i + 1] - times[i]);
            }

            var nowTime = times[i];
            var timeDelta = nowTime - formerTime;
            distDelta += (nowPos - positions[i-1]).magnitude;
            if (timeDelta > maxTimeDelta || distDelta > maxDistDelta) 
            {
                var (p1, p2) = (formerPos + formerVel * timeDelta / 3, nowPos - nowVel * timeDelta / 3);
                cPoints.Add(p1);
                cPoints.Add(p2);
                cPoints.Add(nowPos);
                formerPos = nowPos;
                formerVel = nowVel;
                formerTime = nowTime;
                distDelta = 0f;
            }
        }

        pb = gameObject.AddComponent<PolyBezier>();
        pb.setControlPoints(cPoints);
        SaveControlPointsToFile(cPoints);
    }

    private (Vector3, Vector3) calcBestFitBezierControlPoints(Vector3 startPos, Vector3 endPos, Vector3 startVelocity, Vector3 endVelocity, List<Vector3> samplePositions, float timeDelta)
    {
        int steps = 20;
        float c1 = timeDelta;
        float c2 = timeDelta;

        GameObject tmpBezierObject = new GameObject();

        var (bestP1, bestP2) = (new Vector3(), new Vector3());
        var minDistanceSum = 10000000f;

        for(int i=0; i<=steps; i++)
        {
            for(int j=0; j<=steps; j++)
            {
                Vector3 tmpP1 = startPos + (c1 * i / steps) * startVelocity;
                Vector3 tmpP2 = endPos - (c2 * i / steps) * endVelocity;

                var tmpBezier = tmpBezierObject.AddComponent<Bezier>();
                tmpBezier.SetPoint(new List<Vector3>() { startPos, tmpP1, tmpP2, endPos });
                float sum = 0;
                foreach(var sample in samplePositions)
                {
                    var (pos, distance, t) = tmpBezier.getNearestPosAndDistAndT(sample);
                    sum += distance;
                }

                if(sum < minDistanceSum)
                {
                    minDistanceSum = sum;
                    bestP1 = tmpP1;
                    bestP2 = tmpP2;
                }
            }
        }

        GameObject.Destroy(tmpBezierObject);
        return (bestP1, bestP2);
    }

    private void SaveControlPointsToFile(List<Vector3> plist)
    {
        File.AppendAllText("C:\\Users\\ui-lab\\Desktop\\sample.txt", System.Environment.NewLine);
        foreach(var p in plist)
        {
            File.AppendAllText("C:\\Users\\ui-lab\\Desktop\\sample.txt", "new Vector3(" + p.x.ToString() + ", " + p.y.ToString() + ", " + p.z.ToString() + "), " + System.Environment.NewLine);
        }        
    }

    public void SetCollision()
    {
        pb.SetCollision();
    }
}