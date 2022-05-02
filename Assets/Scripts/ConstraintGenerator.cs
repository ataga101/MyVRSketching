using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstraintGenerator
{
    PolyBezier pb;
    List<CollisionData> collisionData;

    List<(int, float, Vector3)> intersectionCandidate;
    public int candidateNum { get; set; }

    private List<float> sampledTimes;

    public float minControlPointDistance = 0.1f;

    public ConstraintGenerator(PolyBezier pb, List<CollisionData> collisionData, List<float> sampledTimes)
    {
        this.pb = pb;
        this.collisionData = collisionData;

        //CollisionData -> Intersection Candidate
        float formerTime = 0f;
        int formerId = -1;
        
        foreach(var cData in collisionData)
        {
            if(!((cData.collisionTime - formerTime < 0.2f) && cData.strokeId == formerId))
            {
                intersectionCandidate.Add((cData.strokeId, cData.collisionTime, cData.collisionPos));
            }
        }
        this.sampledTimes = sampledTimes;
    }


    public (PolyBezier, List<(int, Vector3)>, List<(int, Vector3)>) Generate(List<bool> disableMap)
    {
        List<(int, Vector3)> retc0Constraint = new List<(int, Vector3)>();
        List<(int, Vector3)> rettangentConstraint = new List<(int, Vector3)>();

        var nowPb = pb;
        var nowIntersectionCandidate = intersectionCandidate;

        int numDisabled = 0;
        for(int i=0; i<candidateNum; i++)
        {
            if (disableMap[i])
            {
                nowIntersectionCandidate.RemoveAt(i - numDisabled);
                numDisabled++;
            }
        }

        List<bool> ControlPointUsed = new List<bool>(pb.numSegments + 1) {false };

        int candidateIdx = 0;
        Debug.Assert(sampledTimes.Count == pb.numSegments);
        for(int i=0; i<pb.numSegments; i++)
        {
            var (strokeId, collisionTime, collisionPos) = nowIntersectionCandidate[candidateIdx];
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

                if(mindist > minControlPointDistance || ControlPointUsed[i])
                {
                    cpIdx = nowPb.SplitNear(collisionPos);
                }
                else
                {
                    ControlPointUsed[cpIdx / 3] = true;
                }

                retc0Constraint.Add((cpIdx, collisionPos));
            }
        }
        return (nowPb, retc0Constraint, rettangentConstraint);
    }
}
