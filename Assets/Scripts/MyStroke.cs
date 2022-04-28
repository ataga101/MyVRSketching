using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MyStroke : MonoBehaviour
{
    List<Vector3> positions;
    List<Vector3> velocities;
    List<float> times;
    int numSamples;

    float maxDist = 2f;
    float maxTimeDelta = 0.3f;

    PolyBezier pb;

    LineRenderer linerenderer;

    public MyStroke()
    {
        linerenderer = gameObject.AddComponent<LineRenderer>();
        numSamples = 0;
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

        linerenderer.widthMultiplier = 0.1f;
        linerenderer.startColor = Color.white;
        linerenderer.endColor = Color.white;

        linerenderer.positionCount = numSamples;
        linerenderer.SetPosition(numSamples - 1, position);
    }

    public void endSampling()
    {
        Destroy(linerenderer);

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

        for(int i=1; i<numSamples; i++)
        {
            var nowPos = positions[i];
            var nowVel = velocities[i];
            var nowTime = times[i];
            var timeDelta = nowTime - formerTime;
            if(((nowPos - formerPos).magnitude > maxDist) || ((timeDelta) > maxTimeDelta))
            {
                cPoints.Add((timeDelta / 3f) * formerVel + formerPos);
                cPoints.Add(nowPos - (timeDelta / 3f) * nowVel);
                cPoints.Add(nowPos);
            }

            formerPos = nowPos;
            formerVel = nowVel;
            formerTime = nowTime;
        }

        pb = new PolyBezier(cPoints);
    }
}