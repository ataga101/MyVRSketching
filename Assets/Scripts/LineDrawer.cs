using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class LineDrawer : MonoBehaviour
{
    //MyStrokes
    public List<MyStroke> MyStrokes = new List<MyStroke> ();
    MyStroke nowMyStroke = null;
    int StrokeIdx = 0;
    private Dictionary<string, int> strokeNameToIdx = new Dictionary<string, int>();

    //Controller triggers
    private SteamVR_Action_Pose pose;
    private SteamVR_Action_Boolean Iui = SteamVR_Actions.default_InteractUI;
    private bool interactui;

    bool writingNow = false;

    bool isFreehandMode = false;

    List<CollisionData> cdList = new List<CollisionData>();

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

            //Create a new stroke
            nowMyStroke = new MyStroke(StrokeIdx);
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

            if (!isFreehandMode)
            {
                nowMyStroke.endSampling();
                nowMyStroke.FitAndShow(cdList);
                cdList = new List<CollisionData>();

                //Debug.Log("Rendered");
                nowMyStroke.SetCollision();
                //Debug.Log("Set collider");
            }
            MyStrokes.Add(nowMyStroke);
            strokeNameToIdx[nowMyStroke.gameObject.name] = StrokeIdx;
            StrokeIdx++;
            //Debug.Log("Ended MyStroke Sketching");
        }

        var changeMode = Iui.GetState(SteamVR_Input_Sources.LeftHand);
        if (changeMode)
        {
            isFreehandMode = !isFreehandMode;
        }

    }

    public void AddCollisionData(string strokeName, float collisionTime, Vector3 collisionPos)
    {
        Debug.Log("Added collision");
        if (!strokeNameToIdx.ContainsKey(strokeName))
        {
            return;
        }
        int strokeIdx = strokeNameToIdx[strokeName];

        //Collision point -> Point on bezier curve
        Debug.Assert(MyStrokes.Count > strokeIdx);
        //Debug.Log("HOGE");
        //Debug.Log((MyStrokes.Count, strokeIdx));
        var stroke = MyStrokes[strokeIdx];
        //Debug.Log("HOGE2");
        var (pos, idx, T) = stroke.pb.getNearestPosAndIdxAndT(collisionPos);
        //Debug.Log("HOGE3");

        var cd = new CollisionData(strokeNameToIdx[strokeName], collisionTime, pos);
        if(interactui || writingNow)
        {
            cdList.Add(cd);
            //Debug.Log("FUGA");
        }
    }
}


public class CollisionData
{
    public int strokeId;
    public float collisionTime;
    public Vector3 collisionPos;

    public CollisionData(int StrokeId, float CollisionTime, Vector3 CollisionPos)
    {
        strokeId = StrokeId;
        collisionTime = CollisionTime;
        collisionPos = CollisionPos;
    }
}

