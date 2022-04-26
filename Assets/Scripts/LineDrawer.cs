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

