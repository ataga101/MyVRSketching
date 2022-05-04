using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;


public class InkSphere : MonoBehaviour
{
    GameObject lineDrawerObject;
    LineDrawer lineDrawer;

    private void Awake()
    {
        lineDrawerObject = GameObject.FindGameObjectWithTag("LineDrawer");
        lineDrawer = lineDrawerObject.GetComponent<LineDrawer>();
    }

    private void Update()
    {
        var pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
        transform.position = pose.GetLocalPosition(SteamVR_Input_Sources.RightHand);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Collision Detected");
        lineDrawer.AddCollisionData(
                collision.gameObject.transform.parent.gameObject.name,
                Time.time,
                collision.GetContact(0).point);
    }
}

