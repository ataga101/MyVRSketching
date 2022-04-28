using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MyStroke : MonoBehaviour
{
    List<Vector3> positions;
    List<Vector3> velocities;
    List<float> times;
    int numSamples;

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
    }

    public void convertToPolyBezier()
    {

    }
}