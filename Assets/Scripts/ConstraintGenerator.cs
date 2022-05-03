using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstraintGenerator
{
    PolyBezier pb;
    List<Vector3> initialControlPoints;
    List<CollisionData> collisionData;

    List<(int, float, Vector3)> intersectionCandidate;
    public int candidateNum { get; set; }

    private List<float> sampledTimes;

    public float minControlPointDistance = 0.1f;

    GameObject cgObject;

    public ConstraintGenerator(List<Vector3> controlPoints, List<CollisionData> collisionData, List<float> sampledTimes)
    {
        cgObject = new GameObject();
        cgObject.name = "ConstraintGenerator";
        this.pb = cgObject.AddComponent<PolyBezier>();
        pb.setControlPoints(controlPoints);

        this.initialControlPoints = new List<Vector3>(controlPoints);

        this.collisionData = collisionData;

        //CollisionData -> Intersection Candidate
        float formerTime = 0f;
        int formerId = -1;

        intersectionCandidate = new List<(int, float, Vector3)>();

        foreach(var cData in collisionData)
        {
            Debug.Log("Collision Data");
            if(cData.collisionTime - formerTime > 0.1f || cData.strokeId != formerId)
            {
                Debug.Log((cData.strokeId, cData.collisionPos, cData.collisionTime)) ;
                intersectionCandidate.Add((cData.strokeId, cData.collisionTime, cData.collisionPos));
                formerTime = cData.collisionTime;
                formerId = cData.strokeId;
            }
        }
        this.sampledTimes = sampledTimes;
    }


    public (List<Vector3>, List<(int, Vector3)>, List<(int, Vector3)>) Generate(List<bool> disableMap)
    {
        //Reset PolyBezier curve
        pb.setControlPoints(initialControlPoints);

        List<(int, Vector3)> retc0Constraint = new List<(int, Vector3)>();
        List<(int, Vector3)> rettangentConstraint = new List<(int, Vector3)>();

        var nowIntersectionCandidate = intersectionCandidate;

        int numDisabled = 0;
        //Debug.Log("aHOGE1");

        for (int i=0; i<candidateNum; i++)
        {
            if (disableMap[i])
            {
                Debug.Log("Removed");
                nowIntersectionCandidate.RemoveAt(i - numDisabled);
                numDisabled++;
            }
        }

        //Debug.Log("aHOGE2");
        List<bool> ControlPointUsed = new List<bool>();

        for(int i=0; i<pb.controlPoints.Count; i++)
        {
            ControlPointUsed.Add(false);
        }

        //Debug.Log(nowIntersectionCandidate.Count);
        //Debug.Log("aHOGE34");
        
        int candidateIdx = 0;
        int numSplit = 0;

        for(int i=0; i<pb.bezierCount-1 && candidateIdx < nowIntersectionCandidate.Count; i++)
        {
            var (strokeId, collisionTime, collisionPos) = nowIntersectionCandidate[candidateIdx];
            Debug.Log((i, pb.bezierCount, sampledTimes.Count));

            if (sampledTimes[i] <= collisionTime && sampledTimes[i + 1] >= collisionTime)
            {
                var nearestControlPoint1 = pb.controlPoints[i * 3];
                var nearestControlPoint2 = pb.controlPoints[(i + 1) * 3];

                var dist1 = (collisionPos - nearestControlPoint1).magnitude;
                var dist2 = (collisionPos - nearestControlPoint2).magnitude;

                float mindist;
                int cpIdx;
                if(dist1 < dist2)
                {
                    mindist = dist1;
                    cpIdx = i * 3;
                }
                else
                {
                    mindist = dist2;
                    cpIdx = (i + 1) * 3;
                }

                cpIdx += numSplit;

                if(mindist > minControlPointDistance || ControlPointUsed[i])
                {
                    cpIdx = pb.SplitNear(collisionPos);
                    numSplit++;
                }
                else
                {
                    ControlPointUsed[cpIdx / 3] = true;
                }
                candidateIdx++;
                retc0Constraint.Add((cpIdx, collisionPos));
            }
        }

        //Debug.Log(retc0Constraint.Count);
        return (pb.controlPoints, retc0Constraint, rettangentConstraint);
    }
}
