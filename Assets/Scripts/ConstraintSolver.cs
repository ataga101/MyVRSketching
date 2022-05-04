using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class ConstraintSolver
{
    int bezierCount;
    int N;

    PolyBezier pb;
    List<Vector3> controlPoints;
    List<Vector3> newControlPoints;

    //Norms of tangents
    float[] tNorms;

    float eps = 10e-6f;
    float displacement_normalizer = 0.04f;
    float lambda = 0.6f;

    ConstraintGenerator constraintGenerator;

    GameObject parentGameObject;

    public ConstraintSolver(PolyBezier pb, List<CollisionData> collisionData, List<float> sampledTimes, GameObject parentObject)
    {
        //number of bezier curve
        this.bezierCount = pb.bezierCount;
        //number of control points
        this.N = bezierCount * 3 + 1;

        //control points
        controlPoints = new List<Vector3>();
        for (int i = 0; i < pb.beziers.Count; i++)
        {
            controlPoints.Add(pb.beziers[i].P0);
            controlPoints.Add(pb.beziers[i].P1);
            controlPoints.Add(pb.beziers[i].P2);
        }
        controlPoints.Add(pb.beziers[pb.beziers.Count - 1].P3);

        this.pb = pb;

        constraintGenerator = new ConstraintGenerator(pb.controlPoints, collisionData, sampledTimes);

        parentGameObject = parentObject;
    }

    //Returns ControlPoints
    public List<Vector3> solve()
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

        List<Vector3> bestControlPoints;
        float minEnergy;
        bool noPointRemoved = false;

        //Debug.Log("HOGE1");

        List<(int, Vector3)> c0Constraints;
        List<(int, Vector3)> tangentConstraints;

        //Solve without removing constraints
        (controlPoints, c0Constraints, tangentConstraints) = constraintGenerator.Generate(disabledMap);
        pb.setControlPoints(controlPoints);

        //Debug.Log("HOGE2");

        solveSingle(c0Constraints, tangentConstraints);
        bestControlPoints = new List<Vector3>(newControlPoints);

        //Debug.Log("HOGE3");
        minEnergy = computeEnergy(disabledMap);
        //Debug.Log("HOGE4");

        //Remove points and test
        while (!noPointRemoved)
        {
            noPointRemoved = true;
            for(int i=0; i<constraintGenerator.candidateNum; i++)
            {
                //Debug.Log("HOGE");
                //Debug.Log(i);
                if (removedMap[i])
                {
                    continue;
                }
                disabledMap = removedMap;
                disabledMap[i] = true;

                //Don't forget to set control points
                (controlPoints, c0Constraints, tangentConstraints) = constraintGenerator.Generate(disabledMap);
                pb.setControlPoints(controlPoints);

                solveSingle(c0Constraints, tangentConstraints);
                float energy = computeEnergy(disabledMap);

                if (energy < minEnergy)
                {
                    noPointRemoved = false;
                    bestMap = new List<bool> (disabledMap);
                    bestControlPoints = new List<Vector3> (newControlPoints);
                    minEnergy = energy;
                }
            }
            removedMap = bestMap;
        }
        return bestControlPoints;
    }

    private float computeEConnectivity(List<bool> disabledMap)
    {
        if (disabledMap.Count == 0)
        {
            return Mathf.Exp(-1);
        }

        float ret = 0f;

        for(int i=0; i<disabledMap.Count; i++)
        {
            if (!disabledMap[i])
            {
                ret += 1f;
            }
        }

        float x = ret / disabledMap.Count;

        return Mathf.Exp(- x * x);
    }

    private float computeEnergy(List<bool> disabledMap)
    {
        return lambda * computeEfidelity() + (1 - lambda) * computeEConnectivity(disabledMap);
    }

    //Generate constraint from controlPoints -> Solve -> Store Result in newControlPoints
    private void solveSingle(List<(int, Vector3)> c0Constraints, List<(int, Vector3)> tangentConstraints) { 
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
        bool hasSelfConstraint = (controlPoints[0] - controlPoints[controlPoints.Count-1]).magnitude < 0.2;

        //Debug.Log("FUGA4");
        int g1ConstraintCount = (hasg1Constraint) ? 1 : 0;
        int selfConstraintCount = (hasSelfConstraint) ? 1 : 0;

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
        if (hasSelfConstraint)
        {
            //Self constraint constraint
            (Matrix<float> C_ret, Vector<float> b_ret) = selfc0Mat();
            C_tmp[c0Constraints.Count + g1ConstraintCount, 0] = C_ret;
            b_tmp2.AddRange(b_ret.Enumerate());

            //Tangent constraint
            (A, b) = EtangentMat(controlPoints[1] - controlPoints[0], N - 1);
            A_tmp += A;
            b_tmp += b;
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
            Vector3 point = new Vector3(
            ansList[3 * i],
            ansList[3 * i + 1],
            ansList[3 * i + 2]);
            newControlPoints.Add(point + controlPoints[i]);
        }
    }

    //Compute Efidelity between controlPoints and newControlPoints
    private float computeEfidelity()
    {
        float ret = 0.0f;
        float pFactor = 0.5f / (N * displacement_normalizer * displacement_normalizer);
        float tFactor = 0.5f / (N - 1);
        for(int i=0; i<controlPoints.Count; i++)
        {
            var tmpvec = newControlPoints[i] - controlPoints[i];
            ret += Vector3.Dot(tmpvec, tmpvec) * pFactor;
            if (i < controlPoints.Count - 1){
                tmpvec = (newControlPoints[i + 1] - newControlPoints[i]) - (controlPoints[i + 1] - controlPoints[i]);
                ret += Vector3.Dot(tmpvec, tmpvec) * tFactor / tNorms[i];
            }
        }
        return ret;
    }
    
    private (Matrix<float>, Vector<float>) EfidelityMat()
    {
        float pFactor = 0.5f / (N * displacement_normalizer * displacement_normalizer);
        float tFactor = 0.5f / (N - 1);

        tNorms = new float[N-1];
        ////Debug.Log("FUGAx");

        for (int i=0; i<N-1; i++)
        {
            var Tangent = controlPoints[i] - controlPoints[i + 1];
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
            avg += controlPoints[i];
        }

        avg /= N;

        float xx = 0f; float xy = 0f; float xz = 0f;
        float yy = 0f; float yz = 0f; float zz = 0f;

        for(int i=0; i<N; i++)
        {
            Vector3 r = controlPoints[i] - avg;
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
            var diff = controlPoints[i + 1] - controlPoints[i];
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

    private (Matrix<float>, Vector<float>) EtangentMat(Vector3 Ttarget, int index)
    {
        Matrix<float> A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        Vector<float> b = Vector<float>.Build.Dense(3 * N);

        int bStart, bEnd;
        if (index < N - 1)
        {
            bStart = index;
            bEnd = index + 1;
        }
        else
        {
            bStart = index - 1;
            bEnd = index;
        }

        Vector3 tangent = controlPoints[bEnd] - controlPoints[bStart];

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
        var b_vec = controlPoints[index] - pos;
        var b = Vector<float>.Build.Dense(new float[] {b_vec.x, b_vec.y, b_vec.z });

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
            var leftT = controlPoints[i * 3 - 1] - controlPoints[i * 3];
            var rightT = controlPoints[i * 3 + 1] - controlPoints[i * 3];
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
            C[i, 3 * (N - 1) + i] = -1f;
        }

        var d_vec = controlPoints[N - 1] - controlPoints[0];
        d[0] = d_vec.x;
        d[1] = d_vec.y;
        d[2] = d_vec.z;

        return (C, d);
    }
}
