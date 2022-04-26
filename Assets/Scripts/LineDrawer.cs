using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LineDrawer : MonoBehaviour
{
    //Strokes
    public List<Stroke> strokes;
    Stroke nowStroke = null;

    //Controller triggers
    private SteamVR_Action_Pose pose;
    private SteamVR_Action_Boolean Iui = SteamVR_Actions.default_InteractUI;
    private bool interactui;

    bool writingNow = false;

    void Start()
    {
        strokes = new List<Stroke>();
    }

    // Update is called once per frame
    void Update()
    {
        interactui = Iui.GetState(SteamVR_Input_Sources.RightHand);
        if (interactui && !writingNow)
        {
            pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            writingNow = true;

            if(nowStroke != null)
            {
                strokes.Add(nowStroke);
            }

            nowStroke = new Stroke();
            nowStroke.addSample(pose.GetLocalPosition(SteamVR_Input_Sources.RightHand),
                pose.GetVelocity(SteamVR_Input_Sources.RightHand),
                Time.time);

            Debug.Log("Started Stroke Sketching");
        }
        else if(interactui && writingNow)
        {
            pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            nowStroke.addSample(pose.GetLocalPosition(SteamVR_Input_Sources.RightHand),
                pose.GetVelocity(SteamVR_Input_Sources.RightHand),
                Time.time);
        }
        else if(!interactui && writingNow)
        {
            writingNow = false;
            nowStroke.endSampling();
            Debug.Log("Ended Stroke Sketching");
        }
    }
}


public class Stroke : MonoBehaviour
{
    List<Vector3> positions;
    List<Vector3> velocities;
    List<float> times;
    int numSamples;

    PolyBezier pb;

    LineRenderer linerenderer;

    public Stroke()
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