using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

public class LineDrawer : MonoBehaviour
{
    //Strokes
    public List<PolyBezier> strokes;

    //Controller triggers
    private SteamVR_Action_Pose poseActionR;
    private SteamVR_Action_Boolean Iui = SteamVR_Actions.default_InteractUI;
    private bool interactui;

    //Sampling data
    public float sampleDelta = 0.2f;
    public float sampleWidth = 2f;
    float elapsedTime;
    List<Vector3> ctrlPoints;
    bool writingNow = false;
    bool isFirstSample = true;

    void Awake()
    {
        /*test
        this.bezier = new PolyBezier(
            new List<Vector3>() {
            new Vector3(0, 0, 0),
            new Vector3(8, 0, 0),
            new Vector3(8, -3, 0),
            new Vector3(0, -3, 0),
            new Vector3(-4, -3, 0),
            new Vector3(0, -3, -4),
            new Vector3(0, 0, -4)});

        this.bezier.Render();
        */
        strokes = new List<PolyBezier>();
        ctrlPoints = new List<Vector3>();
    }

    // Update is called once per frame
    void Update()
    {
        interactui = Iui.GetState(SteamVR_Input_Sources.RightHand);
        if (interactui && !writingNow)
        {
            poseActionR = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            ctrlPoints = new List<Vector3>();
            isFirstSample = true;
            elapsedTime = 0f;
            writingNow = true;
            Debug.Log("Started Stroke Sketching");
        }
        else if(interactui && writingNow)
        {
            elapsedTime += Time.deltaTime;
            poseActionR = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            Vector3 nowPos = poseActionR.GetLocalPosition(SteamVR_Input_Sources.RightHand);
            Vector3 diffvec = Vector3.zero;
            float diff = 0;
            diffvec = nowPos - ctrlPoints[ctrlPoints.Count - 1];
            diff = Mathf.Sqrt(Vector3.Dot(diffvec, diffvec));
            if(elapsedTime > sampleDelta || ((!isFirstSample) && diff > sampleWidth))
            {
                ctrlPoints.Add(-(poseActionR.GetVelocity(SteamVR_Input_Sources.RightHand) * sampleDelta / 3) + nowPos);
                ctrlPoints.Add(nowPos);
                ctrlPoints.Add(poseActionR.GetVelocity(SteamVR_Input_Sources.RightHand) * sampleDelta / 3 + nowPos);
                Debug.Log("Added a Point");
                Debug.Log(poseActionR.GetVelocity(SteamVR_Input_Sources.RightHand) * sampleDelta / 3 + nowPos);
                elapsedTime = 0f;
            }
        }
        else if(!interactui && writingNow)
        {
            elapsedTime += Time.deltaTime;
            poseActionR = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
            var nowPos = poseActionR.GetLocalPosition(SteamVR_Input_Sources.RightHand);
            ctrlPoints.Add(-(poseActionR.GetVelocity(SteamVR_Input_Sources.RightHand) * sampleDelta / 3) + nowPos);
            ctrlPoints.Add(nowPos);
            writingNow = false;

            var pb = new PolyBezier(ctrlPoints);
            pb.Render();
            Debug.Log("Ended Stroke Sketching");
        }
    }
}


public class Stroke
{
    public float sampleDelta = 0.2f;
    float formerSampleTime;
    Vector3 oneFrameFormerPos;

    PolyBezier pb;

}