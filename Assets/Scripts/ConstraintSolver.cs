using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class ConstraintSolver
{
    int bezierCount;
    int N;

    PolyBezier pb;
    List<Vector3> ControlPoints;

    PolyBezier newPb;
    List<Vector3> newControlPoints;

    float[] tNorms;

    List<(int, Vector3)> c0Constraints;
    List<(int, Vector3)> tangentConstraints;

    float eps = 10e-6f;
    float displacement_normalizer = 0.04f;

    ConstraintGenerator constraintGenerator;

    public ConstraintSolver(PolyBezier pb, List<CollisionData> collisionData, List<float> sampledTimes)
    {
        //number of bezier curve
        this.bezierCount = pb.bezierCount;
        //number of control points
        this.N = bezierCount * 3 + 1;

        //control points
        ControlPoints = new List<Vector3>();
        for (int i = 0; i < pb.beziers.Count; i++)
        {
            ControlPoints.Add(pb.beziers[i].P0);
            ControlPoints.Add(pb.beziers[i].P1);
            ControlPoints.Add(pb.beziers[i].P2);
        }
        ControlPoints.Add(pb.beziers[pb.beziers.Count - 1].P3);

        this.pb = pb;

        constraintGenerator = new ConstraintGenerator(pb, collisionData, sampledTimes);
    }

    public PolyBezier solve()
    {
        var disabledMap = new List<bool>();
        var removedMap = new List<bool>();
        var bestMap = new List<bool>();
        for(int i=0; i<constraintGenerator.candidateNum; i++)
        {
            disabledMap.Add(false);
            removedMap.Add(false);
            bestMap.Add(false);
        }

        PolyBezier bestPb;
        float minEnergy;
        bool noPointRemoved = false;

        Debug.Log("HOGE1");
        (bestPb, c0Constraints, tangentConstraints) = constraintGenerator.Generate(disabledMap);
                Debug.Log("HOGE2");
        solveSingle();
                Debug.Log("HOGE3");
        minEnergy = computeEfidelity();
        Debug.Log("HOGE4");

        while (!noPointRemoved)
        {
            noPointRemoved = true;
            for(int i=0; i<constraintGenerator.candidateNum; i++)
            {
                Debug.Log("HOGE");
                Debug.Log(i);
                if (removedMap[i])
                {
                    continue;
                }
                disabledMap = removedMap;
                disabledMap[i] = true;
                
                (pb, c0Constraints, tangentConstraints) = constraintGenerator.Generate(disabledMap);
                solveSingle();
                float energy = computeEfidelity();

                if (energy < minEnergy)
                {
                    noPointRemoved = false;
                    bestMap = disabledMap;
                    bestPb = newPb;
                    minEnergy = energy;
                }
            }
            removedMap = bestMap;
        }
        
        return bestPb;
    }

    private float computeEConnectivity(List<bool> disabledMap)
    {
        float ret = 0f;
        return ret;
    }

    public void solveSingle() { 
        //solve
        var A_tmp = Matrix<float>.Build.Dense(3 * N, 3 * N);
        var b_tmp = Vector<float>.Build.Dense(3 * N);

        //Debug.Log("FUGA");
        var (A, b) = EfidelityMat();
        A_tmp += A;
        b_tmp += b;

        //Debug.Log("FUGA1");
        (A, b) = EPlainerMat();
        A_tmp += A;
        b_tmp += b;

        //Debug.Log("FUGA2");
        foreach ((int idx, Vector3 pos) in tangentConstraints)
        {
            (A, b) = EtangentMat(pos, idx);
            A_tmp += A;
            b_tmp += b;
        }

        //Debug.Log("FUGA3");
        bool hasg1Constraint = (N > 4);
        bool isSelfConstraint = (ControlPoints[0] - ControlPoints[ControlPoints.Count-1]).magnitude < 0.05;

        //Debug.Log("FUGA4");
        int g1ConstraintCount = (hasg1Constraint) ? 1 : 0;
        int selfConstraintCount = (isSelfConstraint) ? 1 : 0;

        //Debug.Log("FUGA5");
        int numConstraints = c0Constraints.Count + g1ConstraintCount + selfConstraintCount;

        var C_tmp = new Matrix<float>[numConstraints, 1];
        var b_tmp2 = new List<float>();

        //Debug.Log("FUGA6");
        for (int i = 0; i < c0Constraints.Count; i++)
        {
            var (idx, pos) = c0Constraints[i];

            (Matrix<float> C_ret, Vector<float> b_ret) = c0Mat(idx, pos);
            C_tmp[i, 0] = C_ret;
            b_tmp2.AddRange(b_ret.Enumerate());
        }

        //Debug.Log("FUGA7");
        if(hasg1Constraint)
        {
            (Matrix<float> C_ret, Vector<float> b_ret) = g1Mat();
            C_tmp[c0Constraints.Count, 0] = C_ret;
            b_tmp2.AddRange(b_ret.Enumerate());
        }

        //Debug.Log("FUGA8");
        if (isSelfConstraint)
        {
            (Matrix<float> C_ret, Vector<float> b_ret) = selfc0Mat();
            C_tmp[c0Constraints.Count + g1ConstraintCount, 0] = C_ret;
            b_tmp2.AddRange(b_ret.Enumerate());
        }

        //Debug.Log("FUGA9");
        var M_tmp = new Matrix<float>[2, 2];

        var C = Matrix<float>.Build.DenseOfMatrixArray(C_tmp);
        List<float> b_tmp_enu = new List<float> (b_tmp.Enumerate());
        b_tmp_enu.AddRange(b_tmp2);
        var b_final = Vector<float>.Build.DenseOfEnumerable(b_tmp_enu);

        //Debug.Log("FUGA10");
        M_tmp[0, 0] = A_tmp;
        M_tmp[1, 0] = C;
        M_tmp[0, 1] = C.Transpose();
        M_tmp[1, 1] = Matrix<float>.Build.Dense(C.RowCount, C.RowCount);

        var M = Matrix<float>.Build.DenseOfMatrixArray(M_tmp);

        //Debug.Log("FUGA11");
        //Solve Constraint
        var ansList = new List<float>(M.Solve(b_final).Enumerate());

        //Debug.Log("FUGA12");
        //Retrieve answer
        newControlPoints = new List<Vector3>();
        for(int i=0; i<N; i++)
        {
            Vector3 point = Vector3.zero;
            point.x = ansList[3 * i];
            point.y = ansList[3 * i + 1];
            point.z = ansList[3 * i + 2];
            newControlPoints.Add(point);
        }

        //Debug.Log("FUGA13");
        var newPbObject = new GameObject();
        newPb = newPbObject.AddComponent<PolyBezier>();
        newPb.setControlPoints(newControlPoints);
    }

    private float computeEfidelity()
    {
        float ret = 0.0f;
        float pFactor = 0.5f / N / displacement_normalizer / displacement_normalizer;
        float tFactor = 0.5f / (N - 1);
        Debug.Assert(pb.bezierCount == newPb.bezierCount);
        for(int i=0; i<pb.controlPoints.Count; i++)
        {
            var tmpvec = newPb.controlPoints[i] - pb.controlPoints[i];
            ret += Vector3.Dot(tmpvec, tmpvec) * pFactor;
            if (i < pb.controlPoints.Count - 1){
                tmpvec = (newPb.controlPoints[i + 1] - newPb.controlPoints[i]) - (pb.controlPoints[i + 1] - pb.controlPoints[i]);
                ret += Vector3.Dot(tmpvec, tmpvec) * tFactor / tNorms[i];
            }
        }
        return ret;
    }

    private (Matrix<float>, Vector<float>) EfidelityMat()
    {
        float pFactor = 0.5f / N / displacement_normalizer / displacement_normalizer;
        float tFactor = 0.5f / (N - 1);

        tNorms = new float[N-1];
        ////Debug.Log("FUGAx");

        for (int i=0; i<N-1; i++)
        {
            var Tangent = ControlPoints[i] - ControlPoints[i + 1];
            tNorms[i] = Vector3.Dot(Tangent, Tangent);
        }

        ////Debug.Log("FUGAy");

        Matrix<float> A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        for(int i=0; i<N; i++)
        {
            for(int j=0; j<3; j++)
            {
                A[3 * i + j, 3 * i + j] += 2 * pFactor;
                if (i > 0 && tNorms[i-1] > this.eps)
                {
                    A[3 * i + j, 3 * i + j] += 2 * tFactor / tNorms[i - 1];
                    A[3 * i + j, 3 * (i - 1) + j] -= 2 * tFactor / tNorms[i - 1];
                }
                if (i < N-1 && tNorms[i] > this.eps)
                {
                    A[3 * i + j, 3 * i + j] += 2 * tFactor / tNorms[i];
                    A[3 * i + j, 3 * (i + 1) + j] -= 2 * tFactor / tNorms[i];
                }
            }
        }
        ////Debug.Log("FUGAz");
        return (A, Vector<float>.Build.Dense(3 * N));
    }

    private Vector<float> calcFitPlane()
    {
        Vector3 avg = Vector3.zero;
        for(int i=0; i<N; i++)
        {
            avg += ControlPoints[i];
        }

        avg /= N;

        float xx = 0f; float xy = 0f; float xz = 0f;
        float yy = 0f; float yz = 0f; float zz = 0f;

        for(int i=0; i<N; i++)
        {
            Vector3 r = ControlPoints[i] - avg;
            xx += r.x * r.x;
            xy += r.x * r.y;
            xz += r.x * r.z;
            yy += r.y * r.y;
            yz += r.y * r.z;
            zz += r.z * r.z;
        }

        float det_x = yy * zz - yz * yz;
        float det_y = xx * zz - xz * xz;
        float det_z = xx * yy - xy * xy;

        float det_max = Mathf.Max(det_x, det_y, det_z);
        if(det_max <= 0f)
        {
            return Vector<float>.Build.Dense(3);
        }
        Vector3 normal;
        if (det_max == det_x)
        {
            normal = new Vector3(det_x, xz * yz - xy * zz, xy * yz - xz * yy);
        }
        else if(det_max == det_y)
        {
            normal = new Vector3(xz * yz - xy * zz, det_y, xy * xz - yz * xx);
        }
        else
        {
            normal = new Vector3(xy * yz - xz * yy, xy * xz - yz * xx, det_z);
        }

        normal = Vector3.Normalize(normal);

        return Vector<float>.Build.Dense(new float[] { normal.x, normal.y, normal.z });
    }

    private (Matrix<float>, Vector<float>) EPlainerMat()
    {
        var A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        var b = Vector<float>.Build.Dense(3 * N);

        var normal = calcFitPlane();

        Matrix<float> NN = normal.ToColumnMatrix() * normal.ToRowMatrix();


        float factor = 2f / (N - 1);

        var tangents = new List<Vector<float>>();

        for(int i=0; i<N-1; i++)
        {
            var diff = ControlPoints[i + 1] - ControlPoints[i];
            tangents.Add(Vector<float>.Build.Dense(new float[] { diff.x, diff.y, diff.z }));
        }

        for (int k = 0; k < N; k++)
        {
            Vector<float> b_k = Vector<float>.Build.Dense(3);

            if (k < N - 1 && !Mathf.Approximately(tNorms[k], 0f))
                b_k += factor * NN * tangents[k] / tNorms[k];
            if (k > 0 && !Mathf.Approximately(tNorms[k - 1], 0f))
                b_k += -factor * NN * tangents[k - 1] / tNorms[k - 1];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (k < N - 1)
                    {
                        A[3 * k + i, 3 * k + j] += factor * NN[i, j] / tNorms[k];
                        A[3 * k + i, 3 * (k + 1) + j] += -factor * NN[i, j] / tNorms[k];
                    }
                    if (k > 0)
                    {
                        A[3 * k + i, 3 * k + j] += factor * NN[i, j] / tNorms[k - 1];
                        A[3 * k + i, 3 * (k - 1) + j] += -factor * NN[i, j] / tNorms[k - 1];
                    }
                }
                b[3 * k + i] = b_k[i];
            }
            
        }

        return (A, b);
    }

    private (Matrix<float>, Vector<float>) EtangentMat(Vector3 Ttarget, int bezierIndex)
    {
        Matrix<float> A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        Vector<float> b = Vector<float>.Build.Dense(3 * N);

        int bStart, bEnd;
        if (bezierIndex < N - 1)
        {
            bStart = bezierIndex;
            bEnd = bezierIndex + 1;
        }
        else
        {
            bStart = bezierIndex - 1;
            bEnd = bezierIndex;
        }

        Vector3 tangent = ControlPoints[bEnd] - ControlPoints[bStart];

        var crossTtarget = Matrix<float>.Build.Dense(3, 3);
        crossTtarget[0, 1] = -Ttarget.z;
        crossTtarget[0, 2] = Ttarget.y;
        crossTtarget[1, 0] = Ttarget.z;
        crossTtarget[1, 2] = -Ttarget.x;
        crossTtarget[2, 0] = -Ttarget.y;
        crossTtarget[2, 1] = Ttarget.x;

        var f = 2f / Mathf.Max(eps, Vector3.Dot(tangent, tangent));
        var targetCrossTangent = -f * crossTtarget * Vector<float>.Build.Dense(new float[] {tangent.x, tangent.y, tangent.z });

        for(int i=0; i<3; i++)
        {
            for(int j=0; j<3; j++)
            {
                A[3 * bStart + i, 3 * bStart + j] = -f * crossTtarget[i, j];
                A[3 * bStart + i, 3 * bEnd + j] = f * crossTtarget[i, j];
                A[3 * bEnd + i, 3 * bStart + j] = f * crossTtarget[i, j];
                A[3 * bEnd + i, 3 * bEnd + j] = -f * crossTtarget[i, j];
            }

            b[3 * bStart + i] = targetCrossTangent[i];
            b[3 * bEnd + i] = -targetCrossTangent[i];
        }


        return (A, b);
    }

    private (Matrix<float>, Vector<float>) c0Mat(int index, Vector3 pos)
    {
        var C = Matrix<float>.Build.Dense(3, 3 * N);
        var b = Vector<float>.Build.Dense(new float[] {pos.x, pos.y, pos.z });

        for(int i=0; i<3; i++)
        {
            C[i, 3 * index + i] = 1f;
        }

        return (C, b);

    }

    private (Matrix<float>, Vector<float>) g1Mat()
    {
        var leftTNorms = new List<float>();
        var rightTNorms = new List<float>();
        for(int i=1; i<pb.beziers.Count; i++)
        {
            var leftT = ControlPoints[i * 3 - 1] - ControlPoints[i * 3];
            var rightT = ControlPoints[i * 3 + 1] - ControlPoints[i * 3];
            leftTNorms.Add(leftT.magnitude);
            rightTNorms.Add(rightT.magnitude);
        }

        var C = Matrix<float>.Build.Dense(3 * (bezierCount - 1), 3 * N);
        var b = Vector<float>.Build.Dense(3 * (bezierCount - 1));

        for(int i=0; i<pb.beziers.Count-1; i++)
        {
            if(leftTNorms[i] > eps && rightTNorms[i] > eps)
            {
                int ptIdx = 3 * (i + 1);
                for(int j=0; j<3; j++)
                {
                    C[3 * i + j, 3 * (ptIdx - 1) + j] = - (1 / leftTNorms[i]);
                    C[3 * i + j, 3 * ptIdx + j] = (1 / leftTNorms[i]) + (1 / rightTNorms[i]);
                    C[3 * i + j, 3 * (ptIdx + 1) + j] = (1 / rightTNorms[i]);
                }
            }         
        }

        return (C, b);
    }

    private (Matrix<float>, Vector<float>) selfc0Mat()
    {
        var C = Matrix<float>.Build.Dense(3, 3 * N);
        var d = Vector<float>.Build.Dense(3);

        for(int i=0; i<3; i++)
        {
            C[i, i] = 1f;
            C[i, 3 * (N - 1) + i] = -1;
        }

        return (C, d);
    }
}
