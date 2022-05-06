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

    public float minControlPointDistance = 0.15f;

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
            if(cData.collisionTime - formerTime > 0.2f || cData.strokeId != formerId)
            {
                intersectionCandidate.Add((cData.strokeId, cData.collisionTime, cData.collisionPos));
                formerTime = cData.collisionTime;
                formerId = cData.strokeId;
            }
        }
        this.sampledTimes = sampledTimes;
        this.candidateNum = intersectionCandidate.Count;
    }


    public (List<Vector3>, List<(int, Vector3)>, List<(int, Vector3)>) Generate(List<bool> disableMap)
    {
        //Reset PolyBezier curve
        pb.setControlPoints(initialControlPoints);

        List<(int, Vector3)> retc0Constraint = new List<(int, Vector3)>();
        List<(int, Vector3)> rettangentConstraint = new List<(int, Vector3)>();

        var nowIntersectionCandidate = new List<(int, float, Vector3)>();

        for (int i=0; i<candidateNum; i++)
        {
            if (!disableMap[i])
            {
                nowIntersectionCandidate.Add(intersectionCandidate[i]);
            }
        }
        List<bool> ControlPointUsed = new List<bool>();

        for(int i=0; i<pb.controlPoints.Count; i++)
        {
            ControlPointUsed.Add(false);
        }
        
        
        int candidateIdx = 0;
        int numSplit = 0;

        for(int i=0; i<pb.bezierCount-1 && candidateIdx < nowIntersectionCandidate.Count && i<sampledTimes.Count-1; i++)
        {

            for (; candidateIdx < nowIntersectionCandidate.Count; candidateIdx++)
            {
                var (strokeId, collisionTime, collisionPos) = nowIntersectionCandidate[candidateIdx];
                if(sampledTimes[i + 1] < collisionTime)
                {
                    break;
                }

                //Decide intersection
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

        return (pb.controlPoints, retc0Constraint, rettangentConstraint);
    }
}
