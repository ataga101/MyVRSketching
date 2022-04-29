using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LineDrawer : MonoBehaviour
{
    //MyStrokes
    public List<MyStroke> MyStrokes;
    MyStroke nowMyStroke = null;
    int StrokeIdx = 0;

    //Controller triggers
    private SteamVR_Action_Pose pose;
    private SteamVR_Action_Boolean Iui = SteamVR_Actions.default_InteractUI;
    private bool interactui;

    bool writingNow = false;

    void Start()
    {
        MyStrokes = new List<MyStroke>();
        writingNow = false;
    }

    // Update is called once per frame
    void Update()
    {
        interactui = Iui.GetState(SteamVR_Input_Sources.RightHand);
        if (interactui && !writingNow)
        {
            pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            writingNow = true;
            Debug.Log("Started MyStroke Sketching");

            //Create a new stroke
            nowMyStroke = new MyStroke(StrokeIdx);
            StrokeIdx++;
            //Add a sample
            nowMyStroke.addSample(pose.GetLocalPosition(SteamVR_Input_Sources.RightHand),
                pose.GetVelocity(SteamVR_Input_Sources.RightHand),
                Time.time);

        }
        else if(interactui && writingNow)
        {
            pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            nowMyStroke.addSample(pose.GetLocalPosition(SteamVR_Input_Sources.RightHand),
                pose.GetVelocity(SteamVR_Input_Sources.RightHand),
                Time.time);
        }
        else if(!interactui && writingNow)
        {
            writingNow = false;
            nowMyStroke.addSample(pose.GetLocalPosition(SteamVR_Input_Sources.RightHand),
                pose.GetVelocity(SteamVR_Input_Sources.RightHand),
                Time.time);
            nowMyStroke.endSampling();
            MyStrokes.Add(nowMyStroke);
            Debug.Log("Ended MyStroke Sketching");
        }
    }
}

