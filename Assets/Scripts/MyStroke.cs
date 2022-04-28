using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MyStroke
{
    List<Vector3> positions;
    List<Vector3> velocities;
    List<float> times;
    int numSamples;

    float maxDistDelta = 0.2f;
    float maxTimeDelta = 0.2f;

    PolyBezier pb;

    GameObject gameObject;

    LineRenderer linerenderer;

    public MyStroke()
    {
        positions = new List<Vector3>();
        velocities = new List<Vector3>();
        times = new List<float>();

        numSamples = 0;
        gameObject = new GameObject();
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

        //Convert to PolyBezier -> Show
        convertToPolyBezier();
        pb.Render();
    }

    public void convertToPolyBezier()
    {
        var cPoints = new List<Vector3>();

        Vector3 formerPos = positions[0];
        Vector3 formerVel = velocities[0];
        float formerTime = times[0];
        cPoints.Add(positions[0]);
        Debug.Log(numSamples);

        float distDelta = 0f;

        for(int i=1; i<numSamples; i++)
        {
            var nowPos = positions[i];
            var nowVel = velocities[i];
            var nowTime = times[i];
            var timeDelta = nowTime - formerTime;
            distDelta += (nowPos - positions[i-1]).magnitude;
            if (timeDelta > maxTimeDelta || distDelta > maxDistDelta) 
            {
                Debug.Log("Added three points");
                Debug.Log((timeDelta, distDelta));
                cPoints.Add((formerVel * timeDelta / 3) + formerPos);
                cPoints.Add(nowPos - (nowVel * timeDelta / 3));
                cPoints.Add(nowPos);
                formerPos = nowPos;
                formerVel = nowVel;
                formerTime = nowTime;
                distDelta = 0f;
            }

        }

        pb = gameObject.AddComponent<PolyBezier>();
        pb.setControlPoints(cPoints);
    }
}