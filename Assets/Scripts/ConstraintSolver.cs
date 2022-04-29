using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class ConstraintSolver
{
    int numSegments;
    int N;

    PolyBezier pb;
    List<Vector3> ControlPoints;
    Vector3 ptarget1;
    Vector3 ptarget2;
    int targetType;
    float[] tNorms;

    float eps = 10e-6f;

    public ConstraintSolver(PolyBezier pb, List<(int, Vector3)> c0Constraints, List<(int, Vector3)> tangentConstraints)
    {
        //set parameters

        //number of bezier curve
        this.numSegments = pb.numSegments;
        //number of control points
        this.N = numSegments * 3 + 1;

        //control points
        ControlPoints = new List<Vector3>();
        for (int i = 0; i < numSegments; i++)
        {
            ControlPoints.Add(pb.beziers[i].P0);
            ControlPoints.Add(pb.beziers[i].P1);
            ControlPoints.Add(pb.beziers[i].P2);
        }
        ControlPoints.Add(pb.beziers[numSegments - 1].P3);
        this.pb = pb;

        //solve
        var A_tmp = Matrix<float>.Build.Dense(3 * N, 3 * N);
        var b_tmp = Vector<float>.Build.Dense(3 * N);

        var (A, b) = EfidelityMat();
        A_tmp += A;
        b_tmp += b;

        (A, b) = EPlainerMat();
        A_tmp += A;
        b_tmp += b;

        foreach ((int idx, Vector3 pos) in tangentConstraints)
        {
            (A, b) = EtangentMat(pos, idx);
            A_tmp += A;
            b_tmp += b;
        }

        bool hasg1Constraint = (N > 4);
        bool isSelfConstraint = (ControlPoints[0] - ControlPoints[ControlPoints.Count-1]).magnitude < 0.05;

        int g1ConstraintCount = (hasg1Constraint) ? 1 : 0;
        int selfConstraintCount = (isSelfConstraint) ? 1 : 0;

        int numConstraints = c0Constraints.Count + g1ConstraintCount + selfConstraintCount;

        var C_tmp = new Matrix<float>[numConstraints, 0];
        var b_tmp2 = new List<float>();

        for (int i = 0; i < c0Constraints.Count; i++)
        {
            var (idx, pos) = c0Constraints[i];
            Vector<float> b_ret;
            (C_tmp[i, 0], b_ret) = c0Mat(idx, pos);
            b_tmp2.AddRange(b_ret.Enumerate());
        }

        if(hasg1Constraint)
        {
            Vector<float> b_ret;
            (C_tmp[c0Constraints.Count, 0], b_ret) = g1Mat();
            b_tmp2.AddRange(b_ret.Enumerate());
        }

        if (isSelfConstraint)
        {
            Vector<float> b_ret;
            (C_tmp[c0Constraints.Count + g1ConstraintCount, 0], b_ret) = selfc0Mat();
            b_tmp2.AddRange(b_ret.Enumerate());
        }

        var M_tmp = new Matrix<float>[2, 2];
        var b_tmp_final = new Vector<float>[2];

        var C = Matrix<float>.Build.DenseOfMatrixArray(C_tmp);
        var b_tmp2_concatinated = Vector<float>.Build.DenseOfEnumerable(b_tmp2);

        M_tmp[0, 0] = A_tmp;
        M_tmp[1, 0] = C;
        M_tmp[0, 1] = C.Transpose();
        M_tmp[1, 1] = Matrix<float>.Build.Dense(C.RowCount, C.RowCount);
    }

    private (Matrix<float>, Vector<float>) EfidelityMat(float displacement_normalizer=0.04f)
    {
        float pFactor = 0.5f / N;
        float tFactor = 0.5f / (N - 1);

        tNorms = new float[N-1];

        for(int i=0; i<N-1; i++)
        {
            var Tangent = ControlPoints[i] - ControlPoints[i + 1];
            tNorms[i] = Vector3.Dot(Tangent, Tangent);
        }

        pFactor /= (displacement_normalizer * displacement_normalizer);

        Matrix<float> A = Matrix<float>.Build.Dense(3 * N, 3 * N);
        for(int i=0; i<N; i++)
        {
            for(int j=0; j<N; j++)
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
        for(int i=0; i<numSegments-1; i++)
        {
            var leftT = ControlPoints[i * 3 + 3] - ControlPoints[i * 3 + 2];
            var rightT = ControlPoints[i * 3 + 4] - ControlPoints[i * 3 + 3];
            leftTNorms.Add(leftT.magnitude);
            rightTNorms.Add(rightT.magnitude);
        }

        var C = Matrix<float>.Build.Dense(3 * (numSegments - 1), 3 * N);
        var b = Vector<float>.Build.Dense(3 * (numSegments - 1));

        for(int i=0; i<numSegments-1; i++)
        {
            if(leftTNorms[i] > eps && rightTNorms[i] > eps)
            {
                for(int j=0; j<3; j++)
                {
                    C[3 * i + j, 3 * i + j] = - (1 / leftTNorms[i]);
                    C[3 * i + j, 3 * (i + 1) + j] = (1 / leftTNorms[i]) - (1 / rightTNorms[i]);
                    C[3 * i + j, 3 * (i + 2) + j] = (1 / rightTNorms[i]);
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
