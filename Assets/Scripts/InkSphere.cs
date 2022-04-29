using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class InkSphere : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var pose = SteamVR_Input.GetAction<SteamVR_Action_Pose>("Pose");
        transform.position = pose.GetLocalPosition(SteamVR_Input_Sources.RightHand);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.transform.parent.gameObject.name);
        Debug.Log(collision.GetContact(0).point);
    }
}
